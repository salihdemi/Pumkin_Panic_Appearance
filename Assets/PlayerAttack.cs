using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public Animator animator;

    public InputActionAsset InputActions;
    private InputActionMap actionMap;
    private InputAction attack;

    // Kameradan haritaya uzanan toplam ışın menzili (Kamera uzaksa yüksek tutulmalı)
    public float rayDistance = 500f;
    // Oyuncunun düşmana vurabileceği maksimum mesafe (Menzil kontrolü için)
    public float maxAttackRange = 5f;

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
        // 1. Farenin ekrandaki pozisyonunu al
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        // 2. Kameradan farenin olduğu noktaya doğru ışın oluştur
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        RaycastHit hit;

        // Görselleştirmeyi güncelleyelim
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red, 0.5f);

        // 3. Işını fırlat (Sadece enemyLayer katmanındaki objelere çarpar)
        if (Physics.Raycast(ray, out hit, rayDistance, enemyLayer))
        {
            // 4. Mesafe Kontrolü: Çarpılan nokta ile OYUNCU arasındaki mesafeyi ölçüyoruz
            float distanceToEnemy = Vector3.Distance(transform.position, hit.point);

            if (distanceToEnemy <= maxAttackRange)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    Debug.Log("Düşman menzil içinde ve vuruldu: " + hit.collider.name);
                    enemy.GetHit();
                    enemy.GetComponent<JuicyEffect>().TriggerDefaultSquashStretch();
                }
            }
            else
            {
                Debug.Log("Düşmana tıklandı ama oyuncuya çok uzak! Mesafe: " + distanceToEnemy);
            }
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }
}