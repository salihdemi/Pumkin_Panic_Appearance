using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;
    public InputActionAsset InputActions;

    // Raycast'in ne kadar uzağa gideceğini belirleyen menzil (kameradan ölçülüyor)
    public float attackRange = 50f;
    // Sadece düşmanları vurmak için Unity'de seçeceğin Layer
    public LayerMask enemyLayer;

    [Header("Saldırı hissi")]
    [Tooltip("İki vuruş arasındaki en kısa süre.")]
    public float cooldown = 0.25f;
    [Tooltip("Vururken oyuncunun hedefe doğru atıldığı mesafe.")]
    public float lungeDistance = 0.18f;
    public float lungeDuration = 0.12f;

    /// <summary>Lunge sürerken hareket kilitleniyor — PlayerMovement bunu okuyor.</summary>
    public bool IsAttacking { get; private set; }

    private InputActionMap actionMap;
    private InputAction attack;
    private Rigidbody rb;

    private float nextAttackTime;
    private Coroutine lungeRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();

        if (InputActions == null)
        {
            Debug.LogError("PlayerAttack: InputActions atanmamış.", this);
            return;
        }

        // Asset'teki map'lerin adı "Player" ve "UI" — "Custom Player" diye bir map hiç yoktu,
        // FindActionMap null dönüyordu ve bir alttaki satır Awake'te NullReference atıyordu.
        actionMap = InputActions.FindActionMap("Player");
        if (actionMap == null)
        {
            Debug.LogError("PlayerAttack: 'Player' action map'i bulunamadı.", this);
            return;
        }

        attack = actionMap.FindAction("Attack");
        if (attack == null)
            Debug.LogError("PlayerAttack: 'Attack' action'ı bulunamadı.", this);
    }

    private void OnEnable()
    {
        if (attack == null) return;

        actionMap.Enable();
        attack.started += OnAttack;
    }

    private void OnDisable()
    {
        if (attack == null) return;

        attack.started -= OnAttack;
        actionMap.Disable();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + cooldown;

        if (animator != null) animator.SetTrigger("Attack");

        if (Camera.main == null || Mouse.current == null) return;

        // 1. Farenin ekrandaki piksel pozisyonundan kameradan içeri bir ışın gönder
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);

        // Editörde görsellik için ışını çizelim (Kırmızı çizgi)
        Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, 0.5f);

        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, attackRange, enemyLayer))
        {
            // Sarsıntı ve knockback yönü: oyuncudan hedefe, yatay düzlemde.
            // Yukarı/aşağı bileşeni bırakırsak kamera dikey zıplıyor ve kötü görünüyor.
            Vector3 direction = hit.point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

            Lunge(direction);

            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Düşman kendi tepkisini (flash/squash/knockback) verip juice'u da kendisi tetikliyor.
                enemy.GetHit(hit.point, hit.normal, direction);
                return;
            }

            // Enemy layer'ında ama Enemy component'i olmayan bir şey: yine de çarpma efekti verelim.
            if (HitJuice.Instance != null)
                HitJuice.Instance.Impact(hit.point, hit.normal, direction);

            return;
        }

        // Iska: hit-stop yok, sadece hafif bir sarsıntı ve "swish".
        Vector3 whiffDirection = ray.direction;
        whiffDirection.y = 0f;

        Lunge(whiffDirection);

        if (HitJuice.Instance != null)
            HitJuice.Instance.Whiff(whiffDirection);
    }

    private void Lunge(Vector3 direction)
    {
        if (lungeDistance <= 0f || lungeDuration <= 0f) return;
        if (direction.sqrMagnitude < 0.0001f) return;

        if (lungeRoutine != null) StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(LungeRoutine(direction.normalized));
    }

    /// <summary>Rigidbody'yi oynattığımız için fizik adımında ilerliyoruz.</summary>
    private IEnumerator LungeRoutine(Vector3 direction)
    {
        IsAttacking = true;

        float speed = lungeDistance / lungeDuration;
        float t = 0f;

        while (t < lungeDuration)
        {
            // Sertçe fırlayıp yavaşlıyor; sabit hızla itmekten çok daha canlı.
            float k = 1f - (t / lungeDuration);
            Vector3 delta = direction * (speed * 2f * k * Time.fixedDeltaTime);

            if (rb != null) rb.MovePosition(rb.position + delta);
            else transform.position += delta;

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        IsAttacking = false;
    }
}
