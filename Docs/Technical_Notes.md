# Technical notes

Companion to the root README. Implementation details that a reviewer may want after the project is running.

## Initialization order

1. `LoadingManager` starts `LoadSceneAsync("Game")` with `allowSceneActivation = false`.
2. The fill stays at or below 0.9 until Unity’s activation threshold **and** `Minimum Display Seconds` (default 3, unscaled, from Loading `Start`) are both met. Then the bar is set to 1 and Game activates.
3. `LoadSceneAsync` progress is asset load only. It does not measure Game `Awake` or `PoolManager` prewarm.
4. After activation, all Game `Awake` methods run (pools are created here), then `Start`. `RoundManager.Start` sets `IsRunning` and begins the 60-second clock.
5. Loading has no music. `GameAudioController` fades in `BG_Music` when the round is running.

Restart (`EndRoundUI`) reloads the active Game scene by build index.

## Input routing

`PlayerInputRouter` stores Left / Right / Fire as 2 players × 2 sources (`Ui`, `Editor`). Effective hold is the OR of both sources. Releases on one source do not clear the other. `CancelAllInput` clears both sources, calls `ShipController.ClearMovement` and `WeaponController.ClearFire`, and increments `InputEpoch`.

`HoldControl` and `AimAreaControl` use `InputSource.Ui`. Editor adapters pass `InputSource.Editor`.

## Threat spawn positions

Safe X = intersection of:

- camera horizontal extent minus `Horizontal Camera Inset` (0.8)
- `BreachBoundary` and `CleanupBottom` collider bounds
- inset by `Widest Threat Half Width` (0.35)

If that range is empty, one error is logged and nothing is spawned.

After the first spawn of a round, `ThreatSpawner` scores up to 6 random candidates against the last 2–3 X positions (preferred gap = 28% of the safe width). Kind is chosen independently. History resets on Patrol (new round).

## Feedback and audio

`GameFeedbackController` listens to success events only (`ShotFired`, `ThreatDamaged`, `ThreatDestroyed`, `HullLost`, `ShieldIntercepted`, ability `Activated`). Failed shots and rejected abilities are silent and have no muzzle flash.

`GameAudioController` creates a fixed set of `AudioSource`s in `Awake` (music, two fire voices, two priority voices, game-over, victory). A fire cue is skipped if both fire voices are busy. It does not stop a destroy or hull-hit cue.

## What “exhausted pool” looks like

The shot or spawn is skipped. Gameplay continues. One `Debug.LogWarning` per pool type for the session.
