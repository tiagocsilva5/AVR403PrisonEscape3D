using UnityEngine;
using UnityEngine.Events;

public class PlayerCapture : MonoBehaviour
{
    [Header("Cell")]
    [Tooltip("Optional. If empty, uses the player's starting position.")]
    public Transform cellSpawnPoint;

    [Header("Capture Settings")]
    [Min(1)] public int maximumCaptures = 3;
    [Min(0.1f)] public float captureCooldown = 1f;

    [Header("Game Over")]
    public GameObject gameOverPanel;

    [Tooltip("Assign your player movement/input scripts here.")]
    public Behaviour[] disableOnGameOver;

    public bool pauseOnGameOver = true;
    public UnityEvent onGameOver = new UnityEvent();

    public int CaptureCount { get; private set; }
    public bool HasLost { get; private set; }

    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private float nextCaptureTime;

    private void Awake()
    {
        startingPosition = transform.position;
        startingRotation = transform.rotation;

        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void Capture()
    {
        // Prevent repeated captures from overlapping NPC colliders.
        if (HasLost || Time.unscaledTime < nextCaptureTime)
            return;

        nextCaptureTime = Time.unscaledTime + captureCooldown;
        CaptureCount++;

        ReturnToCell();

        Debug.Log(
            $"Player caught: {CaptureCount}/{maximumCaptures}",
            this);

        if (CaptureCount >= maximumCaptures)
            LoseGame();
    }

    private void ReturnToCell()
    {
        Vector3 destination = cellSpawnPoint != null
            ? cellSpawnPoint.position
            : startingPosition;

        Quaternion rotation = cellSpawnPoint != null
            ? cellSpawnPoint.rotation
            : startingRotation;

        // Temporarily disable the controller to allow teleporting.
        bool controllerWasEnabled =
            characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(destination, rotation);

        if (playerRigidbody != null)
        {
            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            playerRigidbody.position = destination;
            playerRigidbody.rotation = rotation;
        }

        Physics.SyncTransforms();

        if (controllerWasEnabled)
            characterController.enabled = true;
    }

    private void LoseGame()
    {
        HasLost = true;

        if (disableOnGameOver != null)
        {
            foreach (Behaviour component in disableOnGameOver)
            {
                if (component != null && component != this)
                    component.enabled = false;
            }
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseOnGameOver)
            Time.timeScale = 0f;

        Debug.Log("Game Over: caught three times.", this);
        onGameOver.Invoke();
    }
}