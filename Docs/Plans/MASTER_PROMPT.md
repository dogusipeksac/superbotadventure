# SUPER BOT ADVENTURE — MASTER DEVELOPMENT PROMPT

This document is the complete specification for building **Super Bot Adventure** from its current state to a
finished, store-ready mobile game and beyond. You (the coding agent) implement it **phase by phase**.

**How to work with this document**
1. Read the whole file once before writing any code.
2. Implement **only the phase you are told to implement** (e.g. "Implement Phase 3"). Never start the next phase on
   your own.
3. Inside a phase, implement the numbered steps in order. When a step is ambiguous, choose the simplest solution
   that satisfies the "Done when" list and write your decision in the final report.
4. At the end of every phase print a report: files created, files modified, deviations from the spec, anything not
   done, and what the human must do (assets to download, accounts to create, settings to click).
5. The human reviews and commits after each phase. Never run git.

---------------------------------------------------------------------------------------------------

## PART 1 — PRODUCT VISION

### 1.1 Elevator pitch
A cheerful 3D low-poly collect-a-thon platformer for phones and tablets, played in landscape. The player controls
**Bolt**, a small service robot who wakes up on a floating archipelago that has lost its power. Bolt explores open
islands, collects **Bolts** (currency) and **Golden Cores** (progression), defeats rusty enemy bots, and restores
power island by island. Short sessions (3–10 minutes per goal), no timers, no energy system, fully offline.

This is an ORIGINAL game. Do not copy names, characters, levels, UI or music of any existing game.

### 1.2 Target
- Platforms: iOS 15+ (iPhone + iPad) and Android 7.0+ (API 24), landscape only.
- Performance: stable 60 FPS on mid-range devices (e.g. Galaxy A54, iPhone 11), 30 FPS minimum on low-end.
- Build size: < 150 MB (AAB / IPA).
- Audience: everyone (PEGI 3 / ESRB E). No violence beyond cartoon "bonk". No chat, no user data collection in v1.0.
- Languages: English + Turkish at launch (Phase 6), architecture ready for more.

### 1.3 Core loop
Explore island → collect bolts / break crates / defeat enemies → solve a small challenge → earn a Golden Core →
unlock the next island in the Hub → spend bolts on cosmetics → repeat.

### 1.4 Content scope for v1.0
- 1 Hub ("Power Plaza") + **4 islands**, each with **5 Golden Cores** (20 total), ~150 bolts per island.
- Island themes (all built from the Polyfork kit models already in the project; reuse with different tints/props):
  1. **Meadow Isle** (tutorial, green, trees, crates) — existing Island01, expanded.
  2. **Rusty Docks** (docks, cargo crates, cranes, moving platforms, water hazards).
  3. **Crystal Caves** (dark tint, glowing crystals, crumbling platforms, spinners, drones).
  4. **Sky Factory** (floating platforms, conveyors, turrets, final boss "Big Rusty").
- Enemies: Beetle (ground patrol), Drone (flying dive), Turret (stationary shooter), Roller (rolling charger),
  Boss "Big Rusty" (3-phase fight).
- Abilities: run, jump, double jump, jetpack hover, dash, spin attack, stomp, (unlock later) **Ground Pound** and
  **Magnet** (pull nearby bolts).
- Cosmetics: hats/antennas, body colors, jetpack trails, (Phase 7) premium items.

---------------------------------------------------------------------------------------------------

## PART 2 — TECHNICAL BASELINE (already in the repo — read before coding)

### 2.1 Engine and packages
Unity **6000.3.1f1**, URP 17.3 (Mobile_RPAsset), Input System 1.17 (legacy Input disabled), uGUI 2.0,
glTFast 6.21 (imports `.glb` models), Test Framework 1.6. Project root contains `Assets/`, `Packages/`,
`ProjectSettings/`, `Docs/Plans/` (Phase1.md, Phase2.md, Phase3.md, this file), `Tools/copy_polyfork_assets.sh`.

