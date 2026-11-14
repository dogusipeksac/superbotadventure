using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.Audio
{
    [CreateAssetMenu(menuName = "SuperBot/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class SfxEntry
        {
            public SfxId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        }

        [Serializable]
        public sealed class MusicEntry
        {
            public MusicId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 0.6f;
        }

        public List<SfxEntry> sfx = new List<SfxEntry>();
        public List<MusicEntry> music = new List<MusicEntry>();

        private Dictionary<SfxId, SfxEntry> sfxMap;

        public SfxEntry GetSfx(SfxId id)
        {
            if (sfxMap == null)
            {
                sfxMap = new Dictionary<SfxId, SfxEntry>();
                foreach (var e in sfx)
                    if (e != null) sfxMap[e.id] = e;
            }
            return sfxMap.TryGetValue(id, out var entry) ? entry : null;
        }

        public MusicEntry GetMusic(MusicId id)
        {
            foreach (var m in music)
                if (m != null && m.id == id) return m;
            return null;
        }

        private void OnValidate() => sfxMap = null;
    }
}
