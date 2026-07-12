using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Can")]
    public int maxHealth = 5;

    [Header("Vurulunca")]
    public Color flashColor = Color.white;
    [Tooltip("GlowShader kullanan sprite'larda emission'ı ne kadar zorlasın.")]
    public float flashIntensity = 5f;
    public float flashDuration = 0.12f;

    public float squashAmount = 0.35f;
    public float squashDuration = 0.3f;

    public float knockbackDistance = 0.25f;
    public float knockbackDuration = 0.22f;

    [Header("Ölüm")]
    public bool respawn = true;
    public float respawnDelay = 2f;

    int _health;
    Renderer _renderer;

    // Squash ve knockback transform'u oynatıyor; nereye döneceğini bilmek için
    // dokunulmamış hallerini saklıyoruz. Yoksa arka arkaya vuruşlarda obje kayıyor.
    Vector3 _restScale;
    Vector3 _restPosition;

    Coroutine _flashRoutine;
    Coroutine _squashRoutine;
    Coroutine _knockRoutine;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _restScale = transform.localScale;
        _restPosition = transform.position;
        _health = maxHealth;
    }

    /// <summary>Yön bilgisi olmayan eski çağrılar için.</summary>
    public void GetHit() => GetHit(transform.position, Vector3.up, transform.forward);

    /// <param name="point">Çarpmanın olduğu dünya noktası.</param>
    /// <param name="normal">Çarpılan yüzeyin normali — partiküller buradan fışkırıyor.</param>
    /// <param name="direction">Oyuncudan düşmana doğru yatay yön — knockback ve sarsıntı yönü.</param>
    public void GetHit(Vector3 point, Vector3 normal, Vector3 direction)
    {
        if (_health <= 0) return;

        _health--;
        Debug.Log(gameObject.name + " vuruldu! Kalan can: " + _health);

        RestartFlash();
        RestartSquash();
        RestartKnockback(direction);

        if (_health <= 0)
        {
            Die(point);
            return;
        }

        if (HitJuice.Instance != null)
            HitJuice.Instance.Impact(point, normal, direction);
    }

    void Die(Vector3 point)
    {
        if (HitJuice.Instance != null)
            HitJuice.Instance.Death(point);

        SetVisible(false);

        if (respawn) StartCoroutine(RespawnRoutine());
        else Destroy(gameObject, 0.1f);
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        transform.position = _restPosition;
        transform.localScale = _restScale;
        _health = maxHealth;

        SetVisible(true);
    }

    /// <summary>Ölüyken raycast'in ölüye çarpmaması için collider da kapanıyor.</summary>
    void SetVisible(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = visible;
    }

    // --- Tween'leri yeniden başlatırken önce dokunulmamış hale döndür,
    //     yoksa yarıda kesilen tween objeyi kaymış/ezilmiş bırakıyor. ---

    void RestartFlash()
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(HitFlash.Flash(_renderer, flashColor, flashIntensity, flashDuration));
    }

    void RestartSquash()
    {
        if (_squashRoutine != null)
        {
            StopCoroutine(_squashRoutine);
            transform.localScale = _restScale;
        }
        _squashRoutine = StartCoroutine(HitFlash.Squash(transform, _restScale, squashAmount, squashDuration));
    }

    void RestartKnockback(Vector3 direction)
    {
        if (_knockRoutine != null)
        {
            StopCoroutine(_knockRoutine);
            transform.position = _restPosition;
        }
        _knockRoutine = StartCoroutine(HitFlash.Knockback(transform, direction, knockbackDistance, knockbackDuration));
    }
}