### 2.2 HARD RULES (apply to every phase)
1. Never modify, move or rename anything under `Assets/_Game/Art/**` (licensed models, gitignored).
   New art you generate (procedural meshes, materials) goes under `Assets/_Game/Generated/` via Editor code.
2. Never hand-write `.unity`, `.prefab`, `.mat`, `.asset`, `.controller`, `.anim` or `.meta` YAML. All scenes,
   prefabs, materials and ScriptableObject assets are produced by C# Editor code under
   `Assets/_Game/Scripts/Editor/` (menu `SuperBot/...`). Every builder must be idempotent (re-running updates
   in place and keeps GUIDs; use `PrefabUtility.SaveAsPrefabAsset` on the same path, `AssetDatabase.LoadAssetAtPath`
   before `CreateAsset`).
3. No third-party SDKs or packages before Phase 7 (and in Phase 7 only the ones listed there).
4. Input only through `UnityEngine.InputSystem` and the existing `MobileInputRouter`.
5. No `Find*`, `GetComponent` in hot paths, LINQ, string concatenation or allocations inside
   Update/FixedUpdate/LateUpdate. Cache everything. Use `PoolManager` for anything spawned repeatedly.
6. One class per file; file name == class name; `[SerializeField] private` fields; XML `<summary>` on public types.
7. Runtime assemblies must compile for iOS and Android: no `UnityEditor` outside `Editor/` folders.
8. All UI text goes through the localization system once it exists (Phase 6); before that, keep strings in one
   place per panel so they are easy to replace.
9. Keep `SuperBot/Run All Setup Steps` working: a clean clone + `Tools/copy_polyfork_assets.sh` + that menu
   must reproduce the whole game.
10. Do not run git. Do not delete files unless a step says so.
11. Tests are written in each phase but **run last**, always with a hard timeout:
    `perl -e 'alarm 420; exec @ARGV' /Applications/Unity/Hub/Editor/6000.3.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testFilter SuperBot.Tests -testResults Builds/test-results.xml -logFile Builds/test.log`
    If it times out, report it; do not retry in a loop.

### 2.3 Folder layout
```
Assets/_Game/
  Art/                (read-only Polyfork GLBs: Characters, Enemies, Animations, Environment/{Terrain,Nature,Bridges}, Props/{Breakables,Collectibles,Checkpoints,Interactive}, Cosmetics)
  Generated/          (meshes/materials produced by editor code — create when needed)
  Materials/  Prefabs/  Scenes/  ScriptableObjects/  Shaders/  Audio/  UI/
  Scripts/{Core,Save,Monetization,Audio,Data,Controls,Player,Camera,World,Collectibles,UI,Editor}
  Tests/PlayMode/
```

### 2.4 Existing architecture (reuse; extend, don't replace)
- **Core**: `ServiceLocator` (Register/Unregister/Get/TryGet; cleared on play start), `GameEvents` (static events +
  `Raise*`, reset in `ResetStatics`), `Bootstrapper` (persistent, `[DefaultExecutionOrder(-1000)]`, registers
  SaveService, EconomyService, HapticManager, IMonetizationService, CosmeticService, adds SaveAutosaver,
  PoolManager, SceneLoader, PauseService, AudioService), `SceneLoader.LoadScene(name)` (fade), `SceneNames`,
  `PauseService` (`GameplayActive` static), `PoolManager.SpawnStatic/DespawnOrDisable`, `PooledParticle`,
  `ObjectPool<T>`, `CoroutineRunner`, `EconomyService`, `HapticManager`, `CosmeticService`, `BootFlow`,
  `IDamageable`, `IStompable`, `IDeathHandler`.
- **Save**: `SaveService` (JSON in persistentDataPath, tmp-write + .bak + SHA-256 envelope), `SaveData`
  (bolts, collectedCores, collectedBoltIds, unlockedCosmetics, equipped*, lastCheckpointId, attConsentAsked,
  volumes, hapticsEnabled, version). Add fields with defaults; bump `version` and add a migration step when you
  change meaning of existing fields.
