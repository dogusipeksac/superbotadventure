using System;
using SuperBot.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.World
{
    /// <summary>Shows an on-screen prompt button while the player stands inside; pressing it raises Interacted.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class InteractZone : MonoBehaviour
    {
        [SerializeField] private Button promptButton;

        public event Action Interacted;
        public bool PlayerInside { get; private set; }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (promptButton != null)
            {
                promptButton.onClick.AddListener(() => Interacted?.Invoke());
                promptButton.gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out PlayerHealth _)) return;
            PlayerInside = true;
            if (promptButton != null) promptButton.gameObject.SetActive(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out PlayerHealth _)) return;
            PlayerInside = false;
            if (promptButton != null) promptButton.gameObject.SetActive(false);
        }

        public void SetPromptVisible(bool visible)
        {
            if (promptButton != null) promptButton.gameObject.SetActive(visible && PlayerInside);
        }
    }
}
