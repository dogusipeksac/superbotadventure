using UnityEngine;

namespace SuperBot.Data
{
    public enum CosmeticType
    {
        Hat,
        BodyColor,
        Trail
    }

    [CreateAssetMenu(menuName = "SuperBot/Cosmetic Item", fileName = "Cosmetic")]
    public sealed class CosmeticItem : ScriptableObject
    {
        public string id;
        public string displayName;
        public CosmeticType type;
        public int boltPrice;
        [Tooltip("Optional store product id (v1.1 IAP). Empty = bolts only.")]
        public string iapProductId;
        public GameObject hatPrefab;
        public Color bodyColor = Color.white;
        public Gradient trailGradient = new Gradient();
        public Sprite icon;

        public Color SwatchColor
        {
            get
            {
                switch (type)
                {
                    case CosmeticType.BodyColor: return bodyColor;
                    case CosmeticType.Trail: return trailGradient != null ? trailGradient.Evaluate(0f) : Color.white;
                    default: return hatPrefab != null ? new Color(0.85f, 0.75f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
                }
            }
        }
    }
}