- **Monetization**: `IMonetizationService` + `MonetizationServiceStub` (rewarded Revive/DoubleBolts, Purchase,
  IsOwned, Restore, RequestTrackingConsent). Gameplay code only talks to the interface.
- **Audio**: `AudioService.Play(SfxId)`, `AudioService.Music(MusicId)`, `AudioLibrary` ScriptableObject, `SceneMusic`.
- **Controls**: `MobileInputRouter.Instance.State` (Move, LookDelta, JumpPressed/Held, DashPressed, AttackPressed),
  on-screen `VirtualJoystick`, `TouchButton(ButtonAction)`, `LookTouchZone`; keyboard/gamepad fallback.
- **Player**: `PlayerMotor` (CharacterController: coyote 0.12 s, jump buffer, double jump, hover, dash, spin,
  stomp via `IStompable`, `Launch`, `Knockback`, `Teleport`, `LastSafePosition`, events Jumped/DoubleJumped/
  Dashed/SpinStarted/OnHit), `PlayerHealth` (3 HP, invulnerability blink, Kill → IDeathHandler), `PlayerFeedback`,
  `PlayerAnimator` (legacy Animation clips Idle/Walk/Run/Jog/Hard_landing + procedural spin), `PlayerCosmetics`,
  `PlayerConfig`.
- **Camera**: `ThirdPersonCamera` (orbit, SphereCast collision, auto-recenter, `SetOverride`).
- **World**: `PatrolEnemy` (nested state classes), `BreakableCrate`, `JumpPad`, `MovingPlatform`, `Checkpoint`,
  `RespawnService`, `KillZone`, `LevelSession` (levelId, coreIds, SessionBolts, Completed), `LevelPortal`
  (targetScene, requiredCoreId), `InteractZone`, `CorePedestal`, `OrbitCamera`.
- **Collectibles**: `Bolt` (persistentId, `Pop`), `GoldenCore` (coreId).
- **UI**: `HudController`, `PauseMenu`, `SettingsPanel`, `DeathPanel`, `LevelCompletePanel`, `ShopPanel`,
  `ShopItemView`, `MainMenuController`, `SafeAreaFitter`, `UiSfx.Bind`.
- **Editor**: `SuperBotSetupMenu.cs` (+ `.Scenes.cs` partial): materials, prefabs, cosmetics, scenes
  Boot/MainMenu/Hub/Island01, helpers `CreateModel`, `PlaceGround`, `PlaceProp`, `OnGround`, `Spawn`, `SpawnBolt`,
  `SpawnCore`, `BuildPortal`, `BuildGameplayScaffold`, `MakeButton/MakeText/MakePanel`, `Set(obj,"field",value)`,
  `SetObjectArray/SetVectorArray/SetVectorList/SetStringArray`. `PlayModeStartScene` starts Play from Boot.
- **Shader**: `SuperBot/VertexLit` (vertex-color albedo × `_BaseColor` × `_Tint`, `_Smoothness`, `_EmissionColor`).
- Scenes in build: Boot, MainMenu, Hub, Island01. Layers: Default, Player, Ground, Enemy, Collectible, UI.
- Robot skeleton (Polyfork humanoids): `character/body/Hips/...` Mixamo-style bone names (Spine, Spine1, Spine2,
  Neck, Head, Left/RightShoulder, Arm, ForeArm, Hand, Left/RightUpLeg, Leg, Foot) + `attach-hat` node.

---------------------------------------------------------------------------------------------------

## PART 3 — PHASES

Each phase lists **Goal**, **Steps**, **Done when**. Phases 1 and 2 are finished.

### PHASE 3 — Content foundation, animation, enemies, game feel
Follow `Docs/Plans/Phase3.md` exactly (procedural poses, FlyingDrone, EnemyWaveTrigger, Spinner,
CrumblingPlatform, Island01 rebuilt with 3 cores, hit-stop, camera shake, HUD pop, cleanup, tests).
Additionally, after Phase3.md is done, change Island01 to **5 cores** (add: "Bolt Rush" — collect 30 bolts in the
start meadow within the island to make a core appear [`World/BoltCountGoal`], and "Crate Smash" — break all 8
crates on the dock area [`World/BreakAllGoal`]). Rename the scene/level to **Meadow Isle** in UI only
(scene name stays Island01).

