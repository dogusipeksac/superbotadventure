using UnityEngine;

namespace SuperBot.Audio
{
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] private MusicId music = MusicId.None;
        private void Start() => AudioService.Music(music);
    }
}
