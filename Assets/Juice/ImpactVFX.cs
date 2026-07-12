using UnityEngine;

/// <summary>
/// Çarpma partikülleri. Projede tek bir partikül dokusu bile olmadığı için
/// hem dokular hem de ParticleSystem'ler tamamen kodla kuruluyor — asset gerekmiyor.
///
/// Üç katman: kıvılcımlar (yön), genişleyen halka (darbe), toz (ağırlık).
/// </summary>
[DisallowMultipleComponent]
public class ImpactVFX : MonoBehaviour
{
    ParticleSystem _sparks;
    ParticleSystem _ring;
    ParticleSystem _dust;

    int _sparkCount;
    int _dustCount;

    // Dokular ve materyaller tüm örnekler arasında paylaşılıyor, bir kez üretiliyor.
    static Material _dotMaterial;
    static Material _ringMaterial;

    public static ImpactVFX Create(string name, int sparkCount, int dustCount, Color sparkColor)
    {
        var go = new GameObject(name);
        var vfx = go.AddComponent<ImpactVFX>();
        vfx.Build(sparkCount, dustCount, sparkColor);
        return vfx;
    }

    /// <summary>Çarpma noktasına taşı, yüzey normali boyunca fışkırt.</summary>
    public void Play(Vector3 point, Vector3 normal)
    {
        transform.position = point;
        transform.rotation = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(normal)   // Koni şekli lokal +Z boyunca yayar.
            : Quaternion.identity;

        if (_sparks != null) _sparks.Emit(_sparkCount);
        if (_ring != null) _ring.Emit(1);
        if (_dust != null) _dust.Emit(_dustCount);
    }

    void Build(int sparkCount, int dustCount, Color sparkColor)
    {
        _sparkCount = sparkCount;
        _dustCount = dustCount;

        _sparks = BuildSparks(sparkColor);
        _ring = BuildRing();
        _dust = BuildDust();
    }

    ParticleSystem BuildSparks(Color color)
    {
        var ps = NewSystem("Sparks", DotMaterial, ParticleSystemRenderMode.Stretch);

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
        main.gravityModifier = 1.6f;
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 38f;
        shape.radius = 0.03f;

        FadeOut(ps);
        Shrink(ps, 1f, 0.2f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.velocityScale = 0.06f;   // Hıza göre uzayıp çizgi gibi görünsünler.
        r.lengthScale = 2f;

        return ps;
    }

    ParticleSystem BuildRing()
    {
        var ps = NewSystem("Ring", RingMaterial, ParticleSystemRenderMode.Billboard);

        var main = ps.main;
        main.startLifetime = 0.22f;
        main.startSpeed = 0f;
        main.startSize = 0.35f;
        main.gravityModifier = 0f;
        main.startColor = Color.white;

        var shape = ps.shape;
        shape.enabled = false;   // Tam çarpma noktasında doğsun.

        FadeOut(ps);
        Shrink(ps, 0.25f, 1.6f);   // Küçük başlayıp hızla açılan şok dalgası.

        return ps;
    }

    ParticleSystem BuildDust()
    {
        var ps = NewSystem("Dust", DotMaterial, ParticleSystemRenderMode.Billboard);

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.gravityModifier = -0.15f;   // Hafifçe yükselsin.
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.82f, 0.75f, 0.5f),
            new Color(0.6f, 0.58f, 0.55f, 0.35f));

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        FadeOut(ps);
        Shrink(ps, 0.7f, 1.3f);   // Toz dağılırken büyür.

        return ps;
    }

    /// <summary>
    /// Sürekli "çalışan" ama saniyede 0 partikül üreten bir sistem.
    /// Böylece Emit() ile istediğimiz anda patlatabiliyoruz ve arka arkaya
    /// gelen vuruşlar birbirinin partiküllerini kesmiyor.
    /// </summary>
    ParticleSystem NewSystem(string name, Material material, ParticleSystemRenderMode renderMode)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = renderMode;
        r.material = material;
        r.sortingOrder = 100;   // Sahnedeki sprite'ların üstünde kalsınlar.

        ps.Play();
        return ps;
    }

    static void FadeOut(ParticleSystem ps)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.35f), new GradientAlphaKey(0f, 1f) });

        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    static void Shrink(ParticleSystem ps, float from, float to)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
    }

    // --- Dokular ve materyaller ---

    static Material DotMaterial
    {
        get
        {
            if (_dotMaterial == null) _dotMaterial = MakeMaterial(MakeDotTexture(64));
            return _dotMaterial;
        }
    }

    static Material RingMaterial
    {
        get
        {
            if (_ringMaterial == null) _ringMaterial = MakeMaterial(MakeRingTexture(128));
            return _ringMaterial;
        }
    }

    /// <summary>
    /// Sprites/Default: alpha-blend yapar ve dokuyu vertex rengiyle çarpar —
    /// yani partikülün start color'ı doğrudan işe yarar. Ekstra keyword ayarı gerekmez.
    ///
    /// NOT: Shader.Find editörde sorunsuz çalışır. Build alırsan bu shader'ı
    /// Project Settings > Graphics > Always Included Shaders listesine eklemen gerekir.
    /// </summary>
    static Material MakeMaterial(Texture2D tex)
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("ImpactVFX: 'Sprites/Default' shader'ı bulunamadı, partiküller görünmeyebilir.");
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        var mat = new Material(shader) { name = "JuiceParticle", hideFlags = HideFlags.DontSave };
        mat.mainTexture = tex;
        return mat;
    }

    /// <summary>Merkezi parlak, kenarları yumuşakça sönen bir nokta.</summary>
    static Texture2D MakeDotTexture(int size)
    {
        var tex = NewTexture(size, "JuiceDot");
        var pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;
            float d = Mathf.Sqrt(dx * dx + dy * dy);

            float a = Mathf.Clamp01(1f - d);
            a = a * a;   // Kareleyince kenar daha çabuk sönüyor, çekirdek daha sıkı görünüyor.

            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>İçi boş halka — genişleyen şok dalgası için.</summary>
    static Texture2D MakeRingTexture(int size)
    {
        var tex = NewTexture(size, "JuiceRing");
        var pixels = new Color[size * size];
        float half = size * 0.5f;

        const float radius = 0.72f;
        const float thickness = 0.20f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;
            float d = Mathf.Sqrt(dx * dx + dy * dy);

            // Halka çizgisinden uzaklaştıkça sön.
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - radius) / thickness);
            a = a * a;

            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D NewTexture(int size, string name)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }
}