**Done when:** Phase3.md "Done when" + Meadow Isle has 5 reachable cores.

### PHASE 4 — Progression framework and the Hub
**Goal:** data-driven levels, core-gated unlocks, objectives UI, new abilities.
1. `Data/LevelDefinition` ScriptableObject: levelId, sceneName, displayNameKey, theme color, `CoreDefinition[]`
   (coreId, titleKey, hintKey), coresRequiredToUnlock (int, counted over ALL cores), music id, thumbnail sprite.
   `Data/LevelDatabase` lists all levels. Register in Bootstrapper.
2. `Core/ProgressionService`: TotalCores, CoresIn(levelId), IsUnlocked(level), NextGoalHint(), raises
   `GameEvents.OnLevelUnlocked(levelId)` when a pickup crosses a threshold. Unlock thresholds:
   Meadow 0, Rusty Docks 3, Crystal Caves 8, Sky Factory 14, Boss arena 18.
3. Replace hard-coded `Island01Cores` usage with LevelDatabase everywhere (LevelSession, CorePedestal, Hub builder,
   tests). `LevelSession.coreIds` filled from the LevelDefinition by the scene builder.
4. **Hub rebuild ("Power Plaza")**: central plaza with 4 portals in a ring, each with a stone gate that shows
   "X / N cores" (world-space UI Canvas, Text) while locked and opens (gate model slides down) when unlocked;
   a big "Power Tower" in the middle whose emissive light strips turn on per 4 cores collected (5 strips);
   shop NPC; a "Wardrobe" mirror stand (opens ShopPanel on cosmetics tab); 20 core pedestals around the tower.
5. **Pause → Objectives tab**: list the current level's cores with title, hint, collected tick; bolt count of the
   level (collected/total, track via LevelSession counting `Bolt` objects with persistentId in the scene).
6. **Core collected cutscene**: 2 s celebration — player input off, camera orbits the player (use
   `ThirdPersonCamera.SetOverride` with an animated temp transform), robot plays `Dance` clip if present, banner
   "GOLDEN CORE!" + core title, then resume (or level complete if all cores).
7. **Abilities** (`Data/AbilityDefinition`, `Core/AbilityService`, SaveData.unlockedAbilities):
   - Ground Pound (unlocked by 5 cores): in air press Spin while holding Jump → slam down at 25 m/s, on land
     break crates/stun enemies in 2.5 m, big dust, camera shake. Add `PlayerMotor.GroundPound()` + state.
   - Bolt Magnet (unlocked by 10 cores): bolts within 4 m fly to the player (Bolt gets `Attract(Transform)`).
   - Unlock popup explaining the ability with an icon.
8. Tests: progression thresholds, unlock event, ability gating (EditMode tests are fine for pure logic —
   add `Tests/EditMode` asmdef).

**Done when:** Hub shows 4 portals with correct lock state; collecting cores updates tower, pedestals,
portals; abilities unlock at thresholds and persist.

### PHASE 5 — Islands 2–4, new enemies, boss
**Goal:** all v1.0 content.
1. Shared level kit (Editor helpers + runtime components):
   - `World/Conveyor` (moves player via `AddPlatformDelta` while standing on it; animated texture-free stripes via
     moving child planks).
   - `World/WaterHazard` (blue translucent plane using a new simple `SuperBot/WaterUnlit` shader with vertex wave;
     touching it = `TakeHit` + respawn at LastSafePosition, not full death).
   - `World/PressurePlate` + `World/Door` (plate opens linked door; optional timer).
   - `World/Collapsible` bridge segments, `World/Fan` (upward wind volume: adds vertical velocity while inside;
     works with hover).
   - `World/Lever` (InteractZone-based) and `World/ElevatorPlatform`.
