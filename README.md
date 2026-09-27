# Operation Cross-Fire: Protecting the Orbital Corridor

A local, same-device, two-player cooperative round. Both players share one ship. Player 1 uses the left controls and Player 2 the right. Roles start as Pilot and Gunner, then reverse at 20 seconds and again at 40 seconds. The round lasts 60 seconds.

This README describes the **current Unity project**, not an earlier plan.

---

## Setup and run

**Unity:** 6000.3.9f1 (`ProjectSettings/ProjectVersion.txt`).

**Scenes in the Build Profile (this order):**

1. `Assets/_OperationCrossFire/Scenes/Loading.unity`
2. `Assets/_OperationCrossFire/Scenes/Game.unity`

**Editor**

1. Open the project in that Unity version.
2. Use **File → Build Profiles** and confirm both scenes are enabled in the order above.
3. Press Play from the **Loading** scene (or from Game if you want to skip the loader). Loading stays on screen for at least 3 unscaled seconds, then activates Game. Restart on the end panel reloads **Game** only; it does not return to Loading.
4. Editor keyboard and mouse are compiled only under `UNITY_EDITOR` (`PilotKeyboardTestInput`, `GunnerEditorInputAdapter`).

**Android**

- Product name: `OperationCrossFire`
- Package: `com.muhammadhasnain.operationcrossfire`
- Orientation: landscape left/right (portrait autorotate off)
- Minimum API: 25
- Active Input Handling: **Input Manager (Old)** (`activeInputHandler: 0`)
- Game scene EventSystem uses `StandaloneInputModule`

Build: **File → Build Settings → Android → Build And Run**. Grant USB debugging on the device.

**Tested device:** TODO — the project files do not record a device model. If the submission device is Redmi 24116RACCG, add that confirmation next to the Profiler captures.

---

## Controls

Player identity is fixed by panel. **Left = Player 1. Right = Player 2.** Role content inside each panel swaps at Quantum Flux (`TouchControlPanel`).

### Android (touch)

Both sides can be used at the same time. Each `HoldControl` / `AimAreaControl` captures its own pointer id.

**Pilot panel**

| Control | Action |
|---------|--------|
| Left / Right | Hold to move the ship |
| Boost | Tap; 1 s duration, 1.75× speed, 4 s cooldown |

**Gunner panel**

| Control | Action |
|---------|--------|
| Aim area | Drag; the aim icon stays inside that player’s pad and returns to center on release |
| Fire | Hold; shots fire every 0.25 s while held if a pool slot is free |
| Shield | Tap; 1.5 s duration, 5 s cooldown |

Aim and Fire only apply when that player is the current Gunner and the round is running (`PlayerInputRouter.AcceptsAimInput` / `CanGunnerCommand`). After Flux or round end, captured pointers are cancelled (`InputEpoch`) and aim icons snap home.

### Editor

Input goes to the **current** Pilot or Gunner, not a fixed player number.

| Input | Action |
|-------|--------|
| A / ← or D / → | Pilot move |
| Space | Pilot Boost |
| Mouse move (game view, not over UI) | Gunner aim |
| Left mouse (game view) | Gunner fire |
| Right mouse (game view) | Gunner Shield |

After Flux or round end, held keys/buttons must be released before they apply again.

---

## Architecture

| Area | Scripts |
|------|---------|
| Round timer, score, win/loss, Flux milestones | `RoundManager` |
| P1/P2 role ownership | `RoleManager` |
| Command gate (player first, then role) | `PlayerInputRouter` |
| Touch panels and holds | `TouchControlPanel`, `HoldControl`, `AimAreaControl` |
| Editor adapters | `PilotKeyboardTestInput`, `GunnerEditorInputAdapter` |
| Ship move / Boost / Shield / hull | `ShipController`, `BoostAbility`, `ShieldAbility`, `ShipHealth` |
| Aim, fire, lasers | `ReticleController`, `WeaponController`, `PlayerProjectile` |
| Pools | `PoolManager` |
| Threats | `ThreatSpawner`, `FallingThreat`, `EnemyProjectile`, `BoundaryTrigger` |
| HUD / end panel | `HUDController`, `EndRoundUI` |
| Loading | `LoadingManager` |
| Audio / cosmetics | `GameAudioController`, `GameFeedbackController` |
| Background cover | `CameraCoverBackground` |

There is no event bus, no FindObjectOfType gameplay lookup, and no generic damage framework. Laser hits call `FallingThreat.TakeDamage`. Ship contacts are handled on `ShipHealth`.

More acquire/reset and loading detail: [Docs/Technical_Notes.md](Docs/Technical_Notes.md).

---

## Role switching and progression

`RoundManager` is the only timer. Phases: Patrol (0–20 s), Alert (20–40 s), Critical (40–60 s).

**Warnings:** Flux banners at 17/18/19 s and 37/38/39 s (`QUANTUM FLUX IN 3…` / `2…` / `1…`).

**At 20 s and 40 s** (`FluxStarting` then `SwapRoles` then `PhaseChanged`):

1. `PlayerInputRouter.CancelAllInput` increments `InputEpoch`, clears movement and fire.
2. Boost and Shield cancel immediately.
3. Roles swap: 20 s → P1 Gunner / P2 Pilot; 40 s → P1 Pilot / P2 Gunner.
4. HUD shows `QUANTUM FLUX — ROLES REVERSED` for 1.25 s.
5. Difficulty: Alert and Critical multiply falling-threat speed by **1.25**. Critical multiplies spawn interval by **0.7** and enemy-projectile speed by **1.5**. Breach Hazards are rolled only from Alert onward (20% of Alert/Critical rolls).

