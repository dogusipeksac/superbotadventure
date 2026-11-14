using System;
using SuperBot.Data;
using SuperBot.Monetization;
using SuperBot.Save;

namespace SuperBot.Core
{
    public sealed class CosmeticService
    {
        private readonly SaveService save;
        private readonly EconomyService economy;
        private readonly IMonetizationService monetization;

        public CosmeticDatabase Database { get; }
        public event Action OnEquippedChanged;

        public CosmeticService(CosmeticDatabase database, SaveService save, EconomyService economy, IMonetizationService monetization)
        {
            Database = database;
            this.save = save;
            this.economy = economy;
            this.monetization = monetization;
        }

        public bool IsUnlocked(CosmeticItem item)
        {
            if (item == null) return false;
            if (item.boltPrice <= 0 && string.IsNullOrEmpty(item.iapProductId)) return true;
            if (save.Data.unlockedCosmetics.Contains(item.id)) return true;
            return !string.IsNullOrEmpty(item.iapProductId) && monetization != null && monetization.IsOwned(item.iapProductId);
        }

        public bool CanAfford(CosmeticItem item) => item != null && economy.Bolts >= item.boltPrice;

        public bool TryBuyWithBolts(CosmeticItem item)
        {
            if (item == null || IsUnlocked(item) || !economy.TrySpend(item.boltPrice)) return false;
            Unlock(item);
            return true;
        }

        public void BuyWithIap(CosmeticItem item, Action<bool> onResult)
        {
            if (item == null || string.IsNullOrEmpty(item.iapProductId) || monetization == null)
            {
                onResult?.Invoke(false);
                return;
            }
            monetization.Purchase(item.iapProductId, ok =>
            {
                if (ok) Unlock(item);
                onResult?.Invoke(ok);
            });
        }

        private void Unlock(CosmeticItem item)
        {
            if (!save.Data.unlockedCosmetics.Contains(item.id)) save.Data.unlockedCosmetics.Add(item.id);
            save.MarkDirty();
            save.Save();
        }

        public CosmeticItem GetEquipped(CosmeticType type)
        {
            string id = type switch
            {
                CosmeticType.Hat => save.Data.equippedHat,
                CosmeticType.BodyColor => save.Data.equippedBodyColor,
                _ => save.Data.equippedTrail
            };
            var item = Database != null ? Database.GetById(id) : null;
            if (item == null || item.type != type) item = Database != null ? Database.GetDefault(type) : null;
            return item;
        }

        public bool IsEquipped(CosmeticItem item) => item != null && GetEquipped(item.type) == item;

        public bool Equip(CosmeticItem item)
        {
            if (item == null || !IsUnlocked(item)) return false;
            switch (item.type)
            {
                case CosmeticType.Hat: save.Data.equippedHat = item.id; break;
                case CosmeticType.BodyColor: save.Data.equippedBodyColor = item.id; break;
                case CosmeticType.Trail: save.Data.equippedTrail = item.id; break;
            }
            save.MarkDirty();
            OnEquippedChanged?.Invoke();
            return true;
        }
    }
}
