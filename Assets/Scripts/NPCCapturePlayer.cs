using UnityEngine;

public class NPCCapturePlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TryCapture(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCapture(collision.collider);
    }

    private void TryCapture(Collider other)
    {
        PlayerCapture player =
            other.GetComponentInParent<PlayerCapture>();

        if (player != null)
            player.Capture();
    }
}