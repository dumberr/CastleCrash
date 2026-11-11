using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TestLauncher : MonoBehaviour
{
    [SerializeField] private Vector2 launchVelocity = new Vector2(12f, 5f);

    private Rigidbody2D rb;
    private bool launched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // sta pe loc pana la lansare
    }

    private void Update()
    {
        if (launched || Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            launched = true;
            rb.gravityScale = 1f;
            rb.linearVelocity = launchVelocity;
        }
    }
}