# Deferred DLL Fixes — Future Iterations

These are the DLL-bound stability issues that were **not** patched in this
branch. Each lives in compiled IL in `Assembly-CSharp.dll` and is deferred
because the exact fault location can't be pinned down from the repo alone —
they need a reporter's `output_log.txt` stack trace and/or an in-game repro,
and several sit in high-blast-radius code (torpedo guidance, save/load, combat)
where a speculative guard could silently corrupt gameplay or saves.

Method names below were resolved against the current assembly with
`mono Inspect.exe <dll> "Type::Method"` (see `README.md`).

## General methodology (apply per issue)

1. **Get a stack trace.** Ask the reporter for `output_log.txt` (or
   `Player.log`). The NRE/exception frame names the exact method + IL offset.
2. **Localize the fault.** `mono Inspect.exe <dll> "Type::Method"` and walk to
   the offset; identify the field/array/cast that is null/out-of-range.
3. **Write the minimal guard.** Prefer a fix that is a no-op on the valid path:
   - `void` method, first deref of field `X` is the fault → inject
     `if (this.X == null) return;` at entry (equivalent to the crash path minus
     the throw).
   - value-returning method (e.g. a getter) → return a safe default
     (`GetTerrainHeightAtPositionFromHeightMap` could clamp indices / return 0f).
   - array index → bounds-check before the load.
4. **Verify structurally.** After patching, confirm: type/method/body counts
   unchanged, instruction delta equals exactly what you injected, and every
   body re-parses (the `verify.cs` pattern used for #190). Keep changes to one
   method per fix.
5. **Play-test under Proton.** CI can't run the game; nothing here is validated
   at runtime until someone launches it.

## Deferred issues

### #184 — NullReferenceException in `Torpedo.FixedUpdate()`
- **Where:** `Torpedo::FixedUpdate` (and likely the parallel `Enemy_Torpedo::FixedUpdate`).
- **Risk:** HIGH — runs every physics frame for every torpedo; central to combat.
- **Needs:** stack trace to identify which field (target? wire? sensor?) is null.
- **Approach:** guard the specific deref; do **not** blanket try/catch the whole
  body (would mask guidance failures). A per-frame NRE spams the log rather than
  hard-crashing, so this is log-noise + dud torpedoes, not a freeze.

### #151 — Crash in "GetTorpedoID" on loading a save
- **Where:** save path `SaveLoadManager::LoadCampaign`/`PopulateLevelLoadData`;
  torpedo id lookups `PlayerFunctions::GetPlayerTorpedoIDInTube` /
  `GetPlayerTorpedoIDInTubeOnInit`; `VesselAI::GetTorpedoToAttack`.
- **Risk:** HIGH — touches save deserialization; a wrong guard can brick saves.
- **Needs:** the failing save + stack trace. Likely a weapon/tube id in the save
  no longer maps to a current weapon definition (data drift between versions).
- **Approach:** validate the id against the current weapon table and skip/clamp
  invalid entries instead of indexing blindly. Tie-in: also a DATA concern
  (renamed weapons) — cross-check `weapons/` ids referenced by old saves.

### #188 / #98 — NRE on missile launch (Harpoon vs KA-27; Trafalgar)
- **Where:** weapon launch flow around `DotModWeapon` (`set_IsLaunched`, launch
  coroutine) and the target-acquisition path.
- **Risk:** MEDIUM-HIGH — combat path.
- **Needs:** both stack traces. #188 is specifically a SAM/Harpoon engaging a
  helicopter (KA-27) — suspect a null target-aspect/track when the target is an
  aircraft vs a ship.
- **Approach:** null-guard the aircraft-target branch; return without firing
  rather than dereferencing a missing track.

### #132 — ArgumentOutOfRange when landing a helicopter
- **Where:** `Helicopter::MoveHelicopter` + landing state in
  `DotModPlayerAircraft` (`LandingCommanded`/`LandingComplete`).
- **Risk:** MEDIUM — player-helicopter feature.
- **Needs:** stack trace (which collection index).
- **Approach:** bounds-check the landing-pad/waypoint index.

### #120 — Crash when changing ship course
- **Where:** steering/movement — `VesselMovement` (e.g. `SubDivePlanes`),
  `HelmManager`.
- **Risk:** MEDIUM — frequent player action.
- **Needs:** stack trace; repro (surface ship vs sub? specific vessel?).

### #94 — NRE in terrain height sampling
- **Where:** `LevelLoadManager::GetTerrainHeightAtPositionFromHeightMap`
  (params=2, returns Single); related `GetTerrainHeightAtPosition`,
  `SetupTerrain`. (Old logs show the pre-rename spelling "…HightAt…".)
- **Risk:** LOW-MEDIUM — value-returning getter; a safe-default guard is clean.
- **Approach:** if the heightmap array is null or the computed indices are out of
  range, clamp the indices (or return 0f) instead of indexing. This is one of the
  better first candidates because the safe default is obvious. May be related to
  the DATA terrain-load failures (#178/#181/#85).

### #80 — MOSS Mk 70 goes sea-skimming under time compression
- **Where:** decoy/weapon movement integration under high `Time.timeScale`.
- **Risk:** MEDIUM — physics correctness, not a crash.
- **Needs:** repro at specific time-compression levels.
- **Approach:** likely a depth-keeping step that doesn't scale with `deltaTime`;
  fix the integration rather than guard.

### #65 — Crew won't dive the sub after Emergency Blow
- **Where:** depth-order state machine — `PlayerFunctions::PlayerDiveSubmarine`,
  `BlowBallast`, `VesselMovement::SubDivePlanes`.
- **Risk:** MEDIUM — state-machine logic, not a crash.
- **Needs:** repro steps. Suspect a "blowing ballast" flag not cleared, blocking
  subsequent dive orders.

### #86 — VoiceManager array index out of range ("LostSonarContact")
- **Where:** `VoiceManager::PlayMessageLogVoice(string)`.
- **Status:** LOW priority — the method is **already wrapped in try/catch**, so
  this is a caught, logged error (a missing voiceline), not a crash. The current
  data row + `.wav` are valid, so any remaining trigger is a DLL-side index
  selection edge case. Fix only if it recurs with a fresh log.

### Generic / unlocalized
- **#136 / #114 / #113 / #111 / #127 / #183** — assorted NRE / IndexOutOfRange /
  "Unity Engine" crashes reported without stack traces. **Cannot localize**
  without `output_log.txt`. Triage each as logs arrive.

## Not worth IL-patching
- **#190** — done (this branch).
- **First-run validation OOM (#31)** — the *correct* DLL fix is to make
  `TextParser.ValidateFiles` stream the hash (`HashAlgorithm.ComputeHash(Stream)`)
  instead of `File.ReadAllBytes`. Deferred because it lives in a compiler-
  generated coroutine state machine (`<ValidateFiles>d__N.MoveNext`), which is
  fiddly and risky to rewrite in raw IL. The data-branch manifest trim already
  resolves the user-facing hang, so this is low priority.
