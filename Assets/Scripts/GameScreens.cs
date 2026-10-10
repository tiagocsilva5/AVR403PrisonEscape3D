using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Creates one reusable screen canvas automatically; no scene Canvas is required.
public class GameScreens : MonoBehaviour
{
    private static GameScreens instance;
    public static GameScreens Instance { get { return EnsureInstance(); } }
    public static bool BlocksGameplay => instance != null && instance.overlay.activeSelf;

    private GameObject overlay;
    private Text chapter, title, description, status, footer;
    private Button primary, secondary;
    private GameObject progressTrack;
    private Image progressFill;
    private EventSystem ownedEventSystem;
    private Font font;
    private bool loading;
    private bool hasShownStartScreen;
    private bool gameplayStarted;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { EnsureInstance(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ShowStartupAfterSceneLoad()
    {
        EnsureInstance().EnsureStartupScreen();
    }

    private static GameScreens EnsureInstance()
    {
        if (instance == null)
            new GameObject("Prison Escape Screens").AddComponent<GameScreens>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildInterface();
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        EnsureEventSystem();
        if (loading) return;
        if (!hasShownStartScreen) EnsureStartupScreen();
        else if (gameplayStarted) StartCoroutine(PrepareScene(scene));
    }

    private void Start() { EnsureStartupScreen(); }

    private void EnsureStartupScreen()
    {
        if (!hasShownStartScreen && !loading)
            ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        if (loading) return;
        hasShownStartScreen = true;
        Present("AN ESCAPE IS EARNED / ONE BLOCK AT A TIME", "Prison Escape",
            "HOW TO PLAY\n" +
            "WASD / Arrow keys: move  |  Shift: run\n" +
            "Space: jump  |  Mouse: turn\n" +
            "Right mouse: camera  |  Scroll: zoom\n\n" +
            "Avoid guards. Use walls to break their line of sight.\n" +
            "Reach the exit to advance. Three captures ends your escape.",
            "STAY QUIET. STAY OUT OF SIGHT. FIND YOUR WAY OUT.",
            "START GAME", () =>
            {
                gameplayStarted = true;
                StartCoroutine(PrepareScene(SceneManager.GetActiveScene()));
            });
        title.fontSize = 50;
        title.rectTransform.anchoredPosition = new Vector2(0f, 155f);
        description.fontSize = 18;
        description.rectTransform.sizeDelta = new Vector2(580f, 170f);
        description.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        status.rectTransform.anchoredPosition = new Vector2(0f, -86f);
        AddSecondary("QUIT GAME", QuitGame);
    }

    private IEnumerator PrepareScene(Scene scene)
    {
        loading = true;
        Present("PRISON ESCAPE / " + scene.name.ToUpperInvariant(),
            "BACK INSIDE", "Stay out of sight. Every capture brings you closer to lockdown.",
            "PREPARING THE PRISON...", "", null);
        yield return new WaitForSecondsRealtime(0.6f);
        loading = false;
        Close();
    }

    public void ShowCaught(int captures, int limit, Action returnToCell)
    {
        if (BlocksGameplay) return;
        int remaining = Mathf.Max(0, limit - captures);
        Present("SECURITY ALERT / CAPTURE " + captures + " OF " + limit,
            "CAUGHT", "The guards found you. You are being sent back to your cell.",
            remaining + (remaining == 1 ? " CHANCE REMAINING" : " CHANCES REMAINING"),
            "RETURN TO CELL", () =>
            {
                returnToCell?.Invoke();
                Close();
            });
    }

    public void ShowGameOver(int captures)
    {
        if (loading) return;
        Present("SECURITY ALERT / FINAL CAPTURE", "LOCKDOWN",
            "Caught " + captures + " times. Your escape attempt is over.",
            "LEARN THE PATROLS. FIND ANOTHER WAY.", "TRY AGAIN", RestartLevel);
        AddSecondary("QUIT GAME", QuitGame);
    }

    public void ShowDeath(string reason = "You did not make it out of the prison.")
    {
        if (BlocksGameplay) return;
        Present("ESCAPE ATTEMPT / FAILED", "YOU DIED", reason,
            "A NEW ATTEMPT STARTS IN YOUR CELL.", "TRY AGAIN", RestartLevel);
        AddSecondary("QUIT GAME", QuitGame);
    }

    public void ShowNextLevel(string nextScene)
    {
        if (BlocksGameplay) return;
        bool hasNext = !string.IsNullOrEmpty(nextScene);
        Present("ESCAPE ROUTE / CHECKPOINT REACHED",
            hasNext ? "BLOCK CLEARED" : "YOU ESCAPED",
            hasNext ? "You made it through. Another prison block stands between you and freedom."
                    : "The prison is behind you. Your escape is complete.",
            hasNext ? "NEXT DESTINATION / " + nextScene.ToUpperInvariant() : "ESCAPE COMPLETE",
            hasNext ? "CONTINUE" : "PLAY AGAIN",
            () => LoadLevel(hasNext ? nextScene : FirstLevel()));
        AddSecondary("QUIT GAME", QuitGame);
    }

    public void RestartLevel() { LoadLevel(SceneManager.GetActiveScene().name); }

    private string FirstLevel()
    {
        if (SceneManager.sceneCountInBuildSettings == 0)
            return SceneManager.GetActiveScene().name;
        return System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0));
    }

