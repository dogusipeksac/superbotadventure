using System.Collections;
using SuperBot.Core;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.Audio
{
    /// <summary>Music with cross-fade + a small pool of SFX sources. Missing clips are silently skipped.</summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int SfxVoices = 8;

        private AudioLibrary library;
        private AudioSource musicA, musicB;
        private readonly AudioSource[] voices = new AudioSource[SfxVoices];
        private int nextVoice;
        private MusicId currentMusic = MusicId.None;
        private float currentMusicBaseVolume = 0.6f;
        private Coroutine musicFade;

        public void Init(AudioLibrary lib)
        {
            library = lib;
            ServiceLocator.Register(this);
            musicA = CreateSource("MusicA", true);
            musicB = CreateSource("MusicB", true);
            for (int i = 0; i < SfxVoices; i++) voices[i] = CreateSource("Sfx" + i, false);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        private AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            if (loop) src.ignoreListenerPause = true;
            return src;
        }

        private static float SfxVolume => ServiceLocator.TryGet(out SaveService s) ? s.Data.sfxVolume : 1f;
        private static float MusicVolume => ServiceLocator.TryGet(out SaveService s) ? s.Data.musicVolume : 1f;

        public static void Play(SfxId id)
        {
            if (ServiceLocator.TryGet(out AudioService audio)) audio.PlaySfx(id);
        }

        public static void Music(MusicId id)
        {
            if (ServiceLocator.TryGet(out AudioService audio)) audio.PlayMusic(id);
        }

        public void PlaySfx(SfxId id)
        {
            if (library == null) return;
            var entry = library.GetSfx(id);
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;
            AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;

            AudioSource src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % SfxVoices;
            src.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
            src.volume = entry.volume * SfxVolume;
            src.clip = clip;
            src.Play();
        }

        public void PlayMusic(MusicId id)
        {
            if (id == currentMusic) return;
            currentMusic = id;
            var entry = library != null ? library.GetMusic(id) : null;
            AudioSource from = musicA.isPlaying ? musicA : musicB;
            AudioSource to = from == musicA ? musicB : musicA;
            if (musicFade != null) StopCoroutine(musicFade);
            to.clip = entry != null ? entry.clip : null;
            currentMusicBaseVolume = entry != null ? entry.volume : 0.6f;
            musicFade = StartCoroutine(CrossFade(from, to.clip != null ? to : null));
        }

        public void ApplyVolumes()
        {
            float v = currentMusicBaseVolume * MusicVolume;
            if (musicA.isPlaying) musicA.volume = v;
            if (musicB.isPlaying) musicB.volume = v;
        }

        private IEnumerator CrossFade(AudioSource from, AudioSource to)
        {
            float target = currentMusicBaseVolume * MusicVolume;
            if (to != null)
            {
                to.volume = 0f;
                to.Play();
            }
            float startFrom = from != null ? from.volume : 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                if (from != null) from.volume = Mathf.Lerp(startFrom, 0f, t);
                if (to != null) to.volume = Mathf.Lerp(0f, target, t);
                yield return null;
            }
            if (from != null && from != to) from.Stop();
            if (to != null) to.volume = target;
            musicFade = null;
        }
    }
}
