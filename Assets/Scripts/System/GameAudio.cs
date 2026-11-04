using UnityEngine;
using UnityEngine.SceneManagement;

// One persistent music source and a fixed voice budget; no scene/prefab wiring is required.
public sealed class GameAudio : MonoBehaviour
{
    internal const int VoiceCount = 12;
    private static GameAudio active;
    private GameAudioBank bank;
    private AudioSource music;
    private AudioListener fallbackListener;
    private readonly AudioSource[] voices = new AudioSource[VoiceCount];
    private readonly bool[] uiVoices = new bool[VoiceCount];
    private float[] nextAllowed;
    private int nextVoice;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (active != null) return;
        var data = Resources.Load<GameAudioBank>("GameAudio");
        if (data == null) { Debug.LogWarning("GameAudio bank missing. Run Tools/System Leveling/Setup Game Audio."); return; }
        active = new GameObject("Game Audio").AddComponent<GameAudio>();
        DontDestroyOnLoad(active.gameObject);
        active.Initialize(data);
        SceneManager.sceneLoaded += active.SceneLoaded;
        active.SetSceneMusic(SceneManager.GetActiveScene().path);
        active.RefreshListener();
    }

    internal void Initialize(GameAudioBank data)
    {
        bank = data;
        fallbackListener = gameObject.AddComponent<AudioListener>(); fallbackListener.enabled = false;
        nextAllowed = new float[System.Enum.GetValues(typeof(GameSound)).Length];
        for (int i = 0; i < nextAllowed.Length; i++) nextAllowed[i] = float.NegativeInfinity;
        music = NewSource(gameObject); music.loop = true; music.volume = 0.28f; music.priority = 128;
        for (int i = 0; i < VoiceCount; i++) voices[i] = NewSource(gameObject);
    }

    private static AudioSource NewSource(GameObject owner)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0f;
        return source;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        for (int i = 0; i < VoiceCount; i++) if (!uiVoices[i]) voices[i].Stop();
        SetSceneMusic(scene.path);
        RefreshListener();
    }

    internal void RefreshListener()
    {
        // Menu cameras have no listener; gameplay cameras already supply one.
        fallbackListener.enabled = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if (listener != fallbackListener && listener.isActiveAndEnabled) return;
        fallbackListener.enabled = true;
    }

    internal void SetSceneMusic(string path)
    {
        AudioClip clip = bank.MusicFor(path);
        if (music.clip == clip) return;
        music.Stop(); music.clip = clip;
        if (clip != null && Application.isPlaying) music.Play();
    }

    private void Update() => UpdateMix(Time.timeScale <= 0f || GameOverController.GameplayStopped, Time.unscaledDeltaTime);

    internal void UpdateMix(bool paused, float delta)
    {
        music.volume = Mathf.MoveTowards(music.volume, paused ? 0.12f : 0.28f, delta * 0.4f);
        if (paused)
            for (int i = 0; i < VoiceCount; i++) if (!uiVoices[i] && voices[i].isPlaying) voices[i].Stop();
    }

    public static void Play(GameSound sound)
    {
        if (!Application.isPlaying || active == null) return;
        if (!GameAudioBank.IsUI(sound) && (Time.timeScale <= 0f || GameOverController.GameplayStopped)) return;
        active.PlaySound(sound, Time.unscaledTime);
    }

    internal bool PlaySound(GameSound sound, float now)
    {
        AudioClip clip = bank.Clip(sound);
        if (clip == null || now < nextAllowed[(int)sound]) return false;
        // Pickup/hit bursts must not drown out attacks or UI feedback.
        nextAllowed[(int)sound] = now + (sound == GameSound.Experience || sound == GameSound.Hit || sound == GameSound.ArrowHit || sound == GameSound.MagicHit ? 0.07f : 0.04f);
        int slot = nextVoice;
        nextVoice = (nextVoice + 1) % VoiceCount;
        AudioSource voice = voices[slot];
        voice.Stop(); voice.clip = clip;
        uiVoices[slot] = GameAudioBank.IsUI(sound);
        voice.volume = sound == GameSound.Hover || sound == GameSound.Experience ? 0.22f : 0.55f;
        voice.priority = uiVoices[slot] || sound == GameSound.PlayerHurt ? 64 : 128;
        if (Application.isPlaying) voice.Play();
        return true;
    }

    public static void Charge(ref AudioSource source, GameObject owner, bool charging)
    {
        if (!charging || !Application.isPlaying || active == null)
        {
            if (source != null) source.Stop();
            return;
        }
        if (source == null)
        {
            source = NewSource(owner); source.loop = true; source.volume = 0.3f;
            source.clip = active.bank.Clip(GameSound.Charge);
        }
        if (!source.isPlaying && source.clip != null) source.Play();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (active == this) active = null;
    }
}