    public void LoadLevel(string sceneName)
    {
        if (loading) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Present("ESCAPE ROUTE / UNAVAILABLE", "ROUTE CLOSED",
                "This destination is unavailable. Return to the current block.",
                "", "RETURN", Close);
            Debug.LogError("Add scene '" + sceneName + "' to Build Settings before loading it.", this);
            return;
        }
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        Present("TRANSFER / " + sceneName.ToUpperInvariant(), "LOADING",
            "Stay quiet. Watch the guards. Use cover to break their line of sight.",
            "0%", "", null);
        progressTrack.SetActive(true);
        SetProgress(0f);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        float start = Time.realtimeSinceStartup;
        while (operation.progress < 0.9f || Time.realtimeSinceStartup - start < 0.6f)
        {
            SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
            yield return null;
        }
        SetProgress(1f);
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;
        yield return null;
        EnsureEventSystem();
        loading = false;
        // A restarted/new level starts with normal gameplay time.
        previousTimeScale = 1f;
        Close();
    }

    private void SetProgress(float value)
    {
        progressFill.rectTransform.anchorMax = new Vector2(value, 1f);
        status.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void Present(string heading, string mainTitle, string body,
        string detail, string buttonLabel, Action action)
    {
        if (!overlay.activeSelf)
        {
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
        }
        overlay.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();
        // Restore the standard layout after the larger instructions screen.
        title.fontSize = 58;
        title.rectTransform.anchoredPosition = new Vector2(0f, 148f);
        description.fontSize = 22;
        description.rectTransform.sizeDelta = new Vector2(550f, 115f);
        description.rectTransform.anchoredPosition = new Vector2(0f, 42f);
        status.rectTransform.anchoredPosition = new Vector2(0f, -46f);
        chapter.text = heading;
        title.text = mainTitle;
        description.text = body;
        status.text = detail;
        progressTrack.SetActive(false);
        secondary.gameObject.SetActive(false);
        primary.onClick.RemoveAllListeners();
        primary.gameObject.SetActive(action != null);
        if (action != null)
        {
            primary.GetComponentInChildren<Text>().text = buttonLabel;
            primary.onClick.AddListener(() => action());
            EventSystem.current?.SetSelectedGameObject(primary.gameObject);
        }
        else EventSystem.current?.SetSelectedGameObject(null);
    }

    private void AddSecondary(string label, Action action)
    {
        secondary.gameObject.SetActive(true);
        secondary.GetComponentInChildren<Text>().text = label;
        secondary.onClick.RemoveAllListeners();
        secondary.onClick.AddListener(() => action());
    }

    private void Close()
    {
        overlay.SetActive(false);
        Time.timeScale = previousTimeScale;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureEventSystem()
    {
        EventSystem other = null;
        foreach (EventSystem candidate in FindObjectsOfType<EventSystem>())
            if (candidate != ownedEventSystem && candidate.isActiveAndEnabled) { other = candidate; break; }
        if (ownedEventSystem == null)
        {
            GameObject events = new GameObject("Screen Input", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(transform, false);
            ownedEventSystem = events.GetComponent<EventSystem>();
        }
        ownedEventSystem.enabled = other == null;
        ownedEventSystem.GetComponent<StandaloneInputModule>().enabled = other == null;
    }

    private void BuildInterface()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new GameObject("Screens Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform background = Panel("Overlay", canvasObject.transform, new Color(0.025f, 0.035f, 0.045f, 0.97f));
        Stretch(background);
        overlay = background.gameObject;
        Color amber = new Color(1f, 0.69f, 0.27f);
        for (int i = 0; i < 13; i++)
        {
            RectTransform bar = Panel("Prison Bar", background, new Color(0.1f, 0.14f, 0.17f, 0.3f));
            bar.anchorMin = new Vector2(i / 12f, 0f);
            bar.anchorMax = new Vector2(i / 12f, 1f);
            bar.sizeDelta = new Vector2(4f, 0f);
            bar.anchoredPosition = Vector2.zero;
        }
        RectTransform card = Panel("Screen Card", background, new Color(0.055f, 0.075f, 0.09f));
        Center(card, new Vector2(680f, 530f), Vector2.zero);
        RectTransform line = Panel("Amber Rule", card, amber);
        Center(line, new Vector2(580f, 3f), new Vector2(0f, 250f));
        chapter = Label("Chapter", card, 15, amber, new Vector2(580f, 30f), new Vector2(0f, 211f));
        title = Label("Title", card, 58, Color.white, new Vector2(610f, 85f), new Vector2(0f, 148f));
        description = Label("Description", card, 22, new Color(0.77f, 0.82f, 0.85f),
            new Vector2(550f, 115f), new Vector2(0f, 42f));
        status = Label("Status", card, 16, amber, new Vector2(580f, 45f), new Vector2(0f, -46f));
        RectTransform track = Panel("Progress", card, new Color(0.13f, 0.17f, 0.2f));
        Center(track, new Vector2(550f, 8f), new Vector2(0f, -97f));
        progressTrack = track.gameObject;
        RectTransform fill = Panel("Progress Fill", track, amber);
        Stretch(fill);
        progressFill = fill.GetComponent<Image>();
        primary = MakeButton("Primary", card, amber, new Vector2(0f, -151f));
        secondary = MakeButton("Secondary", card, new Color(0.14f, 0.19f, 0.22f), new Vector2(0f, -215f));
        secondary.GetComponentInChildren<Text>().color = Color.white;
        footer = Label("Footer", background, 14, new Color(0.6f, 0.67f, 0.72f),
            new Vector2(1000f, 30f), Vector2.zero);
        footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        footer.rectTransform.anchoredPosition = new Vector2(0f, 28f);
        footer.text = "PRISON ESCAPE   /   STAY OUT OF SIGHT";
        overlay.SetActive(false);
    }

    private RectTransform Panel(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return obj.GetComponent<RectTransform>();
    }

    private Text Label(string name, Transform parent, int size, Color color, Vector2 dimensions, Vector2 position)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        Center(text.rectTransform, dimensions, position);
        return text;
    }

    private Button MakeButton(string name, Transform parent, Color color, Vector2 position)
    {
        RectTransform rect = Panel(name, parent, color);
        Center(rect, new Vector2(420f, 52f), position);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 0.75f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
        Label("Label", rect, 18, new Color(0.04f, 0.055f, 0.065f), new Vector2(400f, 48f), Vector2.zero);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded;
        instance = null;
    }
}
