using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodigoECavaleiros.Audio
{
    /// <summary>
    /// Toca música e efeitos. Nasce sozinho antes da primeira cena e sobrevive às trocas de cena.
    /// Os clipes ficam em Resources/Audio (gerados por Tools → Código &amp; Cavaleiros → Gerar Áudio).
    /// Tecla M liga/desliga o som.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const float MusicVolume = 0.35f;
        const float SfxVolume = 0.8f;

        static AudioManager instance;
        AudioSource music;
        AudioSource[] sfxSources;
        int nextSfx;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        bool muted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
            instance.Init();
        }

        void Init()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = MusicVolume;
            sfxSources = new AudioSource[6];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
            muted = PlayerPrefs.GetInt("muted", 0) == 1;
            AudioListener.pause = false;
            Apply();
        }

        void Apply()
        {
            music.mute = muted;
            foreach (var s in sfxSources) s.mute = muted;
        }

        AudioClip Get(string name)
        {
            AudioClip c;
            if (cache.TryGetValue(name, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning("[AudioManager] Clipe não encontrado: " + name);
            cache[name] = c;
            return c;
        }

        // ------------------------------------------------------------------ API estática
        public static void Sfx(string name, float volume = 1f)
        {
            if (instance == null) Boot();
            var c = instance.Get(name);
            if (c == null) return;
            var src = instance.sfxSources[instance.nextSfx];
            instance.nextSfx = (instance.nextSfx + 1) % instance.sfxSources.Length;
            src.PlayOneShot(c, SfxVolume * volume);
        }

        public static void Music(string name)
        {
            if (instance == null) Boot();
            var c = instance.Get(name);
            if (c == null) return;
            if (instance.music.clip == c && instance.music.isPlaying) return;
            instance.music.clip = c;
            instance.music.Play();
        }

        public static void StopMusic()
        {
            if (instance == null) return;
            instance.music.Stop();
            instance.music.clip = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[Key.M].wasPressedThisFrame)
            {
                muted = !muted;
                PlayerPrefs.SetInt("muted", muted ? 1 : 0);
                Apply();
            }
        }
    }
}
