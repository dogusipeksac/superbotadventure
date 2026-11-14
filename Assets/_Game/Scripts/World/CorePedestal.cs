using SuperBot.Core;
using SuperBot.Save;
using UnityEngine;

namespace SuperBot.World
{
    /// <summary>Hub display: shows the core visual once that golden core has been collected.</summary>
    public sealed class CorePedestal : MonoBehaviour
    {
        [SerializeField] private string coreId;
        [SerializeField] private GameObject coreVisual;
        [SerializeField] private float spinSpeed = 60f;

        private void Start()
        {
            bool has = ServiceLocator.TryGet(out SaveService save) && save.Data.collectedCores.Contains(coreId);
            if (coreVisual != null) coreVisual.SetActive(has);
        }

        private void Update()
        {
            if (coreVisual != null && coreVisual.activeSelf)
                coreVisual.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
