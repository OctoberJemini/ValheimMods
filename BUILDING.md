# Building

The six projects with source folders here use the parameterized root `build.ps1` script. The three linked projects have their own build instructions in their existing repositories.

Requirements: Windows PowerShell, a C# compiler (Roslyn recommended), an installed copy of Valheim, and the mod's BepInEx dependencies. Point the script at your own game and profile paths; no game or dependency DLLs are included in this repository.

```powershell
.\build.ps1 -Mod Manual_Tar_Pickup -ManagedPath 'D:\Games\Valheim\valheim_Data\Managed' -BepInExPath 'D:\ModProfile\BepInEx' -CompilerPath 'D:\Tools\Roslyn\csc.exe'
```

`-Mod Building_Pieces` builds all three plugins in that package. Build output goes under `bin/<mod>/`. Auto Mapping embeds its landmark catalog; Building Pieces embeds its menu artwork. Build output is not automatically installed or published. A successful compilation does not substitute for in-game verification.

The version in each mod's `mod.json` records the source import snapshot; the manifest source URL is prepared for future Thunderstore releases without implying a new binary version.

Fish Attractor & Traps has its own parameterized build script in `mods/Fish_Attractor_And_Traps/build.ps1`, using the same `ManagedPath`, `BepInExPath` and `CompilerPath` arguments. It requires Jotunn, produces `FishAttractor.dll` beside the script, and does not install or publish automatically. Compile `TrapChecks.cs` with `src/TrapRules.cs` and `FishAttractorChecks.cs` with `src/BaitPolicy.cs` as console executables to run the 51 policy checks.

Wisplight Control uses `mods/Wisplight_Control/build.ps1` with `ManagedPath`, `BepInExPath` and `CompilerPath` arguments. Its standalone script produces `WisplightControl.dll` beside the source folder. `BehaviorChecks.cs` compiles with `src/WispRules.cs` as a console executable. `NativeCostChecks.cs` references game assemblies and Harmony; set `WISP_GAME_MANAGED` to the game Managed folder and `WISP_BEP_CORE` to the BepInEx core folder before running it beside the built mod DLL. It verifies the scoped Harmony cost patch through native Requirement.GetAmount without starting the game.
