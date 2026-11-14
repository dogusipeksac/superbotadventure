# Phase 1 — Core foundation + playable test scene

Unity 6000.3.1f1, URP 17.3, Input System 1.17, landscape mobile (iOS + Android). C# only.
All game code lives under `Assets/_Game/`. Namespace root: `SuperBot` (sub-namespaces below).

## HARD RULES (do NOT break)
- Do NOT touch: `Assets/_Game/Art/**` (models + .meta), `Assets/Settings/**`, `Assets/TutorialInfo/**`, `ProjectSettings/**` (except what step 1 says), `.gitignore`, `README.md`, `Tools/**`.
- Do NOT add AdMob, Unity IAP, Firebase or any 3rd-party SDK. Only the packages listed in step 1.
- Do NOT hand-write `.unity`, `.prefab`, `.mat`, `.asset` or `.meta` YAML files. Scenes/prefabs/materials are created by the Editor script in step 12 when the user runs a menu item.
- Do NOT use the legacy `UnityEngine.Input` class. Use the Input System package (`UnityEngine.InputSystem`), including `EnhancedTouch` for touch.
- No `FindObjectOfType` / `GameObject.Find` in Update. No LINQ or allocations in Update/FixedUpdate.
- Each class in its own file, file name == class name. Use `[SerializeField] private` fields, not public fields.
- Every runtime script must compile for iOS and Android (no `UnityEditor` usage outside `Editor/` folders or `#if UNITY_EDITOR`).

## 1. Packages
Edit `Packages/manifest.json` dependencies only — add:
- `"com.unity.cloud.gltfast": "6.21.0"`
- `"com.unity.shadergraph": "17.3.0"` (if not already a dependency)
Do not change or remove any other line.

## 2. Folder + assembly layout
Create folders (empty folders need no files):
```
Assets/_Game/Scripts/Core
Assets/_Game/Scripts/Save
Assets/_Game/Scripts/Monetization
Assets/_Game/Scripts/Player
Assets/_Game/Scripts/Camera
Assets/_Game/Scripts/Input
Assets/_Game/Scripts/World
Assets/_Game/Scripts/Collectibles
Assets/_Game/Scripts/UI
Assets/_Game/Scripts/Data
Assets/_Game/Scripts/Editor
Assets/_Game/Shaders
```
Create `Assets/_Game/Scripts/SuperBot.asmdef` (name `SuperBot`, references `Unity.InputSystem`, `Unity.TextMeshPro` NOT needed, autoReferenced true) and `Assets/_Game/Scripts/Editor/SuperBot.Editor.asmdef` (name `SuperBot.Editor`, references `SuperBot`, `Unity.InputSystem`, `glTFast`, includePlatforms `["Editor"]`).

## 3. Vertex-color shader (all Polyfork models use vertex colors COLOR_0, no textures)
Create `Assets/_Game/Shaders/SuperBotVertexLit.shader` — a hand-written HLSL URP shader (NOT a Shader Graph):
- `Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }`
- Properties: `_BaseColor` (Color, default white), `_Tint` (Color, white, multiplies vertex color — used later for robot body color), `_Smoothness` (Range 0-1, 0.2), `_EmissionColor` (HDR Color, black).
- Passes: `UniversalForward` (main light Lambert + `SampleSH` ambient + main light shadows via `_MAIN_LIGHT_SHADOWS` / `_MAIN_LIGHT_SHADOWS_CASCADE` multi_compile, fog via `multi_compile_fog`), `ShadowCaster`, `DepthOnly`, `DepthNormals`.
- Final albedo = vertexColor.rgb * _BaseColor.rgb * _Tint.rgb. Emission added after lighting.
- SRP Batcher compatible: all properties inside `CBUFFER_START(UnityPerMaterial)`.
- No additional lights loop needed (mobile).

