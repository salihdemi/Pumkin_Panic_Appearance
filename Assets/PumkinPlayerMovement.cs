using UnityEngine;
using UnityEngine.InputSystem;

public class PumkinPlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public Animator anim;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (anim == null) anim = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogWarning(gameObject.name + " üzerinde Rigidbody bulunamadý! Hareket transform üzerinden yapýlacak.");
        }
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            float x = Keyboard.current.dKey.isPressed ? 1 : (Keyboard.current.aKey.isPressed ? -1 : 0);
            float z = Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0);
            input = new Vector2(x, z).normalized; // Çapraz gitme hýzýný eþitlemek için normalized ekledik
        }

        // --- GLOBAL HAREKET ---
        // transform.right ve transform.forward yerine Vector3.right (X) ve Vector3.forward (Z) kullanýyoruz.
        Vector3 move = Vector3.right * input.x + Vector3.forward * input.y;

        if (rb != null)
        {
            rb.MovePosition(rb.position + move * walkSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += move * walkSpeed * Time.deltaTime;
        }

        if (anim != null)
        {
            anim.SetFloat("X", input.x, 0.1f, Time.deltaTime);
            anim.SetFloat("Z", input.y, 0.1f, Time.deltaTime);
            anim.SetFloat("Speed", input.magnitude);
        }
    }
}