using System.Collections.Generic;
using UnityEngine;

namespace SuperBot.Data
{
    [CreateAssetMenu(menuName = "SuperBot/Cosmetic Database", fileName = "CosmeticDatabase")]
    public sealed class CosmeticDatabase : ScriptableObject
    {
        public List<CosmeticItem> items = new List<CosmeticItem>();

        public CosmeticItem GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var item in items)
                if (item != null && item.id == id) return item;
            return null;
        }

        /// <summary>First free item of a type: the default when nothing is equipped.</summary>
        public CosmeticItem GetDefault(CosmeticType type)
        {
            foreach (var item in items)
                if (item != null && item.type == type && item.boltPrice == 0 && string.IsNullOrEmpty(item.iapProductId)) return item;
            return null;
        }
    }
}