## 4. Core (`Scripts/Core`, namespace `SuperBot.Core`)
- `ServiceLocator` (static): `Register<T>(T service)`, `Get<T>()`, `TryGet<T>(out T)`, `Clear()`. Dictionary<Type, object>.
- `GameEvents` (static class with C# `event Action` fields): `OnBoltsChanged(int total)`, `OnGoldenCoreCollected(string coreId)`, `OnPlayerDied`, `OnPlayerRespawned`, `OnCheckpointReached(string checkpointId)`, `OnPlayerLanded(float impactSpeed)`. Plus static `Raise...` methods for each.
- `Bootstrapper : MonoBehaviour` — `[DefaultExecutionOrder(-1000)]`, DontDestroyOnLoad singleton (destroy duplicates). In Awake: sets `Application.targetFrameRate = 60`, `Screen.orientation = ScreenOrientation.LandscapeLeft` + `autorotateToLandscapeLeft/Right = true`, portrait false; `Screen.sleepTimeout = SleepTimeout.NeverSleep`. Creates and registers `SaveService`, `MonetizationServiceStub` (as `IMonetizationService`), `HapticManager`, `EconomyService`. Calls `SaveService.Load()`.
- `ObjectPool<T> where T : Component` (plain C# class): ctor(prefab, parent, prewarm), `Get(Vector3 pos, Quaternion rot)`, `Release(T)`. Uses `Stack<T>`.
- `PoolManager : MonoBehaviour` — holds pools keyed by prefab instance ID; `Spawn(Component prefab, pos, rot)` / `Despawn(Component obj)`. Registered in ServiceLocator in its Awake.
- `PooledParticle : MonoBehaviour` — requires ParticleSystem; on `OnParticleSystemStopped` despawns itself via PoolManager (set `main.stopAction = Callback` in Awake).

## 5. Save (`Scripts/Save`, namespace `SuperBot.Save`)
- `SaveData` [Serializable]: `int version = 1`, `int bolts`, `List<string> collectedCores`, `List<string> collectedBoltIds` (one-time level bolts), `List<string> unlockedCosmetics`, `string equippedHat`, `string equippedBodyColor`, `string equippedTrail`, `string lastCheckpointId`, `bool attConsentAsked`, `float musicVolume = 1`, `float sfxVolume = 1`, `bool hapticsEnabled = true`.
- `SaveService`: path `Path.Combine(Application.persistentDataPath, "save.json")`. `Load()`, `Save()`, `Data` property, `MarkDirty()`.
  - Safe write: serialize with `JsonUtility.ToJson`, write to `save.json.tmp`, then replace `save.json` (keep previous as `save.json.bak`; use `File.Replace` when target exists, else `File.Move`).
  - Integrity: store `{ "payload": "<json>", "hash": "<sha256 hex of payload + salt const>" }` wrapper (`SaveEnvelope` class). On load: if hash mismatch or parse error → try `.bak` → else new `SaveData`. Wrap everything in try/catch, log warnings, never throw.
  - Autosave: `SaveAutosaver : MonoBehaviour` (added by Bootstrapper to its own GameObject) calls `Save()` if dirty every 5 s, and always in `OnApplicationPause(true)` and `OnApplicationQuit`.

## 6. Monetization stub (`Scripts/Monetization`, namespace `SuperBot.Monetization`)
- `IMonetizationService`:
  - `bool IsInitialized { get; }`, `void Initialize(Action onComplete)`
  - `void RequestTrackingConsent(Action<bool> onResult)` (iOS ATT later)
  - `bool IsRewardedReady(RewardedPlacement placement)`, `void ShowRewarded(RewardedPlacement placement, Action<bool> onFinished)` (bool = reward granted)
  - `void Purchase(string productId, Action<bool> onResult)`, `bool IsOwned(string productId)`, `void RestorePurchases(Action<bool> onResult)`
- `enum RewardedPlacement { Revive, DoubleBolts }`
- `MonetizationServiceStub : IMonetizationService` — always ready; `ShowRewarded` logs `[MonetizationStub]` and invokes `onFinished(true)` on next frame via a coroutine runner (use a hidden `CoroutineRunner : MonoBehaviour` in Core, DontDestroyOnLoad). `RequestTrackingConsent` sets `SaveData.attConsentAsked = true` and returns `false`. Purchases: adds productId to `SaveData.unlockedCosmetics`, returns true. No real SDK code anywhere.

## 7. Economy + Haptics
- `SuperBot.Core.EconomyService`: `int Bolts`, `AddBolts(int amount)`, `bool TrySpend(int amount)`; writes `SaveData.bolts`, marks dirty, raises `GameEvents.OnBoltsChanged`.
- `SuperBot.Core.HapticManager`: `Light()`, `Medium()`, `Heavy()`. Respects `SaveData.hapticsEnabled`. Android: `Handheld.Vibrate()` only for Heavy (others no-op for now); iOS: `Handheld.Vibrate()` for Heavy; Editor: `Debug.Log`. Add a TODO comment for native light haptics in v1.1. Wrap platform code in `#if UNITY_ANDROID || UNITY_IOS`.

## 8. Mobile input (`Scripts/Input`, namespace `SuperBot.InputSystem`)
- `PlayerInputState` (plain class, single shared instance owned by `MobileInputRouter`): `Vector2 Move` (-1..1), `Vector2 LookDelta` (pixels this frame), `bool JumpPressed` (true only on the frame pressed), `bool JumpHeld`, `bool DashPressed`, `bool AttackPressed`. Method `ConsumeFrame()` clears one-frame flags + LookDelta at end of frame.
- `VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler` (uGUI): floating joystick in left-half zone; background + knob RectTransforms; radius serialized (default 90); dead zone 0.1. Writes to router `Move`.
- `TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler` with `enum ButtonAction { Jump, Dash, Attack }`; scales to 0.9 while pressed.
- `LookTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler` — transparent Image covering the right half BEHIND the buttons; accumulates drag delta into router `LookDelta`. Tracks only the first pointerId that pressed it.
- `MobileInputRouter : MonoBehaviour` — `[DefaultExecutionOrder(-500)]`, exposes `State`. Also keyboard/mouse/gamepad fallback for Editor testing via Input System devices directly: WASD/left stick → Move; Space/south button → Jump; LeftShift/west → Dash; mouse left/east → Attack; mouse delta while right mouse held / right stick*10 → LookDelta. Calls `State.ConsumeFrame()` in `LateUpdate`.

## 9. Player (`Scripts/Player`, namespace `SuperBot.Player`)
- `PlayerConfig : ScriptableObject` (`[CreateAssetMenu(menuName="SuperBot/Player Config")]`) with tunables: `moveSpeed 7`, `acceleration 60`, `deceleration 70`, `airControl 0.6`, `rotationSpeed 720`, `gravity -30`, `fallGravityMultiplier 1.6`, `maxFallSpeed -25`, `jumpHeight 2.2`, `doubleJumpHeight 1.6`, `coyoteTime 0.12`, `jumpBufferTime 0.12`, `hoverDuration 1.2`, `hoverFallSpeed -1.5`, `dashSpeed 18`, `dashDuration 0.18`, `dashCooldown 0.6`, `spinDuration 0.4`, `spinRadius 1.4`, `landSquashAmount 0.25`, `landSquashTime 0.15`, `hardLandingSpeed 14`.
- `PlayerMotor : MonoBehaviour` — requires `CharacterController`. Movement is camera-relative (camera forward flattened on XZ). Implement exactly:
  - Ground check via `controller.isGrounded` + small sphere cast fallback; track `lastGroundedTime`.
  - Jump buffer: store `lastJumpPressedTime`; jump executes if `time - lastJumpPressedTime <= jumpBufferTime` AND (`grounded` OR `time - lastGroundedTime <= coyoteTime`). Velocity y = `sqrt(2 * jumpHeight * -gravity)`. Consume buffer + coyote after jump.
  - Double jump: one extra jump in air (`doubleJumpHeight`), reset on landing.
  - Jetpack hover: after the double jump is used, holding Jump while falling (vy < 0) clamps vy to `hoverFallSpeed` for up to `hoverDuration` total per airtime; exposes `bool IsHovering`.
  - Variable gravity: `gravity * fallGravityMultiplier` when vy < 0; clamp to `maxFallSpeed`.
  - Dash: horizontal burst along facing for `dashDuration`, ignores gravity during dash, one air dash per airtime, cooldown.
  - Spin attack (Attack button): `spinDuration` window, `Physics.OverlapSphereNonAlloc` (preallocated Collider[16]) each FixedUpdate during spin; calls `IDamageable.TakeHit(Vector3 source)` on hits (interface in `SuperBot.Core`).
  - External forces API: `Launch(float upVelocity)` (jump pads; resets double jump + hover), `AddPlatformDelta(Vector3 delta)` (moving platforms).
  - Raises `GameEvents.RaiseOnPlayerLanded(impactSpeed)` on ground contact after airtime > 0.1 s.
  - Exposes read-only state for animation: `Vector3 Velocity`, `bool IsGrounded`, `bool IsDashing`, `bool IsSpinning`, `int JumpCount`.
- `PlayerFeedback : MonoBehaviour` — on `OnPlayerLanded`: squash & stretch on a `visualRoot` Transform (scale y = 1 - landSquashAmount, xz = 1 + amount/2, back to 1 over landSquashTime with ease-out, no tween library), spawn pooled dust particle prefab at feet, `HapticManager.Light()` (Medium if impact > hardLandingSpeed). Stretch slightly (y 1.1) on jump.
- `PlayerAnimator : MonoBehaviour` — drives an `Animator` with parameters `Speed` (float, horizontal speed normalized), `Grounded` (bool), `VerticalSpeed` (float), `Jump` (trigger), `Spin` (trigger), `Dash` (trigger), `HardLand` (trigger). Uses `Animator.StringToHash` cached ints. If animator is null, do nothing (no errors).
- `PlayerHealth : MonoBehaviour, IDamageable` — 3 HP, 1 s invulnerability after hit, knockback via motor. On death raise `OnPlayerDied`, then `RespawnService.Respawn()` after 1 s (revive via rewarded ad comes in Phase 2; leave a commented hook).
- `RespawnService : MonoBehaviour` (World) — knows current checkpoint transform; `Respawn()` teleports player (disable CharacterController while setting position), restores HP, raises `OnPlayerRespawned`. Kill plane: `KillZone : MonoBehaviour` trigger that calls `PlayerHealth.Kill()`.

## 10. Camera (`Scripts/Camera`, namespace `SuperBot.CameraSystem`)
- `ThirdPersonCamera : MonoBehaviour` on the Main Camera (no Cinemachine). Serialized: `target`, `distance 6`, `minDistance 1.2`, `height offset 1.5`, `yaw sensitivity 0.15`, `pitch sensitivity 0.1`, `pitch min -20 max 60`, `start pitch 20`, `followSmoothTime 0.08`, `collisionRadius 0.3`, `collisionMask`, `autoRecenterDelay 2.5`, `autoRecenterSpeed 90`.
  - LateUpdate: yaw/pitch from `PlayerInputState.LookDelta`; pivot = smoothed target position + height offset.
  - **SphereCast collision**: `Physics.SphereCast(pivot, collisionRadius, -forward, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore)`; if hit, distance = max(minDistance, hit.distance - 0.05). Pull in instantly, ease back out (SmoothDamp).
  - Auto-recenter yaw behind player's movement direction when no look input for `autoRecenterDelay` and player is moving.

## 11. World + collectibles
- `Collectibles/Bolt : MonoBehaviour` — trigger collider, spins + bobs (in Update via sin, no allocations). On player enter: `EconomyService.AddBolts(value)` (value default 1), spawn pooled sparkle particle, `HapticManager.Light()`, then return itself to pool if spawned from pool, otherwise `SetActive(false)`. Optional `string persistentId`: if set and in `collectedBoltIds`, disable on Start; add id when collected.
- `Collectibles/GoldenCore : MonoBehaviour` — like Bolt but `string coreId` (required), stored in `collectedCores`, raises `OnGoldenCoreCollected`, `HapticManager.Heavy()`, `SaveService.Save()` immediately.
- `World/BreakableCrate : MonoBehaviour, IDamageable` — on `TakeHit` OR when player lands on top with downward velocity (check via `OnControllerColliderHit` forwarded from PlayerMotor: add `public event Action<ControllerColliderHit> OnHit` on PlayerMotor), spawns `boltCount` (default 3) bolts from PoolManager with small upward scatter, spawns debris particle, disables itself.
- `World/JumpPad : MonoBehaviour` — trigger; calls `PlayerMotor.Launch(launchVelocity = 18)`, plays squash on its own visual, `HapticManager.Medium()`.
- `World/MovingPlatform : MonoBehaviour` — moves kinematic Rigidbody between serialized waypoint list (ping-pong, speed, wait time) in FixedUpdate using `MovePosition`; while player stands on it (trigger box on top), passes per-frame delta to `PlayerMotor.AddPlatformDelta`.
- `World/Checkpoint : MonoBehaviour` — trigger; `string checkpointId`; on first enter set `RespawnService` current checkpoint, save `lastCheckpointId`, raise event, raise flag visual (lerp a child up), Medium haptic.
- `World/PatrolEnemy : MonoBehaviour, IDamageable` — simple state machine with states as nested classes implementing `IEnemyState { Enter(); Tick(float dt); Exit(); }`: `Patrol` (walk between 2+ waypoints), `Chase` (player within 6 m, line of sight), `Stunned` (1 s after hit), `Dead` (disable, drop 2 bolts). Damages player on contact (`PlayerHealth.TakeHit`) unless player is spinning or landing on its head (then enemy takes hit + player gets `Launch(10)`). Uses `CharacterController` or kinematic transform move; no NavMesh in Phase 1.

## 12. UI (`Scripts/UI`, namespace `SuperBot.UI`)
- `SafeAreaFitter : MonoBehaviour` — on a full-screen RectTransform; applies `Screen.safeArea` to anchorMin/Max; re-applies when safeArea or resolution/orientation changes (check in Update, cheap compare).
- `HudController : MonoBehaviour` — bolt counter (uGUI `Text` or TextMeshPro — use `TMPro.TextMeshProUGUI` only if `com.unity.ugui` 2.0 includes TMP, which it does in Unity 6; then add `Unity.TextMeshPro` to the asmdef refs), 3 golden core icons, 3 HP icons. Subscribes to GameEvents in OnEnable, unsubscribes in OnDisable.

## 13. Editor tooling (`Scripts/Editor`, namespace `SuperBot.EditorTools`)
`SuperBotSetupMenu` with menu items:
1. `SuperBot/1. Create Config Assets` — creates `Assets/_Game/ScriptableObjects/PlayerConfig.asset` if missing.
2. `SuperBot/2. Create Materials` — creates `Assets/_Game/Materials/M_VertexLit.mat` using `SuperBotVertexLit` shader, and `M_VertexLit_Emissive.mat` (emission yellow for cores/bolts).
3. `SuperBot/3. Remap Art Materials` — for every `.glb` under `Assets/_Game/Art`, nothing to change on import (glTFast importer) — instead the prefab builder (step 4) replaces renderers' sharedMaterials with `M_VertexLit` when instantiating the model.
4. `SuperBot/4. Build Prefabs` — creates prefabs under `Assets/_Game/Prefabs/` by instantiating the GLB main asset via `AssetDatabase.LoadAssetAtPath<GameObject>` and adding components:
   - `Player.prefab`: root with CharacterController (height 1.6, radius 0.4, center y 0.8), PlayerMotor, PlayerHealth, PlayerFeedback, PlayerAnimator; child `Visual` containing `Art/Characters/service-robot-93f82e.glb` instance (materials → M_VertexLit). Tag `Player`, layer `Player` (create the layer via TagManager SerializedObject if missing — this is the ONLY allowed ProjectSettings change; also add tag/layers `Ground`, `Enemy`, `Collectible`).
   - `Bolt.prefab` (Art/Props/Collectibles/tiny-bolt-and-nut-7c94d4.glb, scaled so it is ~0.5 m, SphereCollider trigger r 0.5), `GoldenCore.prefab` (crystal-shard-9e0a83.glb, emissive mat), `Crate.prefab` (wooden-crate-e46521.glb, BoxCollider fitted to renderer bounds), `JumpPad.prefab` (backyard-trampoline-2779ec.glb), `Checkpoint.prefab` (flag-pole-015e53.glb), `Enemy_Beetle.prefab` (Art/Enemies/stag-beetle-base.glb), `MovingPlatform.prefab` (dock-platform-section-b36e7c.glb + kinematic Rigidbody + BoxCollider), `Dust.prefab` and `Sparkle.prefab` (ParticleSystem built in code, URP particle unlit material, PooledParticle).
   - Fit colliders using combined `Renderer.bounds`.
5. `SuperBot/5. Build Test Scene` — creates `Assets/_Game/Scenes/TestIsland.unity`: Directional light (shadows soft), Bootstrapper + PoolManager + RespawnService GameObject, ground made of 6–8 `grass-terrain-blob` instances (MeshCollider, layer Ground) forming an island, a few trees/rocks from `Art/Environment/Nature`, a `log-plank-bridge` to a second small island, 20 bolts in lines, 4 crates, 1 jump pad to a high ledge, 1 moving platform, 2 checkpoints, 1 patrol beetle, 3 golden cores (one on the high ledge, one behind the bridge, one on the moving platform route), KillZone box under the island (y = -15), Main Camera with ThirdPersonCamera (collisionMask = Ground|Default), Canvas (Screen Space Overlay, CanvasScaler scale with screen size 1920x1080 match 0.5) → SafeArea root → joystick (left), Jump/Dash/Attack buttons (right, Jump biggest), LookTouchZone (right half, behind buttons), HUD top. EventSystem with `InputSystemUIInputModule`. Adds the scene to Build Settings as index 0.
Every menu item must be idempotent (re-running overwrites/updates instead of duplicating) and log what it created.

## 14. Done when
- Project compiles with 0 errors and 0 warnings from `_Game` code.
- Running menu items 1→5 in order produces the scene; pressing Play in Editor with keyboard/mouse: robot moves camera-relative, jumps, double jumps, hovers, dashes, spins, breaks crates, collects bolts (HUD updates), jump pad launches, moving platform carries player, falling off respawns at checkpoint, camera does not clip through ground/trees.
- `save.json` appears in persistentDataPath and survives restart.
- At the end, print a short summary listing every file you created/modified.
