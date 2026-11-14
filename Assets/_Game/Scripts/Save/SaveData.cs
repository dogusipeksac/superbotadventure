using System;
using System.Collections.Generic;

namespace SuperBot.Save
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int bolts;
        public List<string> collectedCores = new List<string>();
        public List<string> collectedBoltIds = new List<string>();
        public List<string> unlockedCosmetics = new List<string>();
        public string equippedHat = "";
        public string equippedBodyColor = "";
        public string equippedTrail = "";
        public string lastCheckpointId = "";
        public bool attConsentAsked;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool hapticsEnabled = true;

        /// <summary>JsonUtility leaves lists null when missing from old files.</summary>
        public void EnsureValid()
        {
            collectedCores ??= new List<string>();
            collectedBoltIds ??= new List<string>();
            unlockedCosmetics ??= new List<string>();
            equippedHat ??= "";
            equippedBodyColor ??= "";
            equippedTrail ??= "";
            lastCheckpointId ??= "";
            if (bolts < 0) bolts = 0;
        }
    }

    [Serializable]
    public sealed class SaveEnvelope
    {
        public string payload;
        public string hash;
    }
}
