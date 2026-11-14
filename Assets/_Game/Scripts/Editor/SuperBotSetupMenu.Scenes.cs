using System.Collections.Generic;
using SuperBot.Audio;
using SuperBot.CameraSystem;
using SuperBot.Collectibles;
using SuperBot.Controls;
using SuperBot.Core;
using SuperBot.Data;
using SuperBot.Player;
using SuperBot.UI;
using SuperBot.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperBot.EditorTools
{
    /// <summary>Phase 2: cosmetics/audio assets and the Boot, MainMenu, Hub and Island01 scenes.</summary>
    public static partial class SuperBotSetupMenu
    {
        private const string CosmeticsDir = SoDir + "/Cosmetics";
        private const string CosmeticDbPath = SoDir + "/CosmeticDatabase.asset";
        private const string AudioLibraryPath = SoDir + "/AudioLibrary.asset";
        private const string GearHatModel = ArtRoot + "/Cosmetics/whimsical-gear-hub-88deb5.glb";
        private const string ShopNpcModel = ArtRoot + "/Characters/companion-bot-170074.glb";
        private const string BeaconModel = ArtRoot + "/Props/Checkpoints/waypoint-beacon-23ba00.glb";
        private const string StumpModel = NatureDir + "/tree-stump-ec3f48.glb";

        private const string BootScene = SceneDir + "/Boot.unity";
        private const string MenuScene = SceneDir + "/MainMenu.unity";
        private const string HubScene = SceneDir + "/Hub.unity";
        private const string Island01Scene = SceneDir + "/Island01.unity";

        private static readonly string[] Island01Cores = { "island01_core_ledge", "island01_core_island2", "island01_core_platform" };

        // ------------------------------------------------------------------ menu

        [MenuItem("SuperBot/5. Create Default Cosmetics + Audio Library", priority = 25)]
        public static void CreateDefaultCosmetics()
        {
            EnsureFolder(CosmeticsDir);
            EnsureFolder(PrefabDir);
            if (AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit) == null) CreateMaterials();
            var lit = AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit);

            GameObject hatPrefab = null;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GearHatModel) != null)
            {
                var root = new GameObject("Hat_GearAntenna");
                CreateModel(GearHatModel, root.transform, 0.35f, SizeMode.MaxDimension, lit, false);
                hatPrefab = SavePrefab(root, "Hat_GearAntenna");
            }

            var items = new List<CosmeticItem>
            {
                Cosmetic("hat_none", "No Hat", CosmeticType.Hat, 0, null, Color.white, null),
                Cosmetic("hat_gear", "Gear Antenna", CosmeticType.Hat, 150, hatPrefab, Color.white, null),
                Cosmetic("color_default", "Factory White", CosmeticType.BodyColor, 0, null, Color.white, null),
                Cosmetic("color_sky", "Sky Blue", CosmeticType.BodyColor, 50, null, new Color(0.55f, 0.8f, 1f), null),
                Cosmetic("color_lava", "Lava Red", CosmeticType.BodyColor, 120, null, new Color(1f, 0.5f, 0.4f), null),
                Cosmetic("trail_cyan", "Cyan Jet", CosmeticType.Trail, 0, null, Color.white, TwoColor(new Color(0.4f, 0.95f, 1f), new Color(0.2f, 0.5f, 1f))),
                Cosmetic("trail_gold", "Golden Jet", CosmeticType.Trail, 80, null, Color.white, TwoColor(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.5f, 0.1f))),
                Cosmetic("trail_rainbow", "Rainbow Jet", CosmeticType.Trail, 200, null, Color.white, Rainbow()),
            };

            var db = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(CosmeticDbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CosmeticDatabase>();
                AssetDatabase.CreateAsset(db, CosmeticDbPath);
            }
            db.items = items;
            EditorUtility.SetDirty(db);

            var lib = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<AudioLibrary>();
                foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
                    lib.sfx.Add(new AudioLibrary.SfxEntry { id = id, clips = new AudioClip[0] });
                foreach (MusicId id in System.Enum.GetValues(typeof(MusicId)))
                    if (id != MusicId.None) lib.music.Add(new AudioLibrary.MusicEntry { id = id });
                AssetDatabase.CreateAsset(lib, AudioLibraryPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SuperBot] Cosmetics + audio library ready.");
        }

        [MenuItem("SuperBot/6. Build Boot + Menu + Hub + Island01", priority = 26)]
        public static void BuildAllScenes()
        {
            if (!CheckModelsImported()) return;
            if (LoadPrefab<PlayerMotor>("Player") == null) BuildPrefabs();
            if (AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(CosmeticDbPath) == null) CreateDefaultCosmetics();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(SceneDir);

            BuildBootScene();
            BuildMenuScene();
            BuildHubScene();
            BuildIsland01Scene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScene, true),
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(HubScene, true),
                new EditorBuildSettingsScene(Island01Scene, true),
            };
            EditorSceneManager.OpenScene(HubScene);
            Debug.Log("[SuperBot] Scenes built: Boot, MainMenu, Hub, Island01 (Build Settings updated).");
        }

        [MenuItem("SuperBot/Delete Save File", priority = 60)]
        public static void DeleteSave()
        {
            new SuperBot.Save.SaveService().DeleteAll();
            Debug.Log("[SuperBot] Save deleted: " + Application.persistentDataPath);
        }

        // ------------------------------------------------------------------ scenes

        private static void BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBootstrapper();
            new GameObject("[BootFlow]").AddComponent<BootFlow>();
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            EditorSceneManager.SaveScene(scene, BootScene);
        }

        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit);
            SetupAtmosphere();
            CreateBootstrapper();
            new GameObject("[Music]").AddComponent<SceneMusic>();
            Set(Object.FindFirstObjectByType<SceneMusic>(), "music", (int)MusicId.Menu);

            var world = new GameObject("World").transform;
            PlaceGround(GrassBlob, world, Vector3.zero, 0f, 1f, lit);
            PlaceProp(NatureDir + "/broadleaf-oak-997c22.glb", world, new Vector3(-2.2f, 0.4f, 0.8f), 30f, lit);
            PlaceProp(NatureDir + "/young-pine-0d7695.glb", world, new Vector3(2f, 0.4f, 1f), 0f, lit);
            PlaceProp(NatureDir + "/round-bush-cd2ac0.glb", world, new Vector3(1.4f, 0.4f, -0.9f), 0f, lit);

            var robotHolder = new GameObject("Robot").transform;
            robotHolder.position = new Vector3(0f, 0.45f, 0f);
            robotHolder.rotation = Quaternion.Euler(0f, 180f, 0f);
            var model = CreateModel(RobotModel, robotHolder, 1.45f, SizeMode.Height, lit, false);
            AttachLegacyClips(model.gameObject);
            var anim = model.GetComponentInChildren<Animation>();
            if (anim != null && anim.GetClip("Dance") != null)
            {
                anim.clip = anim.GetClip("Dance");
                anim.wrapMode = WrapMode.Loop;
            }

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();
            var orbit = camGo.AddComponent<OrbitCamera>();
            Set(orbit, "center", new Vector3(0f, 0.5f, 0f));
            Set(orbit, "radius", 7.5f);
            Set(orbit, "height", 1.8f);
            Set(orbit, "lookHeight", 1.2f);
            cam.fieldOfView = 38f;
            camGo.transform.position = new Vector3(0f, 2.7f, 6f);
            camGo.transform.LookAt(new Vector3(0f, 1.5f, 0f));

            var canvas = CreateCanvas(out RectTransform safe);
            var mainButtons = UiRect("MainButtons", safe, Vector2.zero, Vector2.one);
            var title = MakeText(mainButtons, "SUPER BOT\nADVENTURE", 96, new Vector2(0.05f, 0.5f), new Vector2(0.55f, 0.95f), TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5f, -5f);
            var play = MakeButton(mainButtons, "PLAY", new Vector2(0.15f, 0.3f), new Vector2(0f, 0f), new Vector2(380f, 130f), new Color(0.3f, 0.85f, 1f), 56);
            var settingsBtn = MakeButton(mainButtons, "SETTINGS", new Vector2(0.15f, 0.14f), Vector2.zero, new Vector2(300f, 90f), new Color(1f, 1f, 1f, 0.85f), 38);
            var version = MakeText(safe, "v", 28, new Vector2(0.8f, 0f), new Vector2(0.99f, 0.08f), TextAnchor.LowerRight);
            var settings = BuildSettingsPanel(safe);

            var menu = canvas.gameObject.AddComponent<MainMenuController>();
            Set(menu, "playButton", play);
            Set(menu, "settingsButton", settingsBtn);
            Set(menu, "settings", settings);
            Set(menu, "versionText", version);
            Set(menu, "mainButtons", mainButtons.gameObject);
            FinishCanvas(canvas);
            EditorSceneManager.SaveScene(scene, MenuScene);
        }

        private static void BuildHubScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit);
            var coreMat = AssetDatabase.LoadAssetAtPath<Material>(MatCore);
            SetupAtmosphere();

            var world = new GameObject("World").transform;
            var islands = Child(world, "Island");
            Vector3[] blobs =
            {
                new Vector3(0, 0, 0), new Vector3(7, 0, 0), new Vector3(-7, 0, 0), new Vector3(0, 0, 7),
                new Vector3(0, 0, -7), new Vector3(7, 0, 7), new Vector3(-7, 0, 7), new Vector3(7, 0, -7), new Vector3(-7, 0, -7),
            };
            var rng = new System.Random(7);
            foreach (var b in blobs) PlaceGround(GrassBlob, islands, b, rng.Next(0, 4) * 90f, 1f, lit);
            Physics.SyncTransforms();

            var nature = Child(world, "Nature");
            PlaceProp(NatureDir + "/tall-pine-tree-ab4108.glb", nature, OnGround(new Vector3(-9, 0, 9), 0f), 0f, lit, true);
            PlaceProp(NatureDir + "/broadleaf-oak-997c22.glb", nature, OnGround(new Vector3(9, 0, -8), 0f), 40f, lit, true);
            PlaceProp(NatureDir + "/large-boulder-a29b99.glb", nature, OnGround(new Vector3(-9, 0, -7), 0f), 10f, lit, true);
            PlaceProp(NatureDir + "/wildflower-68d3a1.glb", nature, OnGround(new Vector3(2, 0, -2), 0f), 0f, lit);
            PlaceProp(NatureDir + "/grass-tuft-a40a08.glb", nature, OnGround(new Vector3(-3, 0, 2), 0f), 0f, lit);

            var g = BuildGameplayScaffold(OnGround(new Vector3(0f, 0f, -3f), 0.1f), SceneNames.MainMenu, "MENU", true);
            Set(g.Session, "levelId", "hub");
            Set(g.Session, "isHub", true);
            Set(g.Music, "music", (int)MusicId.Hub);

            // Portal to Island01
            var portal = BuildPortal(world, OnGround(new Vector3(-6f, 0f, -4f), 0f), SceneNames.Island01, lit);
            portal.name = "Portal_Island01";

            // Core pedestals
            var pedestals = Child(world, "CorePedestals");
            for (int i = 0; i < Island01Cores.Length; i++)
            {
                var ped = new GameObject("Pedestal_" + Island01Cores[i]).transform;
                ped.SetParent(pedestals, false);
                ped.position = OnGround(new Vector3(-2.5f + i * 2.5f, 0f, 9f), 0f);
                CreateModel(StumpModel, ped, 0.9f, SizeMode.Height, lit, false);
                var holder = new GameObject("Core").transform;
                holder.SetParent(ped, false);
                holder.localPosition = new Vector3(0f, 1f, 0f);
                var visual = CreateModel(CoreModel, holder, 0.8f, SizeMode.MaxDimension, coreMat, true);
                var cp = ped.gameObject.AddComponent<CorePedestal>();
                Set(cp, "coreId", Island01Cores[i]);
                Set(cp, "coreVisual", holder.gameObject);
                visual.name = "CoreVisual";
            }

            // Shop NPC + zone + preview
            var shop = new GameObject("Shop").transform;
            shop.SetParent(world, false);
            shop.position = OnGround(new Vector3(6f, 0f, 6f), 0f);
            shop.rotation = Quaternion.Euler(0f, 225f, 0f);
            CreateModel(ShopNpcModel, shop, 1.6f, SizeMode.Height, lit, false);
            var zoneGo = new GameObject("ShopZone");
            zoneGo.transform.SetParent(shop, false);
            var zoneCol = zoneGo.AddComponent<BoxCollider>();
            zoneCol.isTrigger = true;
            zoneCol.center = new Vector3(0f, 1f, 1.5f);
            zoneCol.size = new Vector3(4f, 2.5f, 4f);
            var zone = zoneGo.AddComponent<InteractZone>();

            var stand = new GameObject("PreviewStand").transform;
            stand.SetParent(world, false);
            stand.position = OnGround(new Vector3(3.5f, 0f, 3f), 0.05f);
            stand.rotation = Quaternion.Euler(0f, 180f, 0f);
            var view = new GameObject("PreviewCamera").transform;
            view.SetParent(world, false);
            view.position = stand.position + new Vector3(1.3f, 1.2f, -3.6f);
            view.rotation = Quaternion.Euler(8f, 0f, 0f);

            var prompt = MakeButton(g.Hud, "SHOP", new Vector2(1f, 0.5f), new Vector2(-150f, 60f), new Vector2(200f, 110f), new Color(1f, 0.8f, 0.25f), 44);
            Set(zone, "promptButton", prompt);
            BuildShopPanel(g, zone, view, stand);

            FinishCanvas(g.Canvas);
            EditorSceneManager.SaveScene(scene, HubScene);
        }

        private static void BuildIsland01Scene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit);
            var rng = new System.Random(42);
            SetupAtmosphere();

            // ---- Islands (7 m spacing so blobs overlap without gaps) ----
            var world = new GameObject("World").transform;
            var islands = Child(world, "Islands");
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    PlaceGround(GrassBlob, islands, new Vector3(x * 7f, 0f, z * 7f), rng.Next(0, 4) * 90f, 1f, lit);
            Vector3[] island2 = { new Vector3(-3.5f, 0, 31f), new Vector3(3.5f, 0, 31f), new Vector3(0, 0, 37.5f) };
            foreach (var p in island2) PlaceGround(GrassBlob, islands, p, rng.Next(0, 4) * 90f, 1f, lit);
            PlaceGround(GrassBlob, islands, new Vector3(29f, 0f, 0f), 0f, 0.6f, lit);      // moving-platform island
            PlaceGround(GrassBlob, islands, new Vector3(-7f, 4.3f, 13.5f), 0f, 0.45f, lit); // high ledge
            for (int i = 0; i < 4; i++)
                PlaceGround(BridgeModel, islands, new Vector3(0f, 0.05f, 13f + i * 4.4f), 90f, 1.1f, lit);
            Physics.SyncTransforms();

            // ---- Nature ----
            var nature = Child(world, "Nature");
            string[] natureModels =
            {
                "broadleaf-oak-997c22", "tall-pine-tree-ab4108", "young-pine-0d7695", "round-bush-cd2ac0",
                "medium-rock-e08405", "large-boulder-a29b99", "small-rock-db33a7", "toadstool-257a4a",
                "grass-tuft-a40a08", "wildflower-68d3a1", "fallen-log-1685fb", "tree-stump-ec3f48",
            };
            Vector3[] naturePositions =
            {
                new Vector3(-9, 0, -9), new Vector3(9, 0, -9), new Vector3(-10, 0, 2), new Vector3(10, 0, 6),
                new Vector3(-3, 0, -10), new Vector3(8.5f, 0, 9), new Vector3(-9, 0, 8), new Vector3(4, 0, -9.5f),
                new Vector3(-4, 0, 4), new Vector3(5, 0, -3), new Vector3(-7.5f, 0, -3), new Vector3(3, 0, 9),
                new Vector3(-6, 0, 33), new Vector3(6, 0, 34), new Vector3(-2, 0, 40), new Vector3(4, 0, 28),
            };
            for (int i = 0; i < naturePositions.Length; i++)
            {
                int m = i % natureModels.Length;
                PlaceProp($"{NatureDir}/{natureModels[m]}.glb", nature, OnGround(naturePositions[i], 0f),
                    (float)rng.NextDouble() * 360f, lit, m < 6);
            }
            Physics.SyncTransforms();

            var g = BuildGameplayScaffold(OnGround(new Vector3(0f, 0f, -5f), 0.1f), SceneNames.Hub, "HUB", false);
            Set(g.Session, "levelId", "island01");
            SetStringArray(g.Session, "coreIds", Island01Cores);
            Set(g.Music, "music", (int)MusicId.Island);

            // ---- Gameplay ----
            var gameplay = Child(world, "Gameplay");
            var cp1 = Spawn("Checkpoint", gameplay, OnGround(new Vector3(2.5f, 0, -7.5f), 0f));
            Set(cp1.GetComponent<Checkpoint>(), "checkpointId", "island01_cp_start");
            var cp2 = Spawn("Checkpoint", gameplay, OnGround(new Vector3(3.5f, 0, 29f), 0f));
            Set(cp2.GetComponent<Checkpoint>(), "checkpointId", "island01_cp_island2");

            var portal = BuildPortal(gameplay, OnGround(new Vector3(-3.5f, 0f, -7.5f), 0f), SceneNames.Hub, lit);
            portal.name = "Portal_Hub";

            int boltIndex = 0;
            var bolts = Child(gameplay, "Bolts");
            for (int i = 0; i < 6; i++) SpawnBolt(bolts, new Vector3(0f, 0, -2f + i * 1.8f), ref boltIndex, "island01");
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                SpawnBolt(bolts, new Vector3(5f + Mathf.Cos(a) * 2.2f, 0, 4f + Mathf.Sin(a) * 2.2f), ref boltIndex, "island01");
            }
            for (int i = 0; i < 5; i++) SpawnBolt(bolts, new Vector3(0f, 0, 13f + i * 2.5f), ref boltIndex, "island01");
            for (int i = 0; i < 4; i++) SpawnBolt(bolts, new Vector3(-3f + i * 2f, 0, 34f), ref boltIndex, "island01");
            for (int i = 0; i < 3; i++) SpawnBolt(bolts, new Vector3(15f + i * 4f, 0, 0f), ref boltIndex, "island01", 1.6f);

            Vector3[] cratePositions = { new Vector3(-5, 0, -2), new Vector3(-6.2f, 0, -0.8f), new Vector3(-4, 0, 2.5f), new Vector3(7, 0, -5) };
            foreach (var p in cratePositions)
                Spawn("Crate", gameplay, OnGround(p, 0f)).transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f);

            Spawn("JumpPad", gameplay, OnGround(new Vector3(-7f, 0, 7.5f), 0f));

            var platform = Spawn("MovingPlatform", gameplay, new Vector3(13.2f, 0f, 0f));
            SetVectorList(platform.GetComponent<MovingPlatform>(), "waypointOffsets", new[] { Vector3.zero, new Vector3(10f, 0f, 0f) });

            var enemy = Spawn("Enemy_Beetle", gameplay, OnGround(new Vector3(4f, 0, -1f), 0f));
            SetVectorArray(enemy.GetComponent<PatrolEnemy>(), "waypointOffsets", new[] { new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f) });
            var enemy2 = Spawn("Enemy_Beetle", gameplay, OnGround(new Vector3(0f, 0, 34f), 0f));
            SetVectorArray(enemy2.GetComponent<PatrolEnemy>(), "waypointOffsets", new[] { new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f) });

            var cores = Child(gameplay, "GoldenCores");
            SpawnCore(cores, Island01Cores[0], OnGround(new Vector3(-7f, 10f, 13.5f), 1.2f));
            SpawnCore(cores, Island01Cores[1], OnGround(new Vector3(0f, 0f, 40f), 1.2f));
            SpawnCore(cores, Island01Cores[2], OnGround(new Vector3(29f, 0f, 0f), 1.2f));

            var kill = new GameObject("KillZone");
            kill.transform.SetParent(world, false);
            kill.transform.position = new Vector3(0f, -15f, 0f);
            var killCol = kill.AddComponent<BoxCollider>();
            killCol.isTrigger = true;
            killCol.size = new Vector3(400f, 2f, 400f);
            kill.AddComponent<KillZone>();

            FinishCanvas(g.Canvas);
            EditorSceneManager.SaveScene(scene, Island01Scene);
        }

        // ------------------------------------------------------------------ gameplay scaffold

        private sealed class GameplayScene
        {
            public GameObject Player;
            public ThirdPersonCamera Camera;
            public Canvas Canvas;
            public RectTransform Safe;
            public RectTransform Controls;
            public RectTransform Hud;
            public Button PauseButton;
            public LevelSession Session;
            public SceneMusic Music;
        }

        private static GameplayScene BuildGameplayScaffold(Vector3 spawnPos, string exitScene, string exitLabel, bool isHub)
        {
            var g = new GameplayScene();
            CreateBootstrapper();
            new GameObject("[Input]").AddComponent<MobileInputRouter>();
            g.Session = new GameObject("[LevelSession]").AddComponent<LevelSession>();
            g.Music = new GameObject("[Music]").AddComponent<SceneMusic>();

            var respawnGo = new GameObject("[Respawn]");
            var respawn = respawnGo.AddComponent<RespawnService>();
            var spawnPoint = new GameObject("DefaultSpawn").transform;
            spawnPoint.SetParent(respawnGo.transform, false);
            spawnPoint.position = spawnPos;

            g.Player = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefabGo("Player"));
            g.Player.transform.position = spawnPos;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 160f;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            g.Camera = camGo.AddComponent<ThirdPersonCamera>();
            Set(g.Camera, "target", g.Player.transform);
            Set(g.Camera, "targetMotor", g.Player.GetComponent<PlayerMotor>());
            Set(g.Camera, "collisionMask", LayerMask.GetMask("Default", LayerGround));
            camGo.transform.position = spawnPos + new Vector3(0f, 3f, -7f);
            camGo.transform.LookAt(spawnPos + Vector3.up);
            Set(g.Player.GetComponent<PlayerMotor>(), "cameraTransform", camGo.transform);
            Set(respawn, "defaultSpawn", spawnPoint);
            Set(respawn, "player", g.Player.GetComponent<PlayerHealth>());

            // ---- UI ----
            g.Canvas = CreateCanvas(out g.Safe);
            g.Controls = UiRect("Controls", g.Safe, Vector2.zero, Vector2.one);
            BuildTouchControls(g.Controls);
            g.Hud = UiRect("HUD", g.Safe, Vector2.zero, Vector2.one);
            BuildHud(g.Hud);

            g.PauseButton = MakeButton(g.Hud, "II", new Vector2(1f, 1f), new Vector2(-70f, -60f), new Vector2(96f, 96f), new Color(1f, 1f, 1f, 0.8f), 40);
            var settings = BuildSettingsPanel(g.Safe);

            var pauseBox = MakePanel(g.Safe, "PausePanel", new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.8f), 0.55f, out GameObject pauseRoot);
            MakeText(pauseBox, "PAUSED", 64, new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            var resume = MakeButton(pauseBox, "RESUME", new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(360f, 100f), new Color(0.3f, 0.85f, 1f), 40);
            var settingsBtn = MakeButton(pauseBox, "SETTINGS", new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(360f, 90f), new Color(1f, 1f, 1f, 0.9f), 36);
            var exit = MakeButton(pauseBox, exitLabel, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(360f, 90f), new Color(1f, 0.6f, 0.4f), 36);
            var pause = g.Hud.gameObject.AddComponent<PauseMenu>();
            Set(pause, "pauseButton", g.PauseButton);
            Set(pause, "panel", pauseRoot);
            Set(pause, "resumeButton", resume);
            Set(pause, "settingsButton", settingsBtn);
            Set(pause, "exitButton", exit);
            Set(pause, "exitScene", exitScene);
            Set(pause, "settings", settings);

            // Death panel
            var deathBox = MakePanel(g.Safe, "DeathPanel", new Vector2(0.33f, 0.25f), new Vector2(0.67f, 0.75f), 0.45f, out GameObject deathRoot);
            MakeText(deathBox, "OUCH!", 72, new Vector2(0f, 0.7f), new Vector2(1f, 0.97f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
            var revive = MakeButton(deathBox, "REVIVE HERE (AD)", new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(440f, 100f), new Color(0.4f, 1f, 0.5f), 36);
            var checkpoint = MakeButton(deathBox, "CHECKPOINT", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(440f, 90f), new Color(1f, 1f, 1f, 0.9f), 34);
            var death = g.Hud.gameObject.AddComponent<DeathPanel>();
            Set(death, "panel", deathRoot);
            Set(death, "reviveAdButton", revive);
            Set(death, "checkpointButton", checkpoint);

            // Level complete panel
            if (!isHub)
            {
                var doneBox = MakePanel(g.Safe, "LevelCompletePanel", new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.82f), 0.55f, out GameObject doneRoot);
                MakeText(doneBox, "ISLAND COMPLETE!", 60, new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
                var boltsText = MakeText(doneBox, "+0", 90, new Vector2(0f, 0.52f), new Vector2(1f, 0.76f), TextAnchor.MiddleCenter);
                boltsText.color = new Color(1f, 0.85f, 0.3f);
                var x2 = MakeButton(doneBox, "COLLECT x2 (AD)", new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(460f, 100f), new Color(0.4f, 1f, 0.5f), 36);
                var collect = MakeButton(doneBox, "COLLECT", new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(460f, 90f), new Color(1f, 1f, 1f, 0.9f), 34);
                var complete = g.Hud.gameObject.AddComponent<LevelCompletePanel>();
                Set(complete, "panel", doneRoot);
                Set(complete, "boltsText", boltsText);
                Set(complete, "collectButton", collect);
                Set(complete, "doubleButton", x2);
                Set(complete, "nextScene", SceneNames.Hub);
            }

            return g;
        }

        private static void BuildTouchControls(RectTransform root)
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var look = UiRect("LookZone", root, new Vector2(0.4f, 0f), new Vector2(1f, 1f));
            TransparentImage(look);
            look.gameObject.AddComponent<LookTouchZone>();

            var joyZone = UiRect("JoystickZone", root, new Vector2(0f, 0f), new Vector2(0.4f, 0.78f));
            TransparentImage(joyZone);
            var joyBg = UiRect("Background", joyZone, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            joyBg.sizeDelta = new Vector2(220f, 220f);
            joyBg.anchoredPosition = new Vector2(-150f, -170f);
            var bgImg = joyBg.gameObject.AddComponent<Image>();
            bgImg.sprite = circle;
            bgImg.color = new Color(1f, 1f, 1f, 0.25f);
            bgImg.raycastTarget = false;
            var knob = UiRect("Knob", joyBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            knob.sizeDelta = new Vector2(100f, 100f);
            var knobImg = knob.gameObject.AddComponent<Image>();
            knobImg.sprite = circle;
            knobImg.color = new Color(1f, 1f, 1f, 0.7f);
            knobImg.raycastTarget = false;
            var joystick = joyZone.gameObject.AddComponent<VirtualJoystick>();
            Set(joystick, "background", joyBg);
            Set(joystick, "knob", knob);

            TouchButtonUi("JumpButton", root, ButtonAction.Jump, "JUMP", new Vector2(-180f, 180f), 210f, new Color(0.3f, 0.85f, 1f, 0.8f), circle);
            TouchButtonUi("SpinButton", root, ButtonAction.Attack, "SPIN", new Vector2(-420f, 130f), 150f, new Color(1f, 0.55f, 0.25f, 0.8f), circle);
            TouchButtonUi("DashButton", root, ButtonAction.Dash, "DASH", new Vector2(-250f, 400f), 140f, new Color(0.7f, 0.5f, 1f, 0.8f), circle);
        }

        private static void BuildHud(RectTransform hud)
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var hudCtrl = hud.gameObject.AddComponent<HudController>();

            var boltIcon = UiRect("BoltIcon", hud, new Vector2(0f, 1f), new Vector2(0f, 1f));
            boltIcon.sizeDelta = new Vector2(64f, 64f);
            boltIcon.anchoredPosition = new Vector2(70f, -60f);
            var boltIconImg = boltIcon.gameObject.AddComponent<Image>();
            boltIconImg.sprite = circle;
            boltIconImg.color = new Color(1f, 0.8f, 0.2f);
            boltIconImg.raycastTarget = false;

            var boltText = MakeText(hud, "0", 56, new Vector2(0f, 1f), new Vector2(0f, 1f), TextAnchor.MiddleLeft);
            var bt = boltText.rectTransform;
            bt.sizeDelta = new Vector2(300f, 80f);
            bt.pivot = new Vector2(0f, 0.5f);
            bt.anchoredPosition = new Vector2(115f, -60f);
            boltText.fontStyle = FontStyle.Bold;
            boltText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -3f);

            var hpIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var hp = UiRect("HP" + i, hud, new Vector2(0f, 1f), new Vector2(0f, 1f));
                hp.sizeDelta = new Vector2(44f, 44f);
                hp.anchoredPosition = new Vector2(60f + i * 54f, -135f);
                hpIcons[i] = hp.gameObject.AddComponent<Image>();
                hpIcons[i].sprite = circle;
                hpIcons[i].raycastTarget = false;
            }

            var coreIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var c = UiRect("Core" + i, hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
                c.sizeDelta = new Vector2(64f, 64f);
                c.anchoredPosition = new Vector2((i - 1) * 84f, -60f);
                coreIcons[i] = c.gameObject.AddComponent<Image>();
                coreIcons[i].sprite = circle;
                coreIcons[i].raycastTarget = false;
            }

            Set(hudCtrl, "boltText", boltText);
            SetObjectArray(hudCtrl, "coreIcons", coreIcons);
            SetObjectArray(hudCtrl, "hpIcons", hpIcons);
        }

        private static SettingsPanel BuildSettingsPanel(RectTransform parent)
        {
            var box = MakePanel(parent, "SettingsPanel", new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.88f), 0.6f, out GameObject root);
            MakeText(box, "SETTINGS", 60, new Vector2(0f, 0.84f), new Vector2(1f, 0.98f), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;

            var res = UiResources();
            MakeText(box, "Music", 36, new Vector2(0.06f, 0.66f), new Vector2(0.35f, 0.78f), TextAnchor.MiddleLeft);
            var music = SliderAt(box, res, new Vector2(0.38f, 0.68f), new Vector2(0.94f, 0.76f));
            MakeText(box, "Sound FX", 36, new Vector2(0.06f, 0.52f), new Vector2(0.35f, 0.64f), TextAnchor.MiddleLeft);
            var sfx = SliderAt(box, res, new Vector2(0.38f, 0.54f), new Vector2(0.94f, 0.62f));
            MakeText(box, "Vibration", 36, new Vector2(0.06f, 0.38f), new Vector2(0.35f, 0.5f), TextAnchor.MiddleLeft);
            var toggleGo = DefaultControls.CreateToggle(res);
            toggleGo.transform.SetParent(box, false);
            var trt = (RectTransform)toggleGo.transform;
            trt.anchorMin = new Vector2(0.38f, 0.4f);
            trt.anchorMax = new Vector2(0.94f, 0.48f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var toggleLabel = toggleGo.GetComponentInChildren<Text>();
            if (toggleLabel != null) { toggleLabel.text = "On"; toggleLabel.fontSize = 32; toggleLabel.color = Color.white; }
            var toggleBg = toggleGo.transform.Find("Background") as RectTransform;
            if (toggleBg != null) toggleBg.sizeDelta = new Vector2(40f, 40f);

            var restore = MakeButton(box, "RESTORE PURCHASES", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(460f, 80f), new Color(1f, 1f, 1f, 0.9f), 30);
            var privacy = MakeButton(box, "PRIVACY POLICY", new Vector2(0.5f, 0.25f), new Vector2(0f, -90f), new Vector2(460f, 70f), new Color(1f, 1f, 1f, 0.7f), 26);
            var close = MakeButton(box, "CLOSE", new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(300f, 80f), new Color(0.3f, 0.85f, 1f), 34);

            var panel = box.gameObject.AddComponent<SettingsPanel>();
            Set(panel, "root", root);
            Set(panel, "musicSlider", music);
            Set(panel, "sfxSlider", sfx);
            Set(panel, "hapticsToggle", toggleGo.GetComponent<Toggle>());
            Set(panel, "restoreButton", restore);
            Set(panel, "privacyButton", privacy);
            Set(panel, "closeButton", close);
            return panel;
        }

        private static void BuildShopPanel(GameplayScene g, InteractZone zone, Transform previewView, Transform stand)
        {
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var box = MakePanel(g.Safe, "ShopPanel", new Vector2(0.44f, 0.04f), new Vector2(0.98f, 0.96f), 0f, out GameObject root);
            MakeText(box, "ROBO SHOP", 54, new Vector2(0.04f, 0.88f), new Vector2(0.6f, 0.99f), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            var balance = MakeText(box, "0", 44, new Vector2(0.55f, 0.88f), new Vector2(0.84f, 0.99f), TextAnchor.MiddleRight);
            balance.color = new Color(1f, 0.85f, 0.3f);
            var close = MakeButton(box, "X", new Vector2(1f, 1f), new Vector2(-55f, -50f), new Vector2(80f, 80f), new Color(1f, 0.5f, 0.4f), 40);

            string[] tabNames = { "HATS", "COLORS", "TRAILS" }; // order == CosmeticType
            var tabs = new Button[3];
            for (int i = 0; i < 3; i++)
                tabs[i] = MakeButton(box, tabNames[i], new Vector2(0.2f + i * 0.3f, 0.8f), Vector2.zero, new Vector2(250f, 80f), new Color(0.75f, 0.9f, 1f), 32);

            var grid = UiRect("Grid", box, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.72f));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(250f, 220f);
            layout.spacing = new Vector2(18f, 18f);
            layout.childAlignment = TextAnchor.UpperCenter;

            // Item template
            var item = UiRect("ItemTemplate", grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var bg = item.gameObject.AddComponent<Image>();
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;
            var btn = item.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            var swatch = UiRect("Swatch", item, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            swatch.sizeDelta = new Vector2(100f, 100f);
            swatch.anchoredPosition = new Vector2(0f, -65f);
            var swatchImg = swatch.gameObject.AddComponent<Image>();
            swatchImg.sprite = circle;
            swatchImg.raycastTarget = false;
            var itemName = MakeText(item, "Item", 28, new Vector2(0f, 0.22f), new Vector2(1f, 0.42f), TextAnchor.MiddleCenter);
            itemName.color = new Color(0.15f, 0.15f, 0.2f);
            var status = MakeText(item, "0", 26, new Vector2(0f, 0.03f), new Vector2(1f, 0.22f), TextAnchor.MiddleCenter);
            status.color = new Color(0.1f, 0.4f, 0.7f);
            status.fontStyle = FontStyle.Bold;
            var view = item.gameObject.AddComponent<ShopItemView>();
            Set(view, "swatch", swatchImg);
            Set(view, "nameText", itemName);
            Set(view, "statusText", status);
            Set(view, "button", btn);
            Set(view, "background", bg);

            var shop = box.gameObject.AddComponent<ShopPanel>();
            Set(shop, "panel", root);
            Set(shop, "zone", zone);
            SetObjectArray(shop, "tabButtons", tabs);
            Set(shop, "grid", grid);
            Set(shop, "itemTemplate", view);
            Set(shop, "balanceText", balance);
            Set(shop, "closeButton", close);
            Set(shop, "previewView", previewView);
            Set(shop, "previewStandPoint", stand);
            Set(shop, "cameraRig", g.Camera);
            SetObjectArray(shop, "hideWhileOpen", new Object[] { g.Controls.gameObject, g.PauseButton.gameObject });
        }

        private static GameObject BuildPortal(Transform parent, Vector3 position, string targetScene, Material lit)
        {
            var root = new GameObject("Portal");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            CreateModel(BeaconModel, root.transform, 3f, SizeMode.Height, AssetDatabase.LoadAssetAtPath<Material>(MatEmissive) ?? lit, false);
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.2f, 0f);
            col.size = new Vector3(1.8f, 2.4f, 1.8f);
            var portal = root.AddComponent<LevelPortal>();
            Set(portal, "targetScene", targetScene);
            return root;
        }

        // ------------------------------------------------------------------ shared scene helpers

        private static void CreateBootstrapper()
        {
            var systems = new GameObject("[Systems]");
            var boot = systems.AddComponent<Bootstrapper>();
            Set(boot, "cosmeticDatabase", AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(CosmeticDbPath));
            Set(boot, "audioLibrary", AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibraryPath));
        }

        private static void SetupAtmosphere()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.75f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.8f, 0.97f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 140f;
        }

        private static void PlaceProp(string path, Transform parent, Vector3 position, float yRotation, Material mat, bool collide = false)
        {
            var model = InstantiateModelAsset(path, parent);
            if (model == null) return;
            ApplyMaterial(model, mat);
            model.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
            if (collide) AddMeshColliders(model);
            GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
        }

        private static void SpawnBolt(Transform parent, Vector3 xz, ref int index, string prefix, float lift = 0.8f)
        {
            var go = Spawn("Bolt", parent, OnGround(xz, lift));
            Set(go.GetComponent<Bolt>(), "persistentId", $"{prefix}_bolt_{index:000}");
            index++;
        }

        /// <summary>Lifts the robot so its lowest vertex in the Idle pose sits exactly on the CharacterController bottom.</summary>
        private static void GroundRobotModel(Transform wrapper)
        {
            if (wrapper.childCount == 0) return;
            Transform model = wrapper.GetChild(0);
            var anim = model.GetComponentInChildren<Animation>();
            var idle = anim != null ? anim.GetClip("Idle") : null;
            if (idle != null) idle.SampleAnimation(anim.gameObject, 0f);

            float minY = float.MaxValue;
            var baked = new Mesh();
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.BakeMesh(baked, true);
                var t = smr.transform;
                foreach (var v in baked.vertices)
                    minY = Mathf.Min(minY, (t.position + t.rotation * v).y);
            }
            Object.DestroyImmediate(baked);
            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true))
                minY = Mathf.Min(minY, mr.bounds.min.y);
            if (minY == float.MaxValue) return;
            model.position += Vector3.up * (wrapper.position.y - minY);
        }

        // ------------------------------------------------------------------ UI helpers

        private static Canvas CreateCanvas(out RectTransform safe)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            safe = UiRect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            ConfigureUiModule(es.AddComponent<InputSystemUIInputModule>());
            return canvas;
        }

        /// <summary>Binds the UI module to the project's InputSystem_Actions asset (persistent, unlike AssignDefaultActions).</summary>
        private static void ConfigureUiModule(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (asset == null)
            {
                Debug.LogWarning("[SuperBot] Assets/InputSystem_Actions.inputactions missing; UI module left with no actions.");
                return;
            }
            UnityEngine.InputSystem.InputActionReference Ref(string path)
            {
                var action = asset.FindAction(path);
                return action != null ? UnityEngine.InputSystem.InputActionReference.Create(action) : null;
            }
            module.actionsAsset = asset;
            module.point = Ref("UI/Point");
            module.leftClick = Ref("UI/Click");
            module.rightClick = Ref("UI/RightClick");
            module.middleClick = Ref("UI/MiddleClick");
            module.scrollWheel = Ref("UI/ScrollWheel");
            module.move = Ref("UI/Navigate");
            module.submit = Ref("UI/Submit");
            module.cancel = Ref("UI/Cancel");
            module.trackedDevicePosition = Ref("UI/TrackedDevicePosition");
            module.trackedDeviceOrientation = Ref("UI/TrackedDeviceOrientation");
        }

        /// <summary>Sets the built-in runtime font on every Text (DefaultControls may leave it empty).</summary>
        private static void FinishCanvas(Canvas canvas)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var t in canvas.GetComponentsInChildren<Text>(true))
                if (t.font == null || t.font.name != font.name) t.font = font;
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        private static RectTransform UiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void TransparentImage(RectTransform rt)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
        }

        private static Text MakeText(Transform parent, string text, int size, Vector2 anchorMin, Vector2 anchorMax, TextAnchor align)
        {
            var rt = UiRect("Text", parent, anchorMin, anchorMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color, int fontSize)
        {
            var rt = UiRect(label + "Button", parent, anchor, anchor);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = color;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = MakeText(rt, label, fontSize, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            t.color = new Color(0.12f, 0.12f, 0.18f);
            t.fontStyle = FontStyle.Bold;
            return btn;
        }

        /// <summary>Full-screen blocker (dim alpha) with a centered content box. Returns the box; root is the blocker.</summary>
        private static RectTransform MakePanel(Transform parent, string name, Vector2 boxMin, Vector2 boxMax, float dim, out GameObject root)
        {
            var overlay = UiRect(name, parent, Vector2.zero, Vector2.one);
            var oImg = overlay.gameObject.AddComponent<Image>();
            oImg.color = new Color(0f, 0f, 0f, dim);
            oImg.raycastTarget = true;
            var box = UiRect("Box", overlay, boxMin, boxMax);
            var bImg = box.gameObject.AddComponent<Image>();
            bImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.12f, 0.16f, 0.28f, 0.94f);
            root = overlay.gameObject;
            return box;
        }

        private static void TouchButtonUi(string name, Transform parent, ButtonAction action, string label, Vector2 fromBottomRight,
            float size, Color color, Sprite sprite)
        {
            var rt = UiRect(name, parent, new Vector2(1f, 0f), new Vector2(1f, 0f));
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = fromBottomRight;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            var button = rt.gameObject.AddComponent<TouchButton>();
            button.Action = action;
            EditorUtility.SetDirty(button);
            var t = MakeText(rt, label, Mathf.RoundToInt(size * 0.2f), Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
        }

        private static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

        private static Slider SliderAt(Transform parent, DefaultControls.Resources res, Vector2 min, Vector2 max)
        {
            var go = DefaultControls.CreateSlider(res);
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            var handle = slider.handleRect;
            if (handle != null) handle.sizeDelta = new Vector2(40f, 0f);
            return slider;
        }

        // ------------------------------------------------------------------ data helpers

        private static CosmeticItem Cosmetic(string id, string name, CosmeticType type, int price, GameObject hat, Color color, Gradient trail)
        {
            string path = $"{CosmeticsDir}/{id}.asset";
            var item = AssetDatabase.LoadAssetAtPath<CosmeticItem>(path);
            bool create = item == null;
            if (create) item = ScriptableObject.CreateInstance<CosmeticItem>();
            item.id = id;
            item.displayName = name;
            item.type = type;
            item.boltPrice = price;
            item.hatPrefab = hat;
            item.bodyColor = color;
            item.trailGradient = trail ?? TwoColor(Color.white, Color.white);
            if (create) AssetDatabase.CreateAsset(item, path);
            else EditorUtility.SetDirty(item);
            return item;
        }

        private static Gradient TwoColor(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static Gradient Rainbow()
        {
            var g = new Gradient();
            g.SetKeys(new[]
                {
                    new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.25f),
                    new GradientColorKey(new Color(0.4f, 1f, 0.4f), 0.5f), new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0.75f),
                    new GradientColorKey(new Color(0.8f, 0.4f, 1f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