2. Enemies:
   - `Turret` (Art/Enemies/defense-turret-12ba8d.glb): rotates toward player within 14 m, telegraphs 0.5 s, fires a
     pooled `Projectile` (slow, visible, 12 m/s); projectile can be spun back (reflect) to destroy the turret.
   - `Roller` (scaled `service-drone-bot` or barrel prop): patrols, sees player, revs 0.8 s, charges straight,
     stunned 2 s when hitting a wall → vulnerable to stomp only.
   - All enemies share an `EnemyBase` abstract class extracted from PatrolEnemy/FlyingDrone (health, loot, hit
     flash, death fx, sfx, contact damage, stomp/spin rules). Refactor existing enemies onto it without behaviour
     change.
3. **Rusty Docks** (`Island02`, theme: docks/cargo, tint warm orange): 5 cores — "Crane Climb" (stacked cargo
   crates + elevator), "Conveyor Maze", "Turret Alley" (destroy 3 turrets), "Sunken Crate" (water hazard + moving
   rafts), "Harbour Race" (reach the lighthouse beacon within 45 s after pressing a plate; `World/TimedChallenge`
   with HUD timer).
4. **Crystal Caves** (`Island03`, dark ambient, emissive crystals as light, fog darker; use `lair-crystal-column`
   and `crystal-shard` props): 5 cores — "Crumble Run", "Spinner Gauntlet", "Drone Nest" (wave), "Dark Path"
   (`World/LightPath` crystals light up when walked near, revealing the route), "Echo Switches"
   (3 pressure plates in order shown by blinking crystals).
5. **Sky Factory** (`Island04`, metallic tint, fans + conveyors): 4 cores + boss portal (needs 18 cores).
6. **Boss "Big Rusty"** (`BossArena` scene): a large scaled robot (service-robot model tinted rust, scale 4) in a
   round arena. `World/BossController` with phases:
   - Phase 1: stomps creating shockwave rings (jump over); after 3 stomps it is stunned 3 s → player ground-pounds
     its head (weak point trigger) = 1 damage.
   - Phase 2: summons 4 drones + shockwaves faster.
   - Phase 3: arena edge platforms fall (CrumblingPlatform), laser sweep (Spinner-like beam) + stomps.
   - 3 HP total, boss health bar UI, defeat cutscene: core rain (5 bonus bolts piles), power restored to the
     whole archipelago → `GameEvents.OnGameCompleted`, credits panel, then Hub with "Power Tower" fully lit and
     post-game sky (warmer lighting).
7. Each island: 120–160 bolts, 3–4 checkpoints, a portal back to Hub at start, LevelDefinition entry,
   music id, bolt/ core counts verified by an editor validator menu `SuperBot/Validate Levels` (checks unique ids,
   reachable spawn on Ground, every core id in LevelDatabase, every scene in Build Settings).

**Done when:** all 4 islands + boss playable start to end; validator passes; 20 cores total.

### PHASE 6 — Polish: UI/UX, audio, juice, localization, accessibility, performance
1. **UI art pass**: a consistent UI theme generated in code — rounded panels (9-slice sprite generated into
   `Generated/UI/` by Editor code drawing into a Texture2D and saving as PNG + importing as Sprite), icon set drawn
   procedurally (bolt, core, heart, pause, gear, lock, cart, play) as sprites; bouncy button press (scale 0.92),
   panel open/close tweens (unscaled, own tiny `UiTween` helper, no packages).
2. **Main menu**: title logo text with outline, "Continue" (last level) / "Play" , Settings, Credits; animated
   robot waving; version label.
3. **Loading tips** on the fade screen (localized list).
4. **Localization**: `Data/LocalizationTable` ScriptableObject (key → EN, TR), `Core/LocalizationService`
   (language from `Application.systemLanguage`, override in Settings, saved), `UI/LocalizedText` component
   (sets Text on enable and on language change). Replace every hard-coded UI string. Editor menu
   `SuperBot/Localization/Export CSV` and `Import CSV` (Assets/_Game/Localization/strings.csv).
