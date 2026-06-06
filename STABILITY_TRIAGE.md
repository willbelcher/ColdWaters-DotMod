# Stability Issue Triage

Triage of open stability/crash issues from the upstream tracker
(`DotModGroup/ColdWaters-DotMod`), classified by where the fix has to live.

**Why the split matters:** there is **no C# source** in this repo — game logic
ships only as the compiled `ColdWaters_Data/Managed/Assembly-CSharp.dll`.

- **DATA** = fixable by editing payload files in `StreamingAssets/dotmod/`.
- **DLL** = lives in compiled code; needs decompile/patch of `Assembly-CSharp.dll`.
- **NEEDS INFO** = needs the reporter's `output_log.txt` or in-game repro to
  pinpoint; a correct value can't be verified from the repo alone.

Runtime verification of any fix requires launching the game under Proton, which
isn't possible in this environment — DATA fixes here are evidence-based, not
play-tested.

## Fixed on this branch (DATA)

| # | Title | Fix |
|---|-------|-----|
| 126 | Zombie Type 42s | `uk_84_ddg_type42_pl_description.txt` (EN+RU) listed **Sheffield** and **Coventry** as playable in the 1984 campaign, though both were sunk in the 1982 Falklands War (the file's own History text says so). Removed both from `PlayerClassNames` and `PlayerClassHullNumbers`. |

## DATA-suspected, NOT changed (need a reference value or repro)

These look data-driven but a blind edit could regress a shipped, presumably
play-tested asset. Flagged for a maintainer who can verify in-game.

| # | Title | Observation / where to look |
|---|-------|------------------------------|
| 88 | Flying Barbel (sub floats above water) | `vessels/01_Submarine/01_USN/01_SSK/02_Barbel/usn_{68,84}_ss_barbel.txt` have `Waterline=-0.07`; peer subs use positive values (Skipjack/Permit `0.04`, Collins `0.035`). Anomalous, but correct value unverified — may also be a model/pivot (bundle) issue. |
| 181 | Trondheim land-attack: SSGN spawns deep inland | Spawn coordinate in the Trondheim USSR-campaign mission under `campaign/` / `missions/`. Needs correct coords (verify on the map). |
| 178 | 1984 Soviet port-strike: terrain/ships don't load, "Checkspawndepth could not find a valid position" | Looks like a campaign/terrain-reference problem (empty minimap, trees above sub). No `kmn`/`knm` prefix typo found in payload. Needs the specific mission + terrain asset check. |
| 85 | Spawned on very shallow water / stuck in mud | Mission spawn-depth data; reporter attached a save. Possibly the same family as #178/#181. |
| 169 | Ticonderoga towed decoy can't be repaired | `subsystems.txt` defines `Subsystem=TOWEDDECOY` with `RepairTime=0.01`. Repair *button* missing suggests a damage-control HUD mapping (`hud/damage_control/usn_*_cg_ticonderoga_pl*`) rather than the repair time — needs UI/asset confirmation. |
| 133 | Wrong colour / pink smoke on surface ships (macOS) | Pink = missing texture/shader/material binding, typically in a `.unity3d` bundle under `bundles/` or `ships/`. Asset-level, not plain-text. |
| 171 | Collins knuckles form at random locations | No anomaly in the Collins stat file; knuckle spawn transform is likely in the model bundle or DLL. |

## DLL-bound (need to patch `Assembly-CSharp.dll`)

Crashes / logic bugs in compiled code. Targets for the DLL branch.

| # | Title | Likely site |
|---|-------|-------------|
| 188 | NRE: Harpoon (Ticonderoga) vs KA-27 (Udaloy) | weapon/target resolution |
| 184 | NRE in `Torpedo.FixedUpdate()` | torpedo update loop |
| 175 | Crash when enemy sub shoots down player heli | helicopter death handling |
| 151 | Crash in `GetTorpedoID` on loading save | save deserialization |
| 132 | `ArgumentOutOfRange` landing helicopter | helicopter landing |
| 120 | Crash when changing ship course | surface-ship steering |
| 103 | Torpedoes lose lock at periscope depth | sensor/seeker logic |
| 98 | NRE on Trafalgar missile launch | missile launch |
| 94 | NRE `LevelLoadManager.GetTerrainHightAtPositionFromHeightMap()` | terrain sampling |
| 86 | `VoiceManager.PlayMessageLogVoice` array index out of range ("LostSonarContact") | voice index selection (data row & wav are valid → DLL-side) |
| 80 | MOSS Mk 70 sea-skimming under time compression | physics under time compression |
| 65 | Crew won't dive after Emergency Blow | depth-order state machine |
| 190 | Game pauses/minimizes when unfocused | Unity "Run In Background" (player setting, baked into engine config) |
| 136 / 114 / 113 / 111 / 127 / 183 | Generic NRE / IndexOutOfRange / "Unity Engine" crashes | need stack/log to localize |

## NEEDS INFO (no actionable detail yet)

`189` (screenshot only), `185` (stuck "Initialising" — could be the first-run
validation OOM already addressed for Linux, see README/#31), `179` (can't start
custom battle — screenshot only), `177`/`121`/`191`/`192` (UI/HUD layout, incl.
ultrawide), `159`/`156` (mission-editor/campaign load errors — need the log),
`176` (out-of-torpedoes RTB logic), `162` (save-load), `173` (FormatException —
need log to find the offending value), `139` (button tooltips — feature).

## Not bugs (feature requests)

`134`, `131`, `129`, `110`, `109` — new vessels/weapons/playable ships.
