using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    // Singleton yapısı: Her yerden kolayca çağırabilmek için
    public static HitStopManager Instance { get; private set; }

    private bool isWaiting = false;
    public float defaultHitStopDuration = 0.1f; // Varsayılan darbe durdurma süresi

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Bu fonksiyonu darbe anında çağıracağız
    public void TriggerDefaultHitStop()
    {
        TriggerHitStop(defaultHitStopDuration);
    }
    public void TriggerHitStop(float duration)
    {
        if (isWaiting) return; // Eğer zaten donmuşsa üst üste binmesin
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        isWaiting = true;

        // Zamanı neredeyse tamamen durdur (0 yaparsak bazı fizik/kod sorunları olabilir, 0.02 idealdir)
        Time.timeScale = 0.02f;

        // Zaman durduğu için normal 'WaitForSeconds' çalışmaz. 'WaitForSecondsRealtime' kullanmalıyız!
        yield return new WaitForSecondsRealtime(duration);

        // Zamanı normale döndür
        Time.timeScale = 1.0f;
        isWaiting = false;
    }
}