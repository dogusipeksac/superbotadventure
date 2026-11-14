using System.Collections.Generic;
using SuperBot.Audio;
using SuperBot.CameraSystem;
using SuperBot.Core;
using SuperBot.Data;
using SuperBot.Player;
using SuperBot.World;
using UnityEngine;
using UnityEngine.UI;

namespace SuperBot.UI
{
    /// <summary>Cosmetics shop: tabs per type, buy with bolts (or IAP stub), equip. Previews on the real player.</summary>
    public sealed class ShopPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private InteractZone zone;
        [SerializeField] private Button[] tabButtons = new Button[3];
        [SerializeField] private Transform grid;
        [SerializeField] private ShopItemView itemTemplate;
        [SerializeField] private Text balanceText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform previewView;
        [SerializeField] private Transform previewStandPoint;
        [SerializeField] private ThirdPersonCamera cameraRig;
        [SerializeField] private GameObject[] hideWhileOpen;

        private readonly List<ShopItemView> views = new List<ShopItemView>();
        private CosmeticType currentTab = CosmeticType.BodyColor;
        private CosmeticService cosmetics;

        private void Awake()
        {
            if (zone != null) zone.Interacted += Open;
            UiSfx.Bind(closeButton, Close);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var type = (CosmeticType)i;
                UiSfx.Bind(tabButtons[i], () => ShowTab(type));
            }
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() => GameEvents.OnBoltsChanged += OnBolts;
        private void OnDisable() => GameEvents.OnBoltsChanged -= OnBolts;

        private void OnBolts(int total)
        {
            if (balanceText != null) balanceText.text = total.ToString();
        }

        public void Open()
        {
            if (!ServiceLocator.TryGet(out cosmetics) || cosmetics.Database == null)
            {
                Debug.LogWarning("[Shop] No CosmeticService/Database.");
                return;
            }
            panel.SetActive(true);
            if (zone != null) zone.SetPromptVisible(false);
            foreach (var go in hideWhileOpen) if (go != null) go.SetActive(false);

            if (ServiceLocator.TryGet(out PlayerHealth player))
            {
                player.Motor.InputEnabled = false;
                if (previewStandPoint != null) player.Motor.Teleport(previewStandPoint.position, previewStandPoint.rotation);
            }
            if (cameraRig != null) cameraRig.SetOverride(previewView);
            if (ServiceLocator.TryGet(out EconomyService economy)) OnBolts(economy.Bolts);
            ShowTab(currentTab);
        }

        public void Close()
        {
            panel.SetActive(false);
            foreach (var go in hideWhileOpen) if (go != null) go.SetActive(true);
            if (ServiceLocator.TryGet(out PlayerHealth player)) player.Motor.InputEnabled = true;
            if (cameraRig != null) cameraRig.SetOverride(null);
            if (zone != null) zone.SetPromptVisible(true);
        }

        private void ShowTab(CosmeticType type)
        {
            currentTab = type;
            for (int i = 0; i < tabButtons.Length; i++)
                if (tabButtons[i] != null) tabButtons[i].interactable = i != (int)type;
            Refresh();
        }

        private void Refresh()
        {
            if (cosmetics == null) return;
            int index = 0;
            foreach (var item in cosmetics.Database.items)
            {
                if (item == null || item.type != currentTab) continue;
                ShopItemView view = GetView(index++);
                bool unlocked = cosmetics.IsUnlocked(item);
                bool equipped = cosmetics.IsEquipped(item);
                string status;
                bool interactable = true;
                if (equipped) status = "EQUIPPED";
                else if (unlocked) status = "EQUIP";
                else if (!string.IsNullOrEmpty(item.iapProductId) && item.boltPrice <= 0) status = "BUY";
                else
                {
                    status = item.boltPrice + " BOLTS";
                    interactable = cosmetics.CanAfford(item);
                }
                CosmeticItem captured = item;
                view.Bind(item, status, interactable, equipped, () => OnItemClicked(captured));
            }
            for (int i = index; i < views.Count; i++) views[i].gameObject.SetActive(false);
        }

        private ShopItemView GetView(int index)
        {
            while (views.Count <= index)
            {
                var v = Instantiate(itemTemplate, grid);
                views.Add(v);
            }
            views[index].gameObject.SetActive(true);
            return views[index];
        }

        private void OnItemClicked(CosmeticItem item)
        {
            if (cosmetics.IsUnlocked(item))
            {
                cosmetics.Equip(item);
            }
            else if (!string.IsNullOrEmpty(item.iapProductId) && item.boltPrice <= 0)
            {
                cosmetics.BuyWithIap(item, ok =>
                {
                    if (ok) { cosmetics.Equip(item); AudioService.Play(SfxId.Purchase); }
                    Refresh();
                });
                return;
            }
            else if (cosmetics.TryBuyWithBolts(item))
            {
                AudioService.Play(SfxId.Purchase);
                if (ServiceLocator.TryGet(out HapticManager h)) h.Medium();
                cosmetics.Equip(item);
            }
            Refresh();
        }
    }
}
