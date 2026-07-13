using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("Flash Ayarları")]
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material whiteFlashMaterial;
    private Coroutine flashCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Düşmanın kendi çizimli orijinal materyalini kaydet
            originalMaterial = spriteRenderer.material;

            // Unity'nin içindeki, çizimleri tamamen beyaz/katı renge boyayan gizli materyali buluyoruz
            whiteFlashMaterial = new Material(Shader.Find("GUI/Text Shader"));
        }
    }

    public void Flash()
    {
        if (spriteRenderer == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Materyali tamamen katı beyaz yapan shader ile değiştir
        spriteRenderer.material = whiteFlashMaterial;
        spriteRenderer.color = Color.white; // İçindeki yazıyı/artı tamamen beyaz kütle yapar

        yield return new WaitForSeconds(flashDuration);

        // Süre bitince düşmanın kendi çizimli orijinal haline geri döndür
        spriteRenderer.material = originalMaterial;

        flashCoroutine = null;
    }
}