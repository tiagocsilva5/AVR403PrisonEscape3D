using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("Leave empty to load the next enabled scene in Build Settings.")]
    public string nextSceneName;
    private bool activated;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCapture player = other.GetComponentInParent<PlayerCapture>();
        if (activated || player == null || player.HasLost || GameScreens.BlocksGameplay) return;
        activated = true;

        string destination = nextSceneName;
        if (string.IsNullOrWhiteSpace(destination))
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            destination = SceneManager.GetActiveScene().buildIndex >= 0 &&
                nextIndex < SceneManager.sceneCountInBuildSettings
                ? System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(nextIndex))
                : "";
        }
        GameScreens.Instance.ShowNextLevel(destination);
    }
}
