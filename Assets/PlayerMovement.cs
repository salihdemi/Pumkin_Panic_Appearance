using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public Animator anim;

    private Rigidbody rb;
    private PlayerAttack playerAttack;

    private Vector2 input;

    // Lamb'in controller'ında (LambHitAnimator) sadece Speed ve Attack var — X ve Z yok.
    // Pumkin karakterinin controller'ında ise dördü de var. Olmayan parametreye SetFloat
    // çağırmak her frame konsola uyarı bastığı için hangisinin bulunduğunu bir kez ölçüyoruz.
    private bool hasX, hasZ, hasSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerAttack = GetComponent<PlayerAttack>();
        if (anim == null) anim = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogWarning(gameObject.name + " üzerinde Rigidbody bulunamadı! Hareket transform üzerinden yapılacak.");
        }

        if (anim != null)
        {
            hasX = HasParameter(anim, "X");
            hasZ = HasParameter(anim, "Z");
            hasSpeed = HasParameter(anim, "Speed");
        }
    }

    void Update()
    {
        input = Vector2.zero;

        // Saldırı sırasında hareket kilitli: vuruşun bir ağırlığı olsun, kaçarak vurulamasın.
        bool attacking = playerAttack != null && playerAttack.IsAttacking;

        if (!attacking && Keyboard.current != null)
        {
            float x = Keyboard.current.dKey.isPressed ? 1 : (Keyboard.current.aKey.isPressed ? -1 : 0);
            float z = Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0);
            input = new Vector2(x, z).normalized; // çapraz gitme hızını eşitlemek için normalized ekledik
        }

        if (anim != null)
        {
            if (hasX) anim.SetFloat("X", input.x, 0.1f, Time.deltaTime);
            if (hasZ) anim.SetFloat("Z", input.y, 0.1f, Time.deltaTime);
            if (hasSpeed) anim.SetFloat("Speed", input.magnitude);
        }
    }

    // Rigidbody fizik adımında oynatılır. MovePosition'ı Update'ten çağırmak
    // yüksek FPS'te titremeye yol açıyordu.
    void FixedUpdate()
    {
        // Lunge sırasında Rigidbody'yi PlayerAttack sürüyor. Buradan da MovePosition
        // çağırırsak aynı fizik adımında son çağrı kazanır ve lunge'ı iptal ederiz.
        if (playerAttack != null && playerAttack.IsAttacking) return;

        // --- GLOBAL HAREKET ---
        // transform.right ve transform.forward yerine Vector3.right (X) ve Vector3.forward (Z) kullanıyoruz.
        Vector3 move = Vector3.right * input.x + Vector3.forward * input.y;

        if (rb != null)
        {
            rb.MovePosition(rb.position + move * walkSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position += move * walkSpeed * Time.fixedDeltaTime;
        }
    }

    static bool HasParameter(Animator animator, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName) return true;
        }
        return false;
    }
}