5. **Audio**: complete `AudioLibrary` with every SfxId/MusicId. Since no audio files exist yet, write
   `Docs/AudioList.md` with the exact file names, durations and style per id, plus suggested CC0 sources
   (Kenney, freesound CC0 filter). The Editor menu `SuperBot/Audio/Auto-Assign Clips` maps files dropped into
   `Assets/_Game/Audio/Sfx/<SfxId>*.wav` and `Audio/Music/<MusicId>*.ogg` to the library automatically.
   Add footstep sfx on Walk/Run (timed to speed), wind loop while hovering, ambient loops per island.
6. **Juice**: bolt pickup combo (pitch rises on consecutive pickups within 0.6 s), core sparkle beam visible from
   distance (vertical emissive quad), enemy hit flash white 0.08 s, landing dust scaled by impact, dash after-image
   (3 pooled ghost meshes via `SkinnedMeshRenderer.BakeMesh` throttled to 20 Hz), screen-edge damage vignette.
7. **Camera polish**: look-ahead in movement direction, vertical framing when falling, smooth zoom-out on
   wide areas via `CameraZone` triggers (distance/pitch overrides).
8. **Accessibility / options**: camera sensitivity slider, invert Y, control size slider (scale on-screen
   buttons 0.8–1.3), left-handed layout (mirror controls), reduce screen shake toggle, colorblind-safe core icons
   (shape + color), subtitles not needed.
9. **Performance pass**: static batching on all level geometry, GPU instancing on bolts, LOD not required but
   cull distance per layer (props 70 m), shadow distance 35, disable shadows on small props, cap particles,
   object pooling everywhere, `Application.targetFrameRate` 60 with a "Battery saver (30 FPS)" option.
   Add `Editor` menu `SuperBot/Profile/Report Scene Stats` (tri count, renderers, colliders, materials per scene).
10. **Save safety**: cloud-agnostic export/import of the save as text (Settings → "Copy save code"/"Paste"),
    base64 of the envelope, for manual device transfer.

**Done when:** no hard-coded UI strings; TR/EN switch works live; options persist; 60 FPS target scene stats
documented in `Docs/Performance.md`.

### PHASE 7 — Release preparation (closed test + TestFlight), still no ad/IAP SDKs
1. `SuperBot/Release/Configure Player Settings`: product name "Super Bot Adventure", bundle id
   `com.dogusipeksac.superbotadventure` (both platforms — the human may change it), version 1.0.0 /
   build 1, IL2CPP, ARM64, Android min 24 / target highest installed, AAB on, iOS min 15.0, landscape only,
   Metal / Vulkan+GLES3, managed stripping Medium with a `link.xml` that preserves `SuperBot` types used by
   JsonUtility, app icons generated from a 1024 px PNG at `Assets/_Game/UI/AppIcon.png` if present.
2. iOS `PostProcessBuild`: Info.plist `NSUserTrackingUsageDescription`, `ITSAppUsesNonExemptEncryption = false`,
   `UIRequiresFullScreen = true`.
3. `Editor/BuildScripts`: `BuildAndroidAab()`, `BuildAndroidApkDev()`, `BuildIosXcode()`; batch-mode friendly;
   outputs under `Builds/`; fail with non-zero exit code on errors.
4. First-launch flow: language pick (if system language not EN/TR), "How to play" carousel (4 cards), privacy
   notice with link (URL in a `Data/AppConfig` ScriptableObject; empty hides the button).
5. Crash-safe: global `Application.logMessageReceived` handler writing the last 200 log lines to
   persistentDataPath `last_log.txt` (Settings → "Copy debug info" for testers).
6. `Docs/Release.md`: Play Console closed testing (14 days, 12+ testers), TestFlight internal/external steps,
   store listing texts (EN + TR, short + full description, keywords), screenshot sizes and which scenes to
   capture, content rating answers, data safety answers (no data collected in v1.0), age rating 4+.
7. Developer cheats (development builds only, `#if DEVELOPMENT_BUILD || UNITY_EDITOR`): 3-finger tap opens a
   debug menu: add 1000 bolts, unlock all levels, give all cores, toggle invincible, teleport to each core.

