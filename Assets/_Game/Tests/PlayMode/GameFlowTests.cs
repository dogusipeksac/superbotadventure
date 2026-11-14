using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SuperBot.Collectibles;
using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
using SuperBot.UI;
using SuperBot.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SuperBot.Tests
{
    /// <summary>End-to-end smoke test of the Phase 2 loop. Deletes the local save file.</summary>
    public sealed class GameFlowTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator BootMenuHubShopPauseIslandDeathCompleteLoop()
        {
            Debug.Log("[FlowTest] start");
            new SaveService().DeleteAll();
            SceneManager.LoadScene(SceneNames.Boot);
            yield return WaitForScene(SceneNames.MainMenu);

            // Main menu -> Hub
            Debug.Log("[FlowTest] Main menu -> Hub");
            Click(Object.FindFirstObjectByType<MainMenuController>(), "playButton");
            yield return WaitForScene(SceneNames.Hub);
            var player = ServiceLocator.Get<PlayerHealth>();
            Assert.IsNotNull(player, "player in hub");
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.Motor.IsGrounded, "player stands on hub ground");

            // Shop: buy + equip a color
            Debug.Log("[FlowTest] Shop: buy + equip a color");
            ServiceLocator.Get<EconomyService>().AddBolts(500);
            var shop = Object.FindFirstObjectByType<ShopPanel>();
            shop.Open();
            yield return null;
            var cosmetics = ServiceLocator.Get<CosmeticService>();
            var sky = cosmetics.Database.GetById("color_sky");
            Assert.IsTrue(cosmetics.TryBuyWithBolts(sky), "buy sky color");
            Assert.IsTrue(cosmetics.Equip(sky));
            Assert.IsTrue(cosmetics.IsEquipped(sky));
            Assert.AreEqual(450, ServiceLocator.Get<EconomyService>().Bolts);
            var hat = cosmetics.Database.GetById("hat_gear");
            Assert.IsTrue(cosmetics.TryBuyWithBolts(hat) && cosmetics.Equip(hat), "buy hat");
            shop.Close();

            // Pause
            Debug.Log("[FlowTest] Pause");
            var pause = ServiceLocator.Get<PauseService>();
            pause.SetPaused(true);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(Field<GameObject>(Object.FindFirstObjectByType<PauseMenu>(), "panel").activeSelf, "pause panel shown");
            pause.SetPaused(false);

            // Portal -> Island01
            Debug.Log("[FlowTest] Portal -> Island01");
            var portal = Object.FindFirstObjectByType<LevelPortal>();
            player.Motor.Teleport(portal.transform.position + Vector3.up * 0.3f, Quaternion.identity);
            yield return WaitForScene(SceneNames.Island01);
            player = ServiceLocator.Get<PlayerHealth>();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(player.Motor.IsGrounded, "player stands on island ground");

            // Death -> revive with (stub) ad
            Debug.Log("[FlowTest] Death -> revive with (stub) ad");
            player.Kill();
            yield return new WaitForSecondsRealtime(1f);
            var death = Object.FindFirstObjectByType<DeathPanel>();
            Assert.IsTrue(Field<GameObject>(death, "panel").activeSelf, "death panel shown");
            Click(death, "reviveAdButton");
            yield return null;
            yield return null;
            Assert.IsFalse(player.IsDead, "revived");

            // Collect all golden cores -> level complete
            Debug.Log("[FlowTest] Collect all golden cores -> level complete");
            foreach (var core in Object.FindObjectsByType<GoldenCore>(FindObjectsSortMode.None))
            {
                player.Motor.Teleport(core.transform.position - Vector3.up * 0.6f, Quaternion.identity);
                yield return new WaitForSeconds(0.3f);
            }
            var session = ServiceLocator.Get<LevelSession>();
            Assert.IsTrue(session.Completed, "level completed after 3 cores");
            yield return new WaitForSecondsRealtime(1.6f);
            var complete = Object.FindFirstObjectByType<LevelCompletePanel>();
            Assert.IsTrue(Field<GameObject>(complete, "panel").activeSelf, "complete panel shown");
            int before = ServiceLocator.Get<EconomyService>().Bolts;
            Click(complete, "doubleButton");
            yield return WaitForScene(SceneNames.Hub);
            Assert.GreaterOrEqual(ServiceLocator.Get<EconomyService>().Bolts, before);
            Assert.AreEqual(3, ServiceLocator.Get<SaveService>().Data.collectedCores.Count);
        }

        private static IEnumerator WaitForScene(string name, float timeout = 15f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (SceneManager.GetActiveScene().name != name || (ServiceLocator.TryGet(out SceneLoader l) && l.IsLoading))
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail($"Timed out waiting for scene {name} (active: {SceneManager.GetActiveScene().name})");
                yield return null;
            }
            yield return null;
        }

        private static T Field<T>(object target, string name)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(f, $"field {name}");
            return (T)f.GetValue(target);
        }

        private static void Click(object owner, string buttonField)
        {
            var b = Field<Button>(owner, buttonField);
            Assert.IsNotNull(b, buttonField);
            Assert.IsTrue(b.gameObject.activeInHierarchy, buttonField + " visible");
            b.onClick.Invoke();
        }
    }
}
