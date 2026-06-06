# DLL Patcher

Targeted IL patches for `ColdWaters_Data/Managed/Assembly-CSharp.dll`.

Cold Waters' game logic ships only as a compiled assembly — there is **no C#
source** in this repo. Some bugs can therefore only be fixed by patching the
assembly's IL. We use [Mono.Cecil](https://github.com/jbevain/cecil) for this.

`UnityEngine.dll` / `UnityEngine.UI.dll` / `Assembly-CSharp-firstpass.dll` are
base-game files that are not committed here. Cecil needs to resolve them to
re-emit metadata, so `Patch.cs` builds throwaway **stub assemblies** (enum-shaped
type definitions for every referenced type) purely to satisfy write-time
resolution. The stubs are never written into the output assembly.

## Applied patches

| Issue | Method | Change |
|-------|--------|--------|
| #190 — game pauses/minimizes when unfocused | `LevelLoadManager::Awake` | Inject `UnityEngine.Application.runInBackground = true` at method entry. |

The change adds exactly two IL instructions (`ldc.i4.1; call set_runInBackground`)
and leaves every other method byte-identical (verified: type/method/body counts
unchanged, instruction count +2, all bodies re-parse).

## Deferred fixes

The remaining DLL-bound crashes (torpedo/missile/save-load NREs, etc.) are
documented in [`DEFERRED.md`](DEFERRED.md) with resolved method names, risk
level, what's needed to fix each (usually a stack trace), and a candidate
approach. Read it before picking up the next DLL fix.

## Reproduce

See the header of `run.sh` for prerequisites, then:

```bash
cd tools/dll-patcher
./run.sh                       # patches the in-repo DLL (idempotent)
mono Inspect.exe <dll> "Type::Method"   # dump a method's IL
```

> Note: patches here are verified structurally (Cecil round-trip + instruction
> diff) but **not** play-tested — Cold Waters can't run in CI. Validate under
> Proton before release. The original DLL is recoverable via git.
