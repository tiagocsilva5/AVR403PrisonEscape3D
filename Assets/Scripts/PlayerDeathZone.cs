using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class PlayerDeathZone : MonoBehaviour
{
    [TextArea] public string reason = "You fell beyond the prison's escape route.";

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
        if (player != null) player.Die(reason);
    }
}
