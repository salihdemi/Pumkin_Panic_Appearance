using UnityEngine;

/// <summary>
/// Projede hiç ses dosyası yok, o yüzden vuruş seslerini örnek örnek kodla üretiyoruz.
/// Klipler ilk istendiğinde bir kez üretilip saklanıyor.
/// </summary>
public static class ProceduralSfx
{
    const int SampleRate = 44100;

    static AudioClip _hit;
    static AudioClip _whiff;
    static AudioClip _death;

    public static AudioClip Hit => _hit != null ? _hit : (_hit = BuildHit());
    public static AudioClip Whiff => _whiff != null ? _whiff : (_whiff = BuildWhiff());
    public static AudioClip Death => _death != null ? _death : (_death = BuildDeath());

    /// <summary>Sert bir "thwack": gürültü transient'i + hızla düşen bas sinüs.</summary>
    static AudioClip BuildHit()
    {
        float length = 0.20f;
        int count = (int)(SampleRate * length);
        var data = new float[count];
        var rng = new System.Random(1337);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;

            // Çarpma anının "tık"ı: çok hızlı sönen beyaz gürültü.
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float crack = noise * Mathf.Exp(-t * 90f) * 0.7f;

            // Gövde: 190 Hz'den 55 Hz'e düşen sinüs, ağırlık hissi veriyor.
            float freq = Mathf.Lerp(190f, 55f, Mathf.Clamp01(t / length));
            float body = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 16f) * 0.8f;

            data[i] = SoftClip(crack + body);
        }

        return Make("JuiceHit", data);
    }

    /// <summary>Iska: havada süzülen kısa bir "swish".</summary>
    static AudioClip BuildWhiff()
    {
        float length = 0.14f;
        int count = (int)(SampleRate * length);
        var data = new float[count];
        var rng = new System.Random(4242);

        // Tek kutuplu alçak geçiren filtre; katsayısını zamanla kapatınca "süzülme" hissi çıkıyor.
        float lp = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float k = Mathf.Clamp01(t / length);

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            float cutoff = Mathf.Lerp(0.55f, 0.05f, k);
            lp += (noise - lp) * cutoff;

            // Önce yükselip sonra sönen zarf: sallanan bir kolun sesi.
            float env = Mathf.Sin(Mathf.PI * k) * Mathf.Exp(-t * 6f);

            data[i] = SoftClip(lp * env * 0.55f);
        }

        return Make("JuiceWhiff", data);
    }

    /// <summary>Ölüm: daha derin, daha uzun bir çöküş.</summary>
    static AudioClip BuildDeath()
    {
        float length = 0.45f;
        int count = (int)(SampleRate * length);
        var data = new float[count];
        var rng = new System.Random(909);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float k = Mathf.Clamp01(t / length);

            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float crack = noise * Mathf.Exp(-t * 25f) * 0.5f;

            float freq = Mathf.Lerp(140f, 30f, k);
            float body = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 6f);

            data[i] = SoftClip(crack + body);
        }

        return Make("JuiceDeath", data);
    }

    /// <summary>Toplama sonrası tepeleri kırpmak yerine yumuşatır; "digital clipping" çirkin duyulur.</summary>
    static float SoftClip(float x) => (float)System.Math.Tanh(x * 1.4);

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