**Done when:** `BuildAndroidAab` and `BuildIosXcode` succeed in batch mode; Release.md complete.

### PHASE 8 — v1.1 Monetization (only now SDKs are allowed)
Allowed packages: Google Mobile Ads Unity plugin (with UMP consent), `com.unity.purchasing`, Apple ATT via
`com.unity.ads.ios-support`.
1. `MonetizationConfig` ScriptableObject: rewarded ad unit ids per platform (default: Google test ids), product
   ids, toggles.
2. `MonetizationService : IMonetizationService` (selected in Bootstrapper when scripting define `SUPERBOT_ADS` is
   set; otherwise the stub stays): UMP consent → ATT (iOS) → AdMob init; rewarded preload/retry with backoff
   (no reward unless OnUserEarnedReward); IAP non-consumables with receipt-based `IsOwned`, `RestorePurchases`,
   localized prices (`GetLocalizedPrice(productId)` added to the interface; stub returns "--").
3. Placements (only rewarded, never forced interstitials in v1.1): Revive, DoubleBolts at level complete, and a
   daily "Bonus Crate" in the Hub (watch ad → 50 bolts, once per 24 h, `DateTime.UtcNow` saved).
4. Premium cosmetics: 3 IAP items (crown hat, rainbow+ trail, flame jetpack) + "Supporter Pack" bundle; ShopPanel
   shows localized price and Restore button on iOS.
5. Remove-ads not needed (no forced ads). Parental-safe: ads requested with `TagForChildDirectedTreatment` /
   max ad content rating "G".
6. `Docs/Monetization.md`: AdMob setup, app-ads.txt, store product creation, test devices, updated data-safety
   form, ATT copy.

**Done when:** stub build and `SUPERBOT_ADS` build both compile; test ads show on device (human verifies).

### PHASE 9 — Live content and retention (post-launch)
1. Daily goals (3 per day from a pool: "collect 100 bolts", "break 10 crates", "defeat 5 enemies"), rewards bolts.
2. Achievements screen (local; hook for Game Center / Play Games later behind an interface `IAchievementService`).
3. Time trial mode per island (ghost of best run recorded as positions at 10 Hz, replay as transparent robot).
4. New island template guide `Docs/AddingAnIsland.md` so a new island = one LevelDefinition + one builder method.
5. Seasonal cosmetic sets (data only) and a simple remote-config-free "event calendar" (dates in a
   ScriptableObject; app checks local date).

---------------------------------------------------------------------------------------------------

## PART 4 — DESIGN NUMBERS (single source of truth; put them in ScriptableObjects, not in code)
- Player: walk 7 m/s, accel 60, decel 70, air control 0.6, gravity -30 (×1.6 falling), max fall -25, jump 2.2 m,
  double jump 1.6 m, hover 1.2 s at -1.5 m/s, dash 18 m/s × 0.18 s (cooldown 0.6), spin 0.4 s radius 1.4,
  3 HP, 1 s invulnerability.
- Economy: bolt = 1, bolt pile = 5, crate = 3, enemy = 2–3, boss = 50; cosmetics 50–400 bolts; a full island
  yields ~150 bolts; all non-premium cosmetics affordable after finishing the game once (~600 bolts total
  cost < 750 earned).
- Level layout: max gap without help 5 m horizontal / 2.2 m vertical; with double jump 3.8 m vertical;
  checkpoint every 60–90 s of play; never put a core behind a mandatory enemy wave in Meadow Isle.
- Camera: distance 7, pitch 15 (−20..60), height 1.2, FOV 55.
- UI: reference resolution 1920×1080, match 0.5; touch targets ≥ 120 px; HUD inside SafeArea.

## PART 5 — QUALITY BAR FOR EVERY PHASE
- 0 compile errors, 0 warnings in `_Game` code; no errors in the Console when playing the full loop.
- `SuperBot/Run All Setup Steps` rebuilds everything from scratch.
- New gameplay code has at least one PlayMode or EditMode test; tests run last with the timeout command.
- Report at the end of the phase (files, deviations, human TODOs).
