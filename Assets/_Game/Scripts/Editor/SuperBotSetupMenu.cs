using System.Collections.Generic;
using System.IO;
using SuperBot.CameraSystem;
using SuperBot.Collectibles;
using SuperBot.Controls;
using SuperBot.Core;
using SuperBot.Player;
using SuperBot.Save;
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
    /// <summary>
    /// One-click project setup: config assets, materials, prefabs (from the Polyfork GLBs) and a playable
    /// test scene. Every step is idempotent: re-running updates assets in place (GUIDs are kept).
    /// Requires the glTFast package so the .glb files import as models.
    /// </summary>
    public static partial class SuperBotSetupMenu
    {
        private const string Root = "Assets/_Game";
        private const string ArtRoot = Root + "/Art";
        private const string PrefabDir = Root + "/Prefabs";
        private const string MaterialDir = Root + "/Materials";
        private const string SoDir = Root + "/ScriptableObjects";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/TestIsland.unity";

        private const string PlayerConfigPath = SoDir + "/PlayerConfig.asset";
        private const string MatVertexLit = MaterialDir + "/M_VertexLit.mat";
        private const string MatEmissive = MaterialDir + "/M_VertexLit_Emissive.mat";
        private const string MatCore = MaterialDir + "/M_GoldenCore.mat";
        private const string MatParticle = MaterialDir + "/M_Particle.mat";

        private const string RobotModel = ArtRoot + "/Characters/service-robot-93f82e.glb";
        private const string AnimDir = ArtRoot + "/Animations";
        private const string BoltModel = ArtRoot + "/Props/Collectibles/tiny-bolt-and-nut-7c94d4.glb";
        private const string CoreModel = ArtRoot + "/Props/Collectibles/crystal-shard-9e0a83.glb";
        private const string CrateModel = ArtRoot + "/Props/Breakables/wooden-crate-e46521.glb";
        private const string JumpPadModel = ArtRoot + "/Props/Interactive/backyard-trampoline-2779ec.glb";
        private const string FlagModel = ArtRoot + "/Props/Checkpoints/flag-pole-015e53.glb";
        private const string BeetleModel = ArtRoot + "/Enemies/stag-beetle-base.glb";
        private const string PlatformModel = ArtRoot + "/Environment/Terrain/dock-platform-section-b36e7c.glb";
        private const string GrassBlob = ArtRoot + "/Environment/Terrain/grass-terrain-blob-c4f709.glb";
        private const string BridgeModel = ArtRoot + "/Environment/Bridges/log-plank-bridge-a5f74f.glb";
        private const string NatureDir = ArtRoot + "/Environment/Nature";

        private const string LayerPlayer = "Player";
        private const string LayerGround = "Ground";
        private const string LayerEnemy = "Enemy";
        private const string LayerCollectible = "Collectible";

        // ------------------------------------------------------------------ menu

        [MenuItem("SuperBot/Run All Setup Steps", priority = 0)]
        public static void RunAll()
        {
            ConfigurePlayerSettings();
            CreateConfigAssets();
            CreateMaterials();
            BuildPrefabs();
            CreateDefaultCosmetics();
            BuildAllScenes();
        }

        [MenuItem("SuperBot/0. Configure Player Settings (Landscape)", priority = 20)]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.productName = "Super Bot Adventure";
            EnsureLayers();
            Debug.Log("[SuperBot] Player settings: landscape only, layers ensured.");
        }

        [MenuItem("SuperBot/1. Create Config Assets", priority = 21)]
        public static void CreateConfigAssets()
        {
            EnsureFolder(SoDir);
            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PlayerConfig>(), PlayerConfigPath);
                Debug.Log($"[SuperBot] Created {PlayerConfigPath}");
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("SuperBot/2. Create Materials", priority = 22)]
        public static void CreateMaterials()
        {
            EnsureFolder(MaterialDir);
            Shader lit = Shader.Find("SuperBot/VertexLit");
            if (lit == null)
            {
                Debug.LogError("[SuperBot] Shader 'SuperBot/VertexLit' not found. Wait for import/compile and retry.");
                return;
            }

            UpsertMaterial(MatVertexLit, lit, m => { });
            UpsertMaterial(MatEmissive, lit, m =>
            {
                m.SetColor("_EmissionColor", new Color(0.6f, 0.45f, 0.1f));
                m.SetFloat("_Smoothness", 0.7f);
            });
            UpsertMaterial(MatCore, lit, m =>
            {
                m.SetColor("_Tint", new Color(1f, 0.85f, 0.35f));
                m.SetColor("_EmissionColor", new Color(1.2f, 0.8f, 0.15f));
                m.SetFloat("_Smoothness", 0.9f);
            });

            Shader particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particle != null) UpsertMaterial(MatParticle, particle, m => { });
            else Debug.LogWarning("[SuperBot] URP particle shader not found; particles will use default material.");

            AssetDatabase.SaveAssets();
            Debug.Log("[SuperBot] Materials ready.");
        }

        [MenuItem("SuperBot/3. Build Prefabs", priority = 23)]
        public static void BuildPrefabs()
        {
            if (!CheckModelsImported()) return;
            EnsureLayers();
            EnsureFolder(PrefabDir);
            if (AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit) == null) CreateMaterials();
            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath) == null) CreateConfigAssets();

            var lit = AssetDatabase.LoadAssetAtPath<Material>(MatVertexLit);
            var emissive = AssetDatabase.LoadAssetAtPath<Material>(MatEmissive);
            var coreMat = AssetDatabase.LoadAssetAtPath<Material>(MatCore);
            var particleMat = AssetDatabase.LoadAssetAtPath<Material>(MatParticle);

            // Particles first: other prefabs reference them.
            var dust = SavePrefab(BuildBurst("FX_Dust", new Color(0.85f, 0.8f, 0.7f), 10, 0.45f, 2.2f, 0.22f, -0.1f, particleMat), "FX_Dust");
            var sparkle = SavePrefab(BuildBurst("FX_Sparkle", new Color(1f, 0.85f, 0.3f), 10, 0.4f, 3.5f, 0.14f, 0f, particleMat), "FX_Sparkle");
            var debris = SavePrefab(BuildBurst("FX_Debris", new Color(0.6f, 0.4f, 0.2f), 14, 0.8f, 5f, 0.22f, 1.5f, particleMat), "FX_Debris");
            var dustFx = dust.GetComponent<PooledParticle>();
            var sparkleFx = sparkle.GetComponent<PooledParticle>();
            var debrisFx = debris.GetComponent<PooledParticle>();

            int groundMask = LayerMask.GetMask("Default", LayerGround);

            // ---- Bolt ----
            {
                var root = NewRoot("Bolt", LayerCollectible);
                CreateModel(BoltModel, root.transform, 0.55f, SizeMode.MaxDimension, emissive, true);
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.5f;
                col.center = new Vector3(0f, 0.25f, 0f);
                var bolt = root.AddComponent<Bolt>();
                Set(bolt, "collectFx", sparkleFx);
                Set(bolt, "groundMask", groundMask);
                SavePrefab(root, "Bolt");
            }
            var boltPrefab = LoadPrefab<Bolt>("Bolt");

            // ---- Golden Core ----
            {
                var root = NewRoot("GoldenCore", LayerCollectible);
                CreateModel(CoreModel, root.transform, 0.9f, SizeMode.MaxDimension, coreMat, true);
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.7f;
                col.center = new Vector3(0f, 0.2f, 0f);
                var core = root.AddComponent<GoldenCore>();
                Set(core, "collectFx", sparkleFx);
                SavePrefab(root, "GoldenCore");
            }

            // ---- Crate ----
            {
                var root = NewRoot("Crate", "Default");
                CreateModel(CrateModel, root.transform, 1f, SizeMode.MaxDimension, lit, false);
                FitBoxCollider(root, false);
                var crate = root.AddComponent<BreakableCrate>();
                Set(crate, "boltPrefab", boltPrefab);
                Set(crate, "debrisPrefab", debrisFx);
                SavePrefab(root, "Crate");
            }

            // ---- Jump pad ----
            {
                var root = NewRoot("JumpPad", "Default");
                var visual = CreateModel(JumpPadModel, root.transform, 2.4f, SizeMode.MaxDimension, lit, false);
                Bounds b = WorldBounds(visual.gameObject);
                var solid = new GameObject("Solid");
                solid.transform.SetParent(root.transform, false);
                SetLayerRecursive(solid, LayerGround);
                var solidCol = solid.AddComponent<BoxCollider>();
                solidCol.center = new Vector3(0f, b.size.y * 0.4f, 0f);
                solidCol.size = new Vector3(b.size.x, b.size.y * 0.8f, b.size.z);
                var trigger = root.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, b.size.y + 0.25f, 0f);
                trigger.size = new Vector3(b.size.x * 0.85f, 0.6f, b.size.z * 0.85f);
                var pad = root.AddComponent<JumpPad>();
                Set(pad, "visual", visual);
                SavePrefab(root, "JumpPad");
            }

            // ---- Checkpoint ----
            {
                var root = NewRoot("Checkpoint", "Default");
                var visual = CreateModel(FlagModel, root.transform, 3.5f, SizeMode.Height, lit, false);
                var col = root.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, 1.5f, 0f);
                col.size = new Vector3(2.5f, 3f, 2.5f);
                var cp = root.AddComponent<Checkpoint>();
                Transform flag = FindDeep(visual, "flag");
                Set(cp, "flagVisual", flag);
                Set(cp, "loweredOffset", -5f);
                Set(cp, "raiseSpeed", 8f);
                SavePrefab(root, "Checkpoint");
            }

            // ---- Moving platform ----
            {
                var root = NewRoot("MovingPlatform", LayerGround);
                CreateModel(PlatformModel, root.transform, 3.2f, SizeMode.MaxDimension, lit, false);
                FitBoxCollider(root, false);
                var rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                root.AddComponent<MovingPlatform>();
                SavePrefab(root, "MovingPlatform");
            }

            // ---- Beetle enemy ----
            {
                var root = NewRoot("Enemy_Beetle", LayerEnemy);
                var visual = CreateModel(BeetleModel, root.transform, 1.5f, SizeMode.MaxDimension, lit, false);
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                Bounds b = WorldBounds(visual.gameObject);
                col.radius = Mathf.Max(0.5f, Mathf.Max(b.extents.x, b.extents.z) * 0.8f);
                col.center = new Vector3(0f, Mathf.Max(0.4f, b.extents.y), 0f);
                var rb = root.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var enemy = root.AddComponent<PatrolEnemy>();
                Set(enemy, "visual", visual);
                Set(enemy, "boltPrefab", boltPrefab);
                Set(enemy, "deathFx", debrisFx);
                Set(enemy, "sightBlockers", groundMask);
                SavePrefab(root, "Enemy_Beetle");
            }

            // ---- Player ----
            {
                var root = NewRoot("Player", LayerPlayer);
                root.tag = "Player";
                var cc = root.AddComponent<CharacterController>();
                cc.height = 1.4f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.72f, 0f);
                cc.skinWidth = 0.02f;
                cc.stepOffset = 0.3f;
                cc.slopeLimit = 50f;

                var visualRoot = new GameObject("Visual").transform;
                visualRoot.SetParent(root.transform, false);
                var model = CreateModel(RobotModel, visualRoot, 1.45f, SizeMode.Height, lit, false);
                SetLayerRecursive(visualRoot.gameObject, LayerPlayer);
                AttachLegacyClips(model.gameObject);
                GroundRobotModel(model);

                var trailGo = new GameObject("Trail");
                trailGo.transform.SetParent(root.transform, false);
                trailGo.transform.localPosition = new Vector3(0f, 0.8f, -0.25f);
                var trail = trailGo.AddComponent<TrailRenderer>();
                trail.time = 0.3f;
                trail.minVertexDistance = 0.1f;
                trail.widthCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 0f);
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.emitting = false;
                if (particleMat != null) trail.sharedMaterial = particleMat;

                var hover = BuildLoop("JetpackFx", new Color(0.4f, 0.9f, 1f), particleMat);
                hover.transform.SetParent(root.transform, false);
                hover.transform.localPosition = new Vector3(0f, 0.9f, -0.3f);
                hover.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var motor = root.AddComponent<PlayerMotor>();
                Set(motor, "config", AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath));
                Set(motor, "spinHitMask", ~LayerMask.GetMask(LayerPlayer));
                var health = root.AddComponent<PlayerHealth>();
                Set(health, "visualRoot", visualRoot);
                var feedback = root.AddComponent<PlayerFeedback>();
                Set(feedback, "visualRoot", visualRoot);
                Set(feedback, "dustPrefab", dustFx);
                Set(feedback, "hoverParticles", hover.GetComponent<ParticleSystem>());
                var anim = root.AddComponent<PlayerAnimator>();
                Set(anim, "spinVisual", model);
                var cosmetics = root.AddComponent<PlayerCosmetics>();
                Set(cosmetics, "modelRoot", model);
                Set(cosmetics, "trail", trail);
                SavePrefab(root, "Player");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SuperBot] Prefabs built in " + PrefabDir);
        }

        // ------------------------------------------------------------------ scene helpers

        private static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static void PlaceGround(string modelPath, Transform parent, Vector3 position, float yRotation, float scale, Material mat)
        {
            var model = InstantiateModelAsset(modelPath, parent);
            if (model == null) return;
            ApplyMaterial(model, mat);
            model.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
            model.transform.localScale = Vector3.one * scale;
            SetLayerRecursive(model, LayerGround);
            AddMeshColliders(model);
            GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);
        }

        private static Vector3 OnGround(Vector3 xz, float lift)
        {
            var origin = new Vector3(xz.x, Mathf.Max(30f, xz.y + 10f), xz.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask(LayerGround), QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * lift;
            return new Vector3(xz.x, 0.5f + lift, xz.z);
        }

        private static GameObject Spawn(string prefabName, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefabGo(prefabName), parent);
            go.transform.position = position;
            return go;
        }

        private static void SpawnBolt(Transform parent, Vector3 xz, ref int index)
        {
            var go = Spawn("Bolt", parent, OnGround(xz, 0.8f));
            Set(go.GetComponent<Bolt>(), "persistentId", $"testisland_bolt_{index:000}");
            index++;
        }

        private static void SpawnCore(Transform parent, string id, Vector3 position)
        {
            var go = Spawn("GoldenCore", parent, position);
            Set(go.GetComponent<GoldenCore>(), "coreId", id);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != path) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ prefab helpers

        private enum SizeMode { Height, MaxDimension }

        private static GameObject NewRoot(string name, string layer)
        {
            var go = new GameObject(name);
            SetLayerRecursive(go, layer);
            return go;
        }

        /// <summary>
        /// Instantiates a GLB under a "Visual" wrapper, scales it to <paramref name="size"/>, puts its bottom at y=0
        /// and centers it on XZ. Returns the wrapper transform.
        /// </summary>
        private static Transform CreateModel(string path, Transform parent, float size, SizeMode mode, Material mat, bool centerY)
        {
            var wrapper = new GameObject("Model").transform;
            wrapper.SetParent(parent, false);
            var model = InstantiateModelAsset(path, wrapper);
            if (model == null) return wrapper;
            ApplyMaterial(model, mat);
            SetLayerRecursive(model, LayerMask.LayerToName(parent.gameObject.layer));

            Bounds b = WorldBounds(model);
            float current = mode == SizeMode.Height ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float scale = current > 0.0001f ? size / current : 1f;
            model.transform.localScale = Vector3.one * scale;

            b = WorldBounds(model);
            Vector3 offset = parent.position - b.center;
            offset.y = centerY ? parent.position.y + 0.25f - b.center.y : parent.position.y - b.min.y;
            model.transform.position += offset;
            return wrapper;
        }

        private static GameObject InstantiateModelAsset(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogError($"[SuperBot] Model not found or not imported: {path} (is glTFast installed?)");
                return null;
            }
            var go = Object.Instantiate(asset, parent);
            go.name = Path.GetFileNameWithoutExtension(path);
            return go;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
        }

        private static void AddMeshColliders(GameObject go)
        {
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        private static void FitBoxCollider(GameObject root, bool trigger)
        {
            Bounds b = WorldBounds(root);
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = trigger;
            col.center = root.transform.InverseTransformPoint(b.center);
            col.size = b.size;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
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

        private static void SetLayerRecursive(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) return;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        /// <summary>Adds the separate Polyfork clip GLBs to the robot's legacy Animation component.</summary>
        private static void AttachLegacyClips(GameObject model)
        {
            var anim = model.GetComponentInChildren<Animation>();
            var clips = new List<AnimationClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimDir }))
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (obj is AnimationClip c && !c.name.StartsWith("__preview") && !clips.Contains(c)) clips.Add(c);
            }
            if (clips.Count == 0)
            {
                Debug.LogWarning("[SuperBot] No animation clips found under " + AnimDir);
                return;
            }
            if (!clips[0].legacy)
            {
                Debug.LogWarning("[SuperBot] Clips are not legacy (glTFast Animation Method = Mecanim). " +
                                 "PlayerAnimator expects an Animator Controller in that case; skipping clip attach.");
                return;
            }
            if (anim == null) anim = model.AddComponent<Animation>();
            foreach (var c in clips)
            {
                if (anim.GetClip(c.name) == null) anim.AddClip(c, c.name);
            }
            if (anim.GetClip("Idle") != null) anim.clip = anim.GetClip("Idle");
            anim.playAutomatically = true;
        }

        private static PooledParticle BuildBurst(string name, Color color, int burst, float lifetime, float speed, float size, float gravity, Material mat)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.25f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go.AddComponent<PooledParticle>();
        }

        private static GameObject BuildLoop(string name, Color color, Material mat)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = color;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 40f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static GameObject SavePrefab(Component c, string name) => SavePrefab(c.gameObject, name);

        private static GameObject SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[SuperBot] Prefab saved: {path}");
            return prefab;
        }

        private static GameObject LoadPrefabGo(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");

        private static T LoadPrefab<T>(string name) where T : Component
        {
            var go = LoadPrefabGo(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        // ------------------------------------------------------------------ asset helpers

        private static bool CheckModelsImported()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RobotModel) != null) return true;
            Debug.LogError("[SuperBot] GLB models are not imported. Install glTFast (com.unity.cloud.gltfast) and make sure " +
                           "Tools/copy_polyfork_assets.sh has copied the art into Assets/_Game/Art.");
            return false;
        }

        private static void UpsertMaterial(string path, Shader shader, System.Action<Material> configure)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                configure(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                configure(mat);
                EditorUtility.SetDirty(mat);
            }
            mat.enableInstancing = true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var name in new[] { LayerPlayer, LayerGround, LayerEnemy, LayerCollectible })
            {
                if (LayerMask.NameToLayer(name) >= 0) continue;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    var p = layers.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(p.stringValue)) continue;
                    p.stringValue = name;
                    break;
                }
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ serialized field setters

        private static void Set(Object target, string field, object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[SuperBot] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                default: Debug.LogError($"[SuperBot] Unsupported value type {value.GetType()} for '{field}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVectorArray(Object target, string field, Vector3[] values) => SetVectorList(target, field, values);

        private static void SetVectorList(Object target, string field, Vector3[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).vector3Value = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
