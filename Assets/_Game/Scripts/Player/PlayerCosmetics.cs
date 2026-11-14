using SuperBot.Core;
using SuperBot.Data;
using UnityEngine;

namespace SuperBot.Player
{
    /// <summary>Applies equipped hat / body color / trail to the robot and keeps them in sync with the shop.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerCosmetics : MonoBehaviour
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        [SerializeField] private Transform modelRoot;
        [SerializeField] private string hatBoneName = "attach-hat";
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Vector3 hatOffset = new Vector3(0f, 0.05f, 0f);

        private PlayerMotor motor;
        private CosmeticService cosmetics;
        private Renderer[] bodyRenderers;
        private MaterialPropertyBlock block;
        private Transform hatBone;
        private GameObject currentHat;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            Transform root = modelRoot != null ? modelRoot : transform;
            bodyRenderers = root.GetComponentsInChildren<Renderer>(true);
            hatBone = FindDeep(root, hatBoneName);
            block = new MaterialPropertyBlock();
            if (trail != null) trail.emitting = false;
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out cosmetics)) cosmetics.OnEquippedChanged += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (cosmetics != null) cosmetics.OnEquippedChanged -= Apply;
        }

        private void Update()
        {
            if (trail != null) trail.emitting = motor.IsDashing || motor.IsHovering;
        }

        public void Apply()
        {
            if (cosmetics == null) return;
            ApplyColor(cosmetics.GetEquipped(CosmeticType.BodyColor));
            ApplyHat(cosmetics.GetEquipped(CosmeticType.Hat));
            ApplyTrail(cosmetics.GetEquipped(CosmeticType.Trail));
        }

        private void ApplyColor(CosmeticItem item)
        {
            Color c = item != null ? item.bodyColor : Color.white;
            foreach (var r in bodyRenderers)
            {
                if (r == null || r is TrailRenderer || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetColor(TintId, c);
                r.SetPropertyBlock(block);
            }
        }

        private void ApplyHat(CosmeticItem item)
        {
            if (currentHat != null) Destroy(currentHat);
            currentHat = null;
            if (item == null || item.hatPrefab == null) return;
            Transform parent = hatBone != null ? hatBone : (modelRoot != null ? modelRoot : transform);
            currentHat = Instantiate(item.hatPrefab, parent);
            currentHat.transform.localPosition = hatBone != null ? hatOffset : new Vector3(0f, 1.5f, 0f);
            currentHat.transform.localRotation = Quaternion.identity;
            // Counter the model scale so hats keep their authored size.
            Vector3 s = parent.lossyScale;
            currentHat.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f / Mathf.Max(0.0001f, s.z));
            foreach (var t in currentHat.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        }

        private void ApplyTrail(CosmeticItem item)
        {
            if (trail == null) return;
            if (item != null && item.trailGradient != null) trail.colorGradient = item.trailGradient;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
