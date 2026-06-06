#!/usr/bin/env bash
# Reproducible IL patcher for ColdWaters_Data/Managed/Assembly-CSharp.dll
#
# Cold Waters ships its game logic only as a compiled assembly (no source in
# this repo). We patch targeted, provably-safe fixes directly into the IL with
# Mono.Cecil. UnityEngine.dll is a base-game file and is NOT in the repo, so the
# patcher synthesizes stub assemblies to satisfy Cecil's write-time metadata
# resolution (the stubs are transient and never emitted into the output).
#
# Requirements (Linux): mono-devel (mcs, mono) + Mono.Cecil (from NuGet).
#   sudo apt-get install -y mono-devel
#   curl -sSL -o cecil.nupkg \
#     https://api.nuget.org/v3-flatcontainer/mono.cecil/0.11.5/mono.cecil.0.11.5.nupkg
#   unzip -o cecil.nupkg -d cecil && cp cecil/lib/net40/Mono.Cecil*.dll .
#
# Usage:  ./run.sh [path-to-Assembly-CSharp.dll]
set -euo pipefail
cd "$(dirname "$0")"
DLL="${1:-../../ColdWaters_Data/Managed/Assembly-CSharp.dll}"
[ -f Mono.Cecil.dll ] || { echo "Mono.Cecil.dll missing — see header for fetch steps"; exit 1; }
mcs -r:Mono.Cecil.dll Inspect.cs -out:Inspect.exe
mcs -r:Mono.Cecil.dll Patch.cs   -out:Patch.exe
echo "Patching $DLL ..."
mono Patch.exe "$DLL"
echo "Verifying injection:"
mono Inspect.exe "$DLL" "LevelLoadManager::Awake" | grep -m2 "runInBackground" || true
