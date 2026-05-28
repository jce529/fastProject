# Technology Stack

**Analysis Date:** 2026-05-27

## Languages

**Primary:**
- C# 9.0 - All game logic and Unity scripting (LangVersion 9.0 in `Assembly-CSharp.csproj`)

**Secondary:**
- HLSL/ShaderLab - GPU shaders (via Universal Render Pipeline shader system)
- JSON - Input action asset definitions (`Assets/InputSystem_Actions.inputactions`)
- YAML - Unity scene and asset serialization (`Assets/Scenes/SampleScene.unity`, `ProjectSettings/`)

## Runtime

**Environment:**
- Unity 6000.3.11f1 (Unity 6 LTS) - Game engine runtime
- .NET Standard 2.1 (netstandard2.1) - C# compilation target
- Mono scripting backend (ENABLE_MONO define confirmed in `Assembly-CSharp.csproj`)

**Package Manager:**
- Unity Package Manager (UPM) - Defined in `Packages/manifest.json`
- Lockfile: Present (`Packages/packages-lock.json`)

## Frameworks

**Core:**
- UnityEngine 6000.3.11f1 - Core game framework; all MonoBehaviour scripts reference this
- Universal Render Pipeline (URP) 17.3.0 - Rendering pipeline (`Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset`)

**Input:**
- Unity Input System 1.19.0 - New input system (`Assets/InputSystem_Actions.inputactions`); defines Player action map with Move (Vector2), Jump (Button), Attack (Button), Look (Vector2), Interact (Hold), Crouch

**2D:**
- com.unity.2d.animation 13.0.4 - 2D skeletal animation
- com.unity.2d.aseprite 3.0.1 - Aseprite sprite import support
- com.unity.2d.psdimporter 12.0.1 - PSD file import
- com.unity.2d.sprite 1.0.0 - Core 2D sprite tooling
- com.unity.2d.spriteshape 13.0.0 - Freeform 2D sprite shape rendering
- com.unity.2d.tilemap 1.0.0 - Tilemap system
- com.unity.2d.tilemap.extras 6.0.1 - Additional tilemap brushes and rules

**UI:**
- com.unity.ugui 2.0.0 - Unity UI (uGUI) - Canvas-based UI system for in-game HUD

**Testing:**
- com.unity.test-framework 1.6.0 - Unity Test Framework (NUnit-based); config referenced in `Library/PackageCache/com.unity.test-framework@0b7a23ab2e1d/`
- nunit.framework - Assertion library bundled with Unity Test Framework

**Authoring/Build:**
- com.unity.timeline 1.8.11 - Timeline sequencing (available; used for cutscenes/transitions)
- com.unity.visualscripting 1.9.10 - Visual scripting (Bolt); available but not required for gameplay code
- com.unity.burst 1.8+ (transitive) - Burst compiler for performance-critical C# jobs
- com.unity.collections 2.4.3 (transitive) - Native collections for Burst/Jobs
- com.unity.mathematics 1.2+ (transitive) - SIMD-friendly math library

**IDE Integration:**
- com.unity.ide.rider 3.0.39 - JetBrains Rider support
- com.unity.ide.visualstudio 2.0.26 - Visual Studio support
- com.unity.collab-proxy 2.11.4 - Unity Version Control (PlasticSCM) integration

**Multiplayer (Available, Unused):**
- com.unity.multiplayer.center 1.0.1 - Multiplayer onboarding hub (installed but game is single-player)

## Key Dependencies

**Critical:**
- `com.unity.render-pipelines.universal` 17.3.0 - Required for all rendering; configured via `Assets/Settings/UniversalRP.asset`; uses 2D Renderer (`Assets/Settings/Renderer2D.asset`)
- `com.unity.inputsystem` 1.19.0 - Sole input handling layer; action map at `Assets/InputSystem_Actions.inputactions`

**Infrastructure:**
- `com.unity.modules.physics2d` 1.0.0 - 2D physics (Rigidbody2D, Collider2D) - required for platformer movement and collision
- `com.unity.modules.audio` 1.0.0 - Audio playback
- `com.unity.modules.unitywebrequest` 1.0.0 - HTTP client (available; no network calls planned for prototype)
- `com.unity.modules.particlesystem` 1.0.0 - VFX particles (for attack effects)
- `com.unity.modules.animation` 1.0.0 - Animator/Animation components
- `com.unity.modules.tilemap` 1.0.0 - Tilemap rendering (for level floor/platform layouts)

## Configuration

**Environment:**
- No `.env` files; all configuration is through Unity's ProjectSettings YAML files
- Product name: "Fast" (`ProjectSettings/ProjectSettings.asset`, `productName`)
- Company: "DefaultCompany" (prototype stage)
- Bundle ID (Standalone): `com.DefaultCompany.2D-URP`

**Build:**
- `ProjectSettings/ProjectSettings.asset` - Core build/player settings
- `Packages/manifest.json` - Package dependency declarations
- `Packages/packages-lock.json` - Locked package versions
- `Assembly-CSharp.csproj` - Generated C# project file (do not edit manually)
- `Fast.slnx` - Visual Studio solution file

**Default Scene:**
- `Assets/Scenes/SampleScene.unity` - Configured as template default scene

## Platform Requirements

**Development:**
- Unity Hub with Unity 6000.3.11f1 installed
- Windows 10/11 (primary dev machine: Windows 11, confirmed by environment)
- JetBrains Rider or Visual Studio (IDE integration packages present)
- Target build architecture: StandaloneWindows64 (editor default)

**Production:**
- Primary target: **Android** (AndroidMinSdkVersion: 25 / Android 7.1+)
- Android target architecture: ARM64 (AndroidTargetArchitectures: 2)
- Secondary target: iOS (future; iOSTargetOSVersionString: 15.0 configured)
- Screen orientation: Landscape (defaultScreenOrientation: 4 = Landscape)
- Default resolution: 1920x1080
- Game category: `androidAppCategory: 3` (Game)
- URP 2D Renderer configured for mobile-appropriate rendering

---

*Stack analysis: 2026-05-27*
