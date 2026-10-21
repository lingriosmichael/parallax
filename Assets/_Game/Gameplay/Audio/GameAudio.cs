using System.Collections;
using System.Collections.Generic;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-064 (D-158): a scene's sound. Synthesizes every cue once on Awake (or uses the bank's recording), loops the
    /// ambient bed, plays cues through a small pool of voices, and clicks for every button in the scene. Presentation only:
    /// nothing in gameplay reads it, and removing it changes no test. One per scene (Title, LevelSelect, every level via
    /// _LevelTemplate), placed by PARALLAX/Setup/Audio (PAX-064); not a singleton.</summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        public const int SfxRate = 44100, BedRate = 22050;

        [SerializeField] AudioCueBank bank;
        [SerializeField] bool playBed = true;
        [Tooltip("How many sounds may play at once.")]
        [SerializeField] int voices = 12;
        [Tooltip("Pitch walk between repeats of a cue, in semitones (a fixed pattern, not randomness).")]
        [SerializeField] float pitchWalk = 0.35f;
        [Tooltip("World units from the camera at which a sound is panned fully to one side.")]
        [SerializeField] float panWidth = 14f;
        [Tooltip("A sound placed in the world plays at full volume within this many units of the camera...")]
        [SerializeField] float fullWithin = 9f;
        [Tooltip("...and fades out by this distance (an off-screen launcher stays quiet).")]
        [SerializeField] float silentBeyond = 17f;

        readonly Dictionary<AudioCue, AudioClip> clips = new Dictionary<AudioCue, AudioClip>();
        readonly CueLimiter limiter = new CueLimiter();
        AudioSource[] pool;
        AudioSource bed;
        LevelPause pause;
        Transform listener;
        float bedStart;
        int plays;

        public AudioCueBank Bank => bank;
        public int Plays => plays;

        void Awake()
        {
            if (bank == null) { Debug.LogError("GameAudio: no AudioCueBank assigned; the scene will be silent. Run PARALLAX/Setup/Audio (PAX-064).", this); enabled = false; return; }
            EnsureListener();
            BuildClips();
            pool = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < pool.Length; i++) pool[i] = NewSource("Voice_" + i);
            pause = FindInScene<LevelPause>();
        }

        IEnumerator Start()
        {
            if (!enabled) yield break;
            if (playBed) StartBed();
            // A frame later, so buttons built in other Starts (the level select's rows) are there too.
            yield return null;
            foreach (Button button in FindAllInScene<Button>()) button.onClick.AddListener(PlayUiTap);
        }

        void Update()
        {
            if (bed == null) return;
            float fade = bank.BedFadeIn > 0f ? Mathf.Clamp01((Time.unscaledTime - bedStart) / bank.BedFadeIn) : 1f;
            float paused = pause != null && pause.IsPaused ? bank.PausedBed : 1f;
            bed.volume = bank.VolumeOf(AudioChannel.Music) * fade * paused;
        }

        /// <summary>Plays a cue (panned by its world x when given, and faded with its distance from the camera), unless it is
        /// muted, out of earshot, already sounding as often as it may, or started too recently. Returns whether it started.</summary>
        public bool Play(AudioCue cue, float? worldX = null, float? worldY = null)
        {
            if (!enabled || pool == null || !clips.TryGetValue(cue, out AudioClip clip) || clip == null) return false;
            float distance = 1f;
            if (worldX.HasValue && listener != null)
            {
                Vector2 away = new(worldX.Value - listener.position.x, worldY.HasValue ? worldY.Value - listener.position.y : 0f);
                distance = AudioCues.Falloff(away.magnitude, fullWithin, silentBeyond);
                if (distance <= 0f) return false;
            }
            CueSpec spec = SfxPresets.Spec(cue);
            float trim = 1f;
            if (bank.TryGetOverride(cue, out AudioCueBank.Override o))
            {
                if (o.Mute) return false;
                if (o.Volume > 0f) trim = o.Volume;
            }
            float pitch = AudioCues.PitchFor(plays, cue == AudioCue.LevelComplete || cue == AudioCue.UiTap ? 0f : pitchWalk);
            if (!limiter.TryStart((int)cue, Time.unscaledTime, clip.length / pitch, spec.MaxVoices, spec.MinGap)) return false;

            AudioSource voice = FreeVoice();
            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = spec.Volume * trim * distance * bank.VolumeOf(spec.Channel);
            voice.panStereo = worldX.HasValue && listener != null ? Mathf.Clamp((worldX.Value - listener.position.x) / panWidth, -0.6f, 0.6f) : 0f;
            voice.Play();
            plays++;
            return true;
        }

        /// <summary>A button click. It plays on a short-lived voice that outlives this scene, since most buttons load another.</summary>
        public void PlayUiTap()
        {
            if (!enabled || !clips.TryGetValue(AudioCue.UiTap, out AudioClip clip) || clip == null) return;
            CueSpec spec = SfxPresets.Spec(AudioCue.UiTap);
            float trim = 1f;
            if (bank.TryGetOverride(AudioCue.UiTap, out AudioCueBank.Override o)) { if (o.Mute) return; if (o.Volume > 0f) trim = o.Volume; }
            if (!limiter.TryStart((int)AudioCue.UiTap, Time.unscaledTime, clip.length, spec.MaxVoices, spec.MinGap)) return;
            var carry = new GameObject("UiTap");
            DontDestroyOnLoad(carry);
            AudioSource source = carry.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.clip = clip;
            source.volume = spec.Volume * trim * bank.VolumeOf(spec.Channel);
            source.Play();
            Destroy(carry, clip.length + 0.1f);
            plays++;
        }

        void BuildClips()
        {
            foreach (AudioCue cue in System.Enum.GetValues(typeof(AudioCue)))
            {
                if (bank.TryGetOverride(cue, out AudioCueBank.Override o) && o.Clip != null) { clips[cue] = o.Clip; continue; }
                float[] samples = SfxSynth.Render(SfxPresets.For(cue), SfxRate);
                if (samples.Length == 0) { Debug.LogWarning("GameAudio: cue " + cue + " rendered no samples; it stays silent.", this); continue; }
                clips[cue] = MakeClip(cue.ToString(), samples, SfxRate, false);
            }
        }

        void StartBed()
        {
            AudioClip clip = bank.BedClip;
            if (clip == null)
            {
                float[] samples = AmbientBed.Render(SfxPresets.Bed(), BedRate);
                if (samples.Length == 0) { Debug.LogWarning("GameAudio: the ambient bed rendered no samples.", this); return; }
                clip = MakeClip("AmbientBed", samples, BedRate, false);
            }
            bed = NewSource("Bed");
            bed.clip = clip;
            bed.loop = true;
            bed.volume = 0f;
            bed.ignoreListenerPause = true;
            bedStart = Time.unscaledTime;
            bed.Play();
        }

        static AudioClip MakeClip(string name, float[] samples, int rate, bool stream)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, stream);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioSource NewSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        // A free voice, else the one closest to finishing.
        AudioSource FreeVoice()
        {
            AudioSource best = pool[0];
            float bestLeft = float.MaxValue;
            foreach (AudioSource s in pool)
            {
                if (!s.isPlaying) return s;
                float left = s.clip != null ? (s.clip.length - s.time) / Mathf.Max(0.01f, s.pitch) : 0f;
                if (left < bestLeft) { bestLeft = left; best = s; }
            }
            best.Stop();
            return best;
        }

        // A scene needs exactly one AudioListener: the level camera carries one; a scene without one gets it here.
        void EnsureListener()
        {
            AudioListener found = null;
            foreach (AudioListener l in FindAllInScene<AudioListener>()) if (l.isActiveAndEnabled) { found = l; break; }
            if (found == null) found = gameObject.AddComponent<AudioListener>();
            listener = found.transform;
        }

        T FindInScene<T>() where T : Component
        {
            foreach (T c in FindAllInScene<T>()) return c;
            return null;
        }

        IEnumerable<T> FindAllInScene<T>() where T : Component
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) yield break;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (T c in root.GetComponentsInChildren<T>(true))
                    yield return c;
        }
    }
}