---

## Gameplay rules

Values are from the **Game scene and prefabs**, not from an override in code.

| Rule | Current value |
|------|----------------|
| Round length | 60 s |
| Starting hull | 3; 1 s invulnerability after a hull loss |
| Hull display | HullPoint1–3 on/off. Authored HullText is a static label and is not written by `HUDController` |
| Boost | 1 s, 1.75×, 4 s cooldown; current Pilot only |
| Shield | 1.5 s, 5 s cooldown; current Gunner only; Flux disables the Shield object immediately |
| Fire cadence | 0.25 s |
| Enemy | 1 HP, 10 points |
| Debris | 2 HP, 15 points |
| Breach Hazard | 3 HP, 25 points |
| Spawn | Interval 1.5 s (1.05 s in Critical); first spawn after 0.8 s; base fall speed 2 |
| Win | Survive 60 s (`RoundEndReason.Victory`) |
| Loss | Hull 0, or a live Breach Hazard overlapping `BreachBoundary` |

**Collisions (component checks, not tags)**

- Player laser vs threat: 1 damage. Score only on the killing hit. Partial hits flash the sprite only.
- Ship vs Enemy / Debris / enemy shot: Shield intercepts and pools the other object with no hull loss; otherwise 1 hull, then the other object is pooled. Same physics frame cannot remove two hull.
- Ship vs Breach Hazard: ignored by `ShipHealth` (breach loss is the boundary only).
- `CleanupBottom` / `CleanupTop`: return falling threats, enemy shots, or player lasers to their pools. No score.

After `RoundEnded`: spawning stops, input is cancelled, pools return in-use objects, music fades, victory or game-over plays once, the end panel blocks gameplay UI. Restart/Quit stay disabled until the fade finishes.

---

## Pooling and performance

`PoolManager` instantiates five fixed arrays in `Awake` under `PoolRoot`. No further gameplay `Instantiate`/`Destroy`.

| Type | Prewarm (Game scene) |
|------|----------------------|
| Player projectile | 32 |
| Enemy | 24 |
| Debris | 16 |
| Breach Hazard | 12 |
| Enemy projectile | 48 |

Acquire walks the array for `!IsInUse`. Exhaustion logs one warning per type and skips the spawn/shot. Return calls `PrepareForPool` (inactive, collider off, velocity cleared, authored color restored). Round end returns every in-use instance.

Threat spawn X is clamped to the camera view ∩ `BreachBoundary` ∩ `CleanupBottom`, inset by the widest threat half-width (0.35). A last-resort despawn runs only after a threat’s top is below both the breach strip and the camera bottom.

Custom physics layers in `TagManager`: Player, Shield, Threat, PlayerProjectile, EnemyProjectile, Boundary. Pairwise enable bits live in **Project Settings → Physics 2D → Layer Collision Matrix**; I did not re-derive every bit from the packed mask in this document.

---

## Device and Profiler evidence

TODO — these two files were **not** in the repository when this README was written. When you add them, keep these names:

- `Docs/Evidence/Android_Profiler_CPU_Timeline_Redmi.png`
- `Docs/Evidence/Android_Profiler_GC_Hierarchy_Redmi.png`

**What those captures are specified to show** (selected frame only, not a round average):

- Unity Profiler attached to the Redmi.
- CPU **Timeline** and **Hierarchy** for **frame 1633**.
- About **32.13 ms** CPU time, **0.38 ms** Scripts, **26.13 ms** VSync, and **3.3 KB** GC in that frame.

Do not read this as Critical-phase timing unless another capture or note says so. Do not attribute the 3.3 KB to a specific gameplay script without a Hierarchy row that names it.

---

## Evidence guide

TODO — no Android screenshots or device video were present in the project tree at documentation time. After you add files under `Docs/Evidence/`, list each filename and one visible fact (for example: both players touching; Flux banner; end panel). Do not claim a feature from a file that does not show it.

---

## Assumptions, incomplete work, and next steps

**Assumptions I made for this exercise**

- One 60-second round is enough; no menu, pause, or save.
- Player 1 stays on the left half of the screen even after becoming Gunner.
- Loading may show a 3-second minimum display while Game assets load; `LoadSceneAsync` progress is not Game `Awake` or pool warm-up.
- Editor Play from Game is valid for iteration; the Android path is Loading → Game.

**Known incomplete or scene-side items**

- End-round **card vs full-screen overlay** is still a RectTransform layout task if the decorative panel remains inset (`Size Delta` −1000, −600). Input blocking is handled in script (blocker + `blocksRaycasts`).
- `ThreatSpawner` history/candidate fields use script defaults (3 / 6 / 0.28) unless set in the Inspector.
- Pools do not grow at runtime. A full pool drops that spawn or shot.
- `LoadSceneAsync` reaching 0.9 does not include Game pool instantiation; one activation hitch is possible.

**If I had more time**

- Add the device screenshots, a short play video, and the two Profiler PNGs under `Docs/Evidence/`.
- Tune spawn pressure from a recorded Patrol / Alert / Critical playthrough, one change at a time.
- Finish the end-panel overlay/card hierarchy so the art is not stretched.

---

## AI assistance

I specified and directed the gameplay, rules, controls, progression, system behavior, and implementation requirements.

AI assistance was used for **coding support**: helping write, review, or refine C# under my direction. I reviewed, integrated, configured, and tested the work.

The game design and the decisions about gameplay and systems are mine. I do not claim that every line was written by hand, and I do not claim that AI was unused in coding.
