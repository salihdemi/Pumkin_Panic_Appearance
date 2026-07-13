using System.Collections;
using UnityEngine;

public class JuicyEffect : MonoBehaviour
{
    [Header("Squash & Stretch Ayarları")]
    [SerializeField] private AnimationCurve squashCurve; // Esnekliğin şekli
    [SerializeField] private float duration = 0.2f;      // Ne kadar sürecek?

    private Vector3 originalScale;

    public float DefaultStrengthX = 0.2f; // Varsayılan X ekseni esnekliği
    public float DefaultStrengthY = 0.2f; // Varsayılan X ekseni esnekliği

    void Start()
    {
        // Karakterin orijinal boyutunu kaydet (örn: 1, 1, 1)
        originalScale = transform.localScale;
    }

    // Bu fonksiyonu dışarıdan çağıracağız
    public void TriggerDefaultSquashStretch()
    {
        TriggerSquashStretch(DefaultStrengthX, DefaultStrengthY);
    }
    public void TriggerSquashStretch(float strengthX, float strengthY)
    {
        StopAllCoroutines(); // Eğer üst üste binerse öncekini durdur
        StartCoroutine(SquashRoutine(strengthX, strengthY));
    }

    private IEnumerator SquashRoutine(float strengthX, float strengthY)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float percent = elapsedTime / duration;

            // Animasyon eğrisinden o anki değeri al (0 ile 1 arasında)
            float curveValue = squashCurve.Evaluate(percent);

            // Yeni boyutları hesapla
            float newX = originalScale.x + (curveValue * strengthX);
            float newY = originalScale.y + (curveValue * strengthY);

            transform.localScale = new Vector3(newX, newY, originalScale.z);

            yield return null;
        }

        // Süre bitince orijinal boyuta kusursuz dön
        transform.localScale = originalScale;
    }
}