using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;

    public InputActionAsset InputActions;
    private InputActionMap actionMap;
    private InputAction attack;

    // Raycast'in ne kadar uzağa gideceğini belirleyen menzil
    public float attackRange = 50f;
    // Sadece düşmanları vurmak için Unity'de seçeceğin Layer
    public LayerMask enemyLayer;


    private void Awake()
    {
        actionMap = InputActions.FindActionMap("Custom Player");
        attack = actionMap.FindAction("Attack");
    }

    private void OnEnable()
    {
        if (actionMap != null)
        {
            actionMap.Enable();
            attack.started += OnAttack;
        }
    }

    private void OnDisable()
    {
        if (actionMap != null)
        {
            actionMap.Disable();
            attack.started -= OnAttack;
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        Debug.Log("Saldırı yapıldı!");

        // 1. Farenin ekrandaki piksel pozisyonunu al
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // 2. Kameradan farenin olduğu noktaya doğru 3D bir ışın (Ray) oluştur
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);

        // Çarpma bilgilerini saklayacağımız 3D hit objesi
        RaycastHit hit;

        // Editörde görsellik için ışını çizelim (Kırmızı çizgi)
        Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, 0.5f);

        // 3. 3D Raycast fırlat (Işının başladığı yer, yönü, çarpma bilgisi, menzil ve hedef katman)
        if (Physics.Raycast(ray.origin, ray.direction, out hit, attackRange, enemyLayer))
        {
            // 4. Çarptığımız objede "Enemy" componenti var mı kontrol et
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log("3D Düşman vuruldu: " + hit.collider.name);
                enemy.GetHit(); // Düşmandaki fonksiyonu çalıştır
            }
        }
        // Animasyonu oynat
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}