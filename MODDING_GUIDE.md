# Cold Waters Modding Guide (preserved reference)

A stable pointer to the **official Cold Waters modding guide by Killerfish
Games** (the base game's developer). Kept here so the reference survives — the
upstream guide/manual links have a history of breaking (cf. issues
[#174](https://github.com/DotModGroup/ColdWaters-DotMod/issues/174) and
[#187](https://github.com/DotModGroup/ColdWaters-DotMod/issues/187)).

> **Note:** the full verbatim text is intentionally **not** mirrored here. The
> guide is Killerfish Games' copyrighted content, and the host sites block
> automated retrieval, so this file preserves the authoritative links plus the
> key points rather than a copy. Use the links below for the complete guide.

## Authoritative sources

- **Steam Community guide:** https://steamcommunity.com/sharedfiles/filedetails/?id=938593459
- **CombatACE (Killerfish forum):** https://combatace.com/forums/topic/91455-modding-guide-creating-custom-content-for-cold-waters-by-killerfish-games-cold-waters/
- Related: [Cold Waters FAQ (Killerfish)](https://combatace.com/forums/topic/91449-frequently-asked-questions-cold-waters/)

## Key points from the official guide

- All moddable content lives under `ColdWaters_Data/StreamingAssets/`. The base
  game content is in the `default` folder.
- At load time the game checks **three** locations, in order of precedence:
  1. the external `override` folder,
  2. the external `default` folder,
  3. the internal (packed) game files.
  A file found earlier in this list overrides the same file found later.
- When referencing **images** from the `default` or `override` folders, the
  file **extension must be included** in the reference.
- **Back up** any files before editing them.
- Recommended approach for newcomers: start with a simpler topic (campaigns are
  **not** recommended as a first project), read up on it, change **one parameter
  at a time** and observe its in-game effect, then build from there. The guide
  is described by its authors as a work in progress.

## How this relates to DotMod

DotMod ships its payload in `ColdWaters_Data/StreamingAssets/dotmod/` and extends
the base load order with two extra folders, giving:

```
default  >  dotmod  >  override  >  priority
```

For the DotMod-specific repository layout and conventions, see
[`CLAUDE.md`](CLAUDE.md).
