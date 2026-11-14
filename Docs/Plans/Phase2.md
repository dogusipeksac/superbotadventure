# Phase 2 — Game loop, meta systems, cosmetics

Builds on Phase 1 (read `Docs/Plans/Phase1.md` HARD RULES — they all still apply).
Existing code lives in `Assets/_Game/Scripts/{Core,Save,Monetization,Controls,Player,Camera,World,Collectibles,UI,Editor}`.
Namespaces: `SuperBot.Core`, `.Save`, `.Monetization`, `.Controls`, `.Player`, `.CameraSystem`, `.World`, `.Collectibles`, `.UI`, `.EditorTools`.
Scenes and prefabs are built by `SuperBotSetupMenu` (Editor) — extend it, never hand-write YAML.

Scope of Phase 2 = everything needed for a closed-test build: boot → main menu → hub → island → back to hub,
pause/settings, revive + 2x bolts through `IMonetizationService`, ATT flow, cosmetics shop, audio hooks.
NOT in scope: new level art, flying enemy, new animation clips (Phase 3).

## 0. Fixes from Phase 1 play test
1. TestIsland ground: grass blobs leave sky gaps between them. In `BuildTestScene` change main-island spacing from 8.5 to 7.0 and island-2 positions accordingly; keep bridge reaching island 2.
2. Player visual may sink into ground on uneven blobs: in `BuildPrefabs` set `cc.skinWidth = 0.02f`, `cc.center.y = 0.7f + skinWidth`, and after `CreateModel` lift the robot model so its lowest renderer bound is at y = 0.0 (current code uses bind-pose bounds; recompute after `Animation.Sample()` of the Idle clip at t=0 if an Animation component exists).
3. `ThirdPersonCamera`: start pitch 15, distance 7, heightOffset 1.2.

## 1. Scene flow
- `Core/SceneLoader : MonoBehaviour` (child of Bootstrapper, registered in ServiceLocator): `Load(string sceneName)` → fade to black (full-screen `CanvasGroup` on its own overlay Canvas, sortingOrder 1000, 0.25 s), `SceneManager.LoadSceneAsync`, fade in. Ignores calls while loading. Uses unscaled time.
- `Core/SceneNames` static class with consts: `Boot = "Boot"`, `MainMenu = "MainMenu"`, `Hub = "Hub"`, `Island01 = "Island01"`.
- Boot scene contains only Bootstrapper and a `BootFlow : MonoBehaviour` that: on iOS (first launch only, `!save.Data.attConsentAsked`) calls `IMonetizationService.RequestTrackingConsent`, then loads MainMenu. On other platforms loads MainMenu immediately.
- Bootstrapper must work when any scene is played directly in the Editor (keep current behaviour).

## 2. Level session + completion
- `World/LevelSession : MonoBehaviour` (one per gameplay scene): serialized `string levelId`, `string[] coreIds` (the cores in this level). Tracks bolts collected this session (`GameEvents.OnBoltsChanged` delta while level active) and cores.
- When all `coreIds` are in `save.Data.collectedCores` after a pickup → raise new `GameEvents.OnLevelCompleted(levelId)`, freeze player input, show Level Complete panel (step 4).
- Add `GameEvents.OnLevelCompleted`, `OnGamePaused(bool)`. Reset them in `ResetStatics`.
- `World/LevelPortal : MonoBehaviour` (trigger): `string targetScene`, optional `string requiredCoreId` (locked until collected; shows a lock visual child when locked). Used in the hub to enter Island01, and in Island01 to return to Hub.

## 3. Pause + settings
- `UI/PauseMenu`: pause button (top-right HUD, 96 px) → panel with Resume, Settings, Back to Hub (Hub only in gameplay scenes), and in Settings: Music slider, SFX slider, Haptics toggle, "Restore Purchases" button, Privacy Policy button (`Application.OpenURL` with a serialized URL, empty = hidden). Writes to `SaveData`, `MarkDirty()`.
- Pausing: `Time.timeScale = 0`, `AudioListener.pause = true`, raise `OnGamePaused(true)`; `MobileInputRouter` disabled while paused.
- Auto-pause in `OnApplicationPause(true)` / `OnApplicationFocus(false)` while in a gameplay scene.

## 4. Death → revive (rewarded stub) and level complete → 2x bolts
- Change `PlayerHealth.Kill()`: instead of auto-respawn, raise `OnPlayerDied` and let `UI/DeathPanel` decide.
- `DeathPanel`: shows after 0.6 s: "Revive here (Watch ad)" → `IMonetizationService.ShowRewarded(RewardedPlacement.Revive)` → on true: revive at the player's last grounded position (track it in `PlayerMotor`: last position where grounded on layer Ground and not on a MovingPlatform), with full HP. "Checkpoint" → `RespawnService.Respawn`. If `!IsRewardedReady` hide the ad button. Max 1 ad revive per life (reset on checkpoint).
- `UI/LevelCompletePanel`: shows bolts collected this session, "Collect" and "Collect x2 (Watch ad)" (`RewardedPlacement.DoubleBolts` → `EconomyService.AddBolts(sessionBolts)` once more). Then `SceneLoader.Load(SceneNames.Hub)`.
- `PlayerHealth` gets `public void ReviveInPlace(Vector3 pos)`.

