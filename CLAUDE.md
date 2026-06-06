# CLAUDE.md

Guidance for working in this repository.

## What this is

**DotMod** is a large overhaul mod for **Cold Waters**, a Cold-War submarine
combat game built on **Unity 5.4** (a Windows-only build). The repo is *not*
an application source tree — it is the mod's payload (game data + a patched
game assembly) plus the tooling to install it. There is **no C# source code**
in the repo; the game's logic ships only as a compiled DLL.

Cold Waters itself is Windows-only. On Linux/macOS it runs through **Proton/
Wine**, and so does the mod (the modded files execute inside the Wine prefix).
See `README.md` -> "Linux / Steam Deck (Proton)" for the install flow.

## Repository layout

| Path | What it is |
|------|------------|
| `ColdWaters_Data/StreamingAssets/dotmod/` | The mod payload — all the data/config the game reads at runtime. This is where ~99% of moddable content lives. |
| `ColdWaters_Data/Managed/Assembly-CSharp.dll` | The **modded** game assembly (compiled C#). `Assembly-CSharp.bak` is the smaller, unmodified vanilla copy. No source is provided. |
| `ColdWaters_Data/Managed/INIFileParser.dll` / `.xml` | Third-party INI parsing library used by the game. |
| `DotModInstaller.py` | Cross-platform Python installer. Auto-detects Steam (Windows/Linux/macOS, incl. Flatpak, Steam Deck, and secondary drives via `libraryfolders.vdf`), copies the payload into `<game>/MODS/<mod_name>/`, and installs JSGME. |
| `DotModInstaller.exe` | Nuitka-compiled build of the installer (build cmd is in the `.py` header). Runs under Wine on Linux. |
| `Installer.ini` | Selects what the installer builds (`mod_name = DotMod`). The same installer is reused for addons by shipping a different ini. |
| `JSGME/` | JoneSoft Generic Mod Enabler — a **Windows** `.exe` tool that toggles mods by overlaying files. Run via Proton/Wine on Linux. |
| `Addons/` | Optional add-on payloads (each has a `*_definition.json`). Layered *over* the main mod. |
| `Utilities/` | Helper scripts. `addons.json` lists downloadable addons; `weaponsscript_public.py` splits a monolithic `weapons.txt` into per-weapon files. |
| `.github/workflows/FileList.yml` | CI: on push, runs `buildWeaponDataTable.ps1` and commits a generated weapon table to the `DotModGroup.github.io` website repo. |
| `DotMod_Installer.pdf` | Manual-install instructions (link in README may be stale — issue #174). |

### Inside `StreamingAssets/dotmod/` (the payload)

Plain-text, key/value + `[Section]` config files (a Cold Waters convention),
plus binary Unity asset bundles. Approximate scale:

- `vessels/` (~716 files) — ship/sub stat files, organized by class folders
  (`01_Submarine`, `02_Surface`, `03_Auxiliary`, `97_Civilian`, `98_Biologic`,
  `99_Special`) plus `profile/`, `signature/`, `textures/`. Each vessel is a
  `.txt` with `[Movement]`, `[Acoustics & Sensors]`, weapon loadouts, etc.
- `weapons/` (~131), `sensors/` (~138), `aircraft/` (~25) — split into
  per-item files by nation (`01_USN`, `02_WP`, `04_UK`, ...). DotMod split the
  old monolithic `weapons.txt`/`sensors.txt`/`aircraft.txt` into trees.
- `ships/` (~247) — 3D/visual definitions for surface ships.
- `campaign/` (~2588) — campaign definitions, maps, events, RPG/prestige data.
- `missions/` (~43) — single missions / mission-editor content.
- `hud/` (~518) — HUD panel layouts (per-vessel-type variants).
- `language_en/` + `language_ru/` (~608 each) — localization dictionaries,
  help text, training, voicelines. **Keep these two in sync.**
- `audio/` (~603), `bundles/` (~100 `.unity3d`), `textures/`, `environment/`,
  `terrain/` — binary assets. `bundles/` holds custom 3rd-party models loaded
  from asset bundles.
- Top-level files: `config.txt` (global tunables + difficulty tables),
  `editor.txt` (campaign map references), `subsystems.txt`, `default_keys.txt`,
  `language_def.txt`, and `Cold Waters 1_15g.mf` (see below).

### `Cold Waters 1_15g.mf` — first-run integrity manifest

Tab-separated `\<file>\t<sha1>` lines listing **base-game** files. On first
launch the modded DLL (`TextParser.ValidateFiles`) hashes each listed file to
confirm the install is vanilla and the right version. The original DLL uses
`File.ReadAllBytes`, which loads each whole file into memory — reading the
multi-hundred-MB `resources.assets.resS` OOM-hangs under 32-bit Proton mono
(issue #31). This manifest is intentionally **trimmed to small core files only**
(`app.info`, `globalgamemanagers`, `level0`, `ScreenSelector.bmp`) so the check
still verifies the game version without the huge reads. Keep it minimal; do not
re-add large `.assets`/`.resS`/`.resource` entries.

## Conventions & gotchas

- **No build step for the mod itself.** Editing payload `.txt` files *is* the
  change; the game reads them at runtime. Only `DotModInstaller.py` ->
  `.exe` (Nuitka) and the weapon-table CI script are "compiled".
- **Line endings:** `.gitattributes` forces `*.txt` to CRLF. Non-`.txt` data
  files (e.g. the `.mf`) are LF — preserve existing endings when editing.
- **README.md uses non-breaking spaces (U+00A0)** in much of its body text;
  exact-string edits must account for this (anchor on ASCII runs).
- **The DLL is the real "engine."** Most crash/stability bugs live in
  `Assembly-CSharp.dll`, for which there is **no source here**. They cannot be
  fixed by editing data files; they require decompiling/patching the assembly
  (no .NET toolchain is installed in this environment by default).
- **Path conventions:** config files use Windows `\` separators internally
  (e.g. `bundles\fonts\custom`); that's fine because the game runs under
  Windows/Proton. The Python installer uses `/` (works on all platforms).
- **Upstream vs. fork:** issues live on `DotModGroup/ColdWaters-DotMod`
  (upstream); development here happens on `willbelcher/ColdWaters-DotMod`.

## Where to make common changes

- Tune a vessel/weapon/sensor -> the matching `.txt` under
  `vessels/`, `weapons/`, `sensors/`.
- Global gameplay tunables / difficulty -> `dotmod/config.txt`.
- Text / translations -> `language_en/` **and** `language_ru/`.
- Campaign / map / mission content -> `campaign/`, `editor.txt`, `missions/`.
- Installer / platform support -> `DotModInstaller.py`.
- Install instructions -> `README.md`.
