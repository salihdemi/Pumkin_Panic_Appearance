using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Vuruş hissini oluşturan her şeyin tek durağı: hit-stop, screen shake, partikül,
/// ışık patlaması, FOV punch, post-process pulse ve ses.
///
/// Lamb'in üzerinde duruyor. PlayerAttack isabet ettiğinde Impact(), ıskaladığında Whiff() çağırıyor.
/// </summary>
[DisallowMultipleComponent]
public class HitJuice : MonoBehaviour
{
    public static HitJuice Instance { get; private set; }

    [Header("Hit-stop (donma karesi)")]
    [Tooltip("Vuruş anında zamanın kısılma süresi (gerçek saniye).")]
    public float hitStopDuration = 0.06f;
    [Tooltip("0 = tam donma, 1 = etkisiz.")]
    [Range(0f, 1f)] public float hitStopScale = 0.05f;

    [Header("Screen shake (Cinemachine Impulse)")]
    public float shakeForce = 0.35f;
    public float whiffShakeForce = 0.07f;
    public float impulseDuration = 0.25f;
    [Tooltip("Kameradaki dinleyicinin kazancı. 0 olursa sarsıntı hiç görünmez.")]
    public float listenerGain = 1f;

    [Header("Kamera")]
    [Tooltip("Vuruşta kameranın bir anlık içeri zoomlaması (derece).")]
    public float fovPunch = 5f;
    public float fovPunchDuration = 0.22f;

    [Header("Post-process pulse")]
    [Tooltip("Vignette + bloom + chromatic aberration'ın anlık şiddeti.")]
    [Range(0f, 1f)] public float postPulseStrength = 0.85f;
    public float postPulseDuration = 0.28f;

    [Header("Işık patlaması")]
    public float flashIntensity = 12f;
    public float flashRange = 4f;
    public float flashDuration = 0.14f;
    public Color flashColor = new Color(1f, 0.92f, 0.7f);

    [Header("Partikül")]
    public int sparkCount = 22;
    public int dustCount = 8;
    public Color sparkColor = new Color(1f, 0.85f, 0.45f);

    [Header("Ses")]
    public bool playSound = true;
    [Range(0f, 1f)] public float hitVolume = 0.7f;
    [Range(0f, 1f)] public float whiffVolume = 0.25f;

    CinemachineImpulseSource _impulse;
    CinemachineCamera _vcam;
    ImpactVFX _vfx;
    Light _flashLight;
    Volume _volume;
    AudioSource _audio;

    float _baseFov;
    float _baseFixedDeltaTime;
    bool _hitStopping;

    Coroutine _fovRoutine;
    Coroutine _postRoutine;
    Coroutine _flashRoutine;

    void Awake()
    {
        Instance = this;
        _baseFixedDeltaTime = Time.fixedDeltaTime;

        SetupImpulse();
        SetupCamera();
        SetupAudio();

        _vfx = ImpactVFX.Create("Juice VFX", sparkCount, dustCount, sparkColor);
        _flashLight = BuildFlashLight();
        _volume = BuildPulseVolume();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Hit-stop sırasında Play'den çıkarsan zaman yavaş kalmasın.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _baseFixedDeltaTime;
    }

    // --- Dışarıya açık API ---

    /// <summary>Düşmana isabet. direction = oyuncudan hedefe doğru (yatay).</summary>
    public void Impact(Vector3 point, Vector3 normal, Vector3 direction)
    {
        StartCoroutine(HitStopRoutine(hitStopDuration, hitStopScale));

        Shake(direction, shakeForce);

        if (_vfx != null) _vfx.Play(point, normal);

        Restart(ref _flashRoutine, FlashRoutine(point));
        Restart(ref _fovRoutine, FovPunchRoutine());
        Restart(ref _postRoutine, PostPulseRoutine());

        Play(ProceduralSfx.Hit, hitVolume);
    }

    /// <summary>Boşa sallama. Hit-stop yok, sadece hafif bir his.</summary>
    public void Whiff(Vector3 direction)
    {
        Shake(direction, whiffShakeForce);
        Play(ProceduralSfx.Whiff, whiffVolume);
    }

    /// <summary>Düşman öldü — her şeyin dozu artıyor.</summary>
    public void Death(Vector3 point)
    {
        StartCoroutine(HitStopRoutine(hitStopDuration * 2f, hitStopScale));

        Shake(Random.onUnitSphere, shakeForce * 2f);

        if (_vfx != null)
        {
            // Üç ayrı yöne patlat: tek burst'ten çok daha dolu görünüyor.
            _vfx.Play(point, Vector3.up);
            _vfx.Play(point, Vector3.left);
            _vfx.Play(point, Vector3.right);
        }

        Restart(ref _flashRoutine, FlashRoutine(point));
        Restart(ref _postRoutine, PostPulseRoutine());

        Play(ProceduralSfx.Death, hitVolume);
    }

    // --- Kurulum ---

    void SetupImpulse()
    {
        _impulse = GetComponent<CinemachineImpulseSource>();
        if (_impulse == null) _impulse = gameObject.AddComponent<CinemachineImpulseSource>();

        var def = _impulse.ImpulseDefinition;
        def.ImpulseChannel = 1;
        def.ImpulseDuration = impulseDuration;
        def.AmplitudeGain = 1f;
        def.FrequencyGain = 1f;
        _impulse.ImpulseDefinition = def;
    }