## 5. Cosmetics (ScriptableObject based)
- `Data/CosmeticItem : ScriptableObject`: `string id`, `string displayName`, `CosmeticType type` (Hat, BodyColor, Trail), `int boltPrice`, `string iapProductId` (empty = bolts only), `GameObject hatPrefab`, `Color bodyColor`, `Gradient trailGradient`, `Sprite icon`.
- `Data/CosmeticDatabase : ScriptableObject`: `List<CosmeticItem> items`, `CosmeticItem GetById(string)`. Loaded via a serialized reference on Bootstrapper and registered in ServiceLocator.
- `Core/CosmeticService`: `IsUnlocked(item)` (price 0, in `unlockedCosmetics`, or `IMonetizationService.IsOwned(iapProductId)`), `TryBuyWithBolts(item)`, `BuyWithIap(item, Action<bool>)`, `Equip(item)` (writes `equippedHat/BodyColor/Trail`), event `OnEquippedChanged`.
- `Player/PlayerCosmetics : MonoBehaviour` on Player prefab: applies equipped items at Start and on `OnEquippedChanged`:
  - Hat: instantiate `hatPrefab` under the robot's `attach-hat` transform (find by name), destroy previous.
  - Body color: `MaterialPropertyBlock` setting `_Tint` on all robot renderers (shader already supports `_Tint`).
  - Trail: `TrailRenderer` child (time 0.3, width 0.25→0, URP particle unlit material) with the gradient; emitting only while `IsDashing || IsHovering`.
- Editor menu `SuperBot/5. Create Default Cosmetics`: creates `Assets/_Game/ScriptableObjects/Cosmetics/` with: 3 body colors (Default white price 0, Sky Blue 50, Lava Red 120), 3 trails (Cyan 0, Gold 80, Rainbow 200), 2 hats (None 0, Gear Antenna 150 using `Art/Cosmetics/whimsical-gear-hub-88deb5.glb` as a prefab `Hat_GearAntenna`), plus a `CosmeticDatabase.asset` referencing all, assigned to the Bootstrapper in every built scene.
- `UI/ShopPanel` (opened by a Shop terminal trigger in the Hub — `World/InteractZone` that shows an "Open" button while the player is inside): three tabs (Hats/Colors/Trails), grid of items (icon or color swatch, name, price or "Owned"/"Equipped"), Buy/Equip button, bolt balance top-right. Price buttons disabled when not enough bolts. Robot preview = the real player standing on the shop pad (camera moves to a serialized preview Transform while the shop is open).

## 6. Audio
- `Core/AudioService` (MonoBehaviour on Bootstrapper): one music `AudioSource` (loop, cross-fade 1 s between tracks), pool of 8 SFX `AudioSource`s. `PlaySfx(SfxId id, Vector3? pos = null)`, `PlayMusic(AudioClip)`. Volumes from `SaveData`.
- `Data/AudioLibrary : ScriptableObject`: `enum SfxId { Jump, DoubleJump, Land, Dash, Spin, Bolt, Core, CrateBreak, JumpPad, Checkpoint, Hurt, Die, EnemyDie, UiClick, Purchase }` → `AudioClip[]` variants + volume + pitch range. Null clips are silently skipped (assets come later).
- Hook calls into existing scripts (motor events, Bolt, GoldenCore, Crate, JumpPad, Checkpoint, PlayerHealth, PatrolEnemy, UI buttons). Do not add audio files.

## 7. Scenes built by the setup menu
- `SuperBot/6. Build Boot + Menu + Hub`:
  - `Boot.unity`: Bootstrapper, BootFlow.
  - `MainMenu.unity`: camera orbiting a robot on a small island (reuse grass blob + 2 trees), title text "Super Bot Adventure", Play button → Hub, Settings button (reuses settings panel), version label `Application.version`.
  - `Hub.unity`: small single-island hub (grass blobs), player spawn, Shop terminal (use `Art/Characters/companion-bot-170074.glb` as the shop NPC) with InteractZone + ShopPanel, LevelPortal to Island01 (visual: `waypoint-beacon-23ba00.glb` + cyan emissive), 3 core pedestals showing collected cores, HUD + pause.
- Rename/duplicate current TestIsland build into `Island01.unity` with LevelSession (`levelId = "island01"`, its 3 core ids) and a LevelPortal back to Hub near the start.
- Build Settings order: Boot, MainMenu, Hub, Island01 (TestIsland stays out of the build).
- `PlayModeStartScene` keeps working (index 0 = Boot).

## 8. Done when
- Compiles with 0 errors/warnings; `SuperBot/Run All Setup Steps` (extended to include steps 5–6) builds everything on a clean clone.
- Boot → MainMenu → Hub → portal → Island01 → collect 3 cores → Level Complete (x2 works via stub) → Hub shows 3 cores.
- Dying offers Revive (stub ad) and Checkpoint; pause works and survives app backgrounding.
- Buying + equipping each cosmetic type changes the robot immediately and persists after restart.
