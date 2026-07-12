using System.Collections;
using UnityEngine;

/// <summary>
/// Vurulan objeye uygulanan tepkiler: parlama, ezilip yaylanma, geri itilme.
/// Coroutine döndürüyorlar, çağıran MonoBehaviour StartCoroutine ile çalıştırır.
/// </summary>
public static class HitFlash
{
    /// <summary>
    /// Renderer'ı kısa süre parlatır.
    ///
    /// MaterialPropertyBlock KULLANMIYORUZ: Lamb'in sprite'ı _GlowTex'i secondary texture
    /// olarak bağlıyor ve SpriteRenderer'a SetPropertyBlock çağırmak o bağlamayı ezebiliyor.
    /// renderer.material per-renderer bir kopya üretir; paylaşılan materyal asset'i güvende kalır.
    /// </summary>
    public static IEnumerator Flash(Renderer renderer, Color color, float intensity, float duration)
    {
        if (renderer == null || duration <= 0f) yield break;

        Material mat = renderer.material;

        // Hangi shader'a denk geldiğimizi bilmiyoruz: Cube URP/Lit (_BaseColor),
        // sprite'lar ise GlowShader (_GlowStrength, Emission'ı sürüyor).
        bool hasBaseColor = mat.HasProperty("_BaseColor");
        bool hasGlow = mat.HasProperty("_GlowStrength");
        bool hasColor = mat.HasProperty("_Color");

        Color baseColor = hasBaseColor ? mat.GetColor("_BaseColor") : default;
        Color plainColor = hasColor ? mat.GetColor("_Color") : default;
        float baseGlow = hasGlow ? mat.GetFloat("_GlowStrength") : 0f;

        float t = 0f;
        while (t < duration)
        {
            // Anında zirveye çıkıp sönmesi, yavaşça yükselmesinden çok daha sert hissettiriyor.
            float k = 1f - (t / duration);

            if (hasBaseColor) mat.SetColor("_BaseColor", Color.Lerp(baseColor, color, k));
            else if (hasColor) mat.SetColor("_Color", Color.Lerp(plainColor, color, k));

            if (hasGlow) mat.SetFloat("_GlowStrength", baseGlow + intensity * k);

            t += Time.deltaTime;
            yield return null;
        }

        if (hasBaseColor) mat.SetColor("_BaseColor", baseColor);
        else if (hasColor) mat.SetColor("_Color", plainColor);
        if (hasGlow) mat.SetFloat("_GlowStrength", baseGlow);
    }

    /// <summary>Ezilip fazlasıyla geri yaylanır, sonra yerine oturur.</summary>
    public static IEnumerator Squash(Transform target, Vector3 baseScale, float amount, float duration)
    {
        if (target == null || duration <= 0f) yield break;

        float t = 0f;
        while (t < duration)
        {
            float k = t / duration;

            // Sönümlü sinüs: bir kez sert ezilir, sonra azalarak salınır.
            float wave = Mathf.Sin(k * Mathf.PI * 2f) * Mathf.Exp(-k * 4f);

            // Yatayda genişlerken dikeyde inceliyor; hacim korunuyormuş gibi görünüyor.
            target.localScale = new Vector3(
                baseScale.x * (1f + amount * wave),
                baseScale.y * (1f - amount * wave),
                baseScale.z * (1f + amount * wave));

            t += Time.deltaTime;
            yield return null;
        }

        target.localScale = baseScale;
    }

    /// <summary>Vuruş yönünde iter, sonra yerine geri çeker. (Cube'da Rigidbody yok, o yüzden transform ile.)</summary>
    public static IEnumerator Knockback(Transform target, Vector3 direction, float distance, float duration)
    {
        if (target == null || duration <= 0f || distance <= 0f) yield break;

        Vector3 start = target.position;
        Vector3 peak = start + direction.normalized * distance;

        float t = 0f;
        while (t < duration)
        {
            float k = t / duration;

            // Hızlı git, yavaş dön: geri çekilme itilmeden uzun sürünce daha ağır hissettiriyor.
            float k2 = k < 0.3f
                ? Mathf.Sin((k / 0.3f) * Mathf.PI * 0.5f)
                : Mathf.Cos(((k - 0.3f) / 0.7f) * Mathf.PI * 0.5f);

            target.position = Vector3.LerpUnclamped(start, peak, k2);

            t += Time.deltaTime;
            yield return null;
        }

        target.position = start;
    }
}