    void SetupCamera()
    {
        _vcam = FindFirstObjectByType<CinemachineCamera>();
        if (_vcam == null)
        {
            Debug.LogWarning("HitJuice: CinemachineCamera bulunamadı, sarsıntı ve FOV punch devre dışı.");
            return;
        }

        _baseFov = _vcam.Lens.FieldOfView;

        var listener = _vcam.GetComponent<CinemachineImpulseListener>();
        if (listener == null) listener = _vcam.gameObject.AddComponent<CinemachineImpulseListener>();

        // TUZAK: CinemachineImpulseListener'da Gain ve ChannelMask'in C# initializer'ı yok.
        // Bunları sadece editördeki Reset() 1 yapıyor; sahneden deserialize edilirken 0 gelebiliyorlar
        // ve o zaman sarsıntı sessizce hiç çalışmıyor. Burada zorluyoruz.
        listener.ChannelMask = 1;
        listener.Gain = listenerGain;

        if (listener.ReactionSettings.Duration <= 0f)
        {
            var reaction = listener.ReactionSettings;
            reaction.AmplitudeGain = 1f;
            reaction.FrequencyGain = 1f;
            reaction.Duration = 1f;
            listener.ReactionSettings = reaction;
        }
    }

    void SetupAudio()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;   // 2D: mesafeye göre kısılmasın.
    }

    Light BuildFlashLight()
    {
        var go = new GameObject("Juice Flash Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = flashColor;
        light.range = flashRange;
        light.intensity = 0f;
        light.enabled = false;
        return light;
    }

    /// <summary>
    /// Sahnedeki "Global Volume Profile" paylaşılan bir asset — runtime'da onu kurcalarsak
    /// diskteki dosyayı kirletme riski var. Onun yerine kendi volume'umuzu kuruyoruz;
    /// daha yüksek priority ile üstüne binip ağırlığını tween'liyoruz.
    /// Chromatic aberration da böylece asset'e hiç dokunmadan gelmiş oluyor.
    /// </summary>
    Volume BuildPulseVolume()
    {
        var go = new GameObject("Juice Volume");
        go.layer = 0;   // Kameranın Volume Mask'i sadece Default layer'ı görüyor.

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.DontSave;

        var vignette = profile.Add<Vignette>();
        vignette.intensity.Override(0.62f);
        vignette.color.Override(Color.black);

        var bloom = profile.Add<Bloom>();
        bloom.intensity.Override(4f);

        var aberration = profile.Add<ChromaticAberration>();
        aberration.intensity.Override(0.8f);

        volume.sharedProfile = profile;
        return volume;
    }

    // --- Efektler ---

    void Shake(Vector3 direction, float force)
    {
        if (_impulse == null || force <= 0f) return;

        Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Random.onUnitSphere;

        // Tamamen düz bir sarsıntı mekanik duruyor; azıcık rastgelelik canlandırıyor.
        Vector3 velocity = (dir + Random.insideUnitSphere * 0.35f).normalized * force;

        _impulse.GenerateImpulseAtPositionWithVelocity(transform.position, velocity);
    }

    IEnumerator HitStopRoutine(float duration, float scale)
    {
        if (_hitStopping || duration <= 0f) yield break;
        _hitStopping = true;

        Time.timeScale = scale;
        Time.fixedDeltaTime = _baseFixedDeltaTime * scale;   // Fizik de yavaşlasın, yoksa tekliyor.

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = _baseFixedDeltaTime;
        _hitStopping = false;
    }

    // Aşağıdaki tween'ler unscaled zaman kullanıyor: hit-stop sırasında zaman dursa bile
    // ışık, FOV ve post-process akmaya devam etsin ki donma karesi ölü görünmesin.

    IEnumerator FlashRoutine(Vector3 point)
    {
        if (_flashLight == null || flashDuration <= 0f) yield break;

        _flashLight.transform.position = point;
        _flashLight.color = flashColor;
        _flashLight.range = flashRange;
        _flashLight.enabled = true;

        float t = 0f;
        while (t < flashDuration)
        {
            float k = 1f - (t / flashDuration);
            _flashLight.intensity = flashIntensity * k * k;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _flashLight.intensity = 0f;
        _flashLight.enabled = false;
    }

    IEnumerator FovPunchRoutine()
    {
        if (_vcam == null || fovPunch <= 0f || fovPunchDuration <= 0f) yield break;

        float t = 0f;
        while (t < fovPunchDuration)
        {
            float k = 1f - (t / fovPunchDuration);

            // Küçük FOV = içeri zoom.
            _vcam.Lens.FieldOfView = _baseFov - fovPunch * k * k;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _vcam.Lens.FieldOfView = _baseFov;
    }

    IEnumerator PostPulseRoutine()
    {
        if (_volume == null || postPulseDuration <= 0f) yield break;

        float t = 0f;
        while (t < postPulseDuration)
        {
            float k = 1f - (t / postPulseDuration);
            _volume.weight = postPulseStrength * k * k;

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _volume.weight = 0f;
    }

    // --- Yardımcılar ---

    void Play(AudioClip clip, float volume)
    {
        if (!playSound || _audio == null || clip == null) return;
        _audio.PlayOneShot(clip, volume);
    }

    /// <summary>Arka arkaya vuruşlarda tween'ler üst üste binmesin; öncekini kesip baştan başlat.</summary>
    void Restart(ref Coroutine slot, IEnumerator routine)
    {
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(routine);
    }
}
