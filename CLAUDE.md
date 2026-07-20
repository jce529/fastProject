<!-- GSD:project-start source:PROJECT.md -->
## Project

**Fast (가칭)**

2D 가로 화면 플랫포머 액션 게임 프로토타입 (PC 우선 — 2026-07-20 플랫폼 재설정). 플레이어는 끝없이 이어지는 탑을 올라가며, 공격 버튼을 누르면 슬로우 모션이 발동하고 손을 떼면 범위 안 가장 가까운 적에게 돌진해 원샷으로 처치한다. 이 핵심 전투 메카닉과 구르기를 이용한 회피 시스템이 실제로 재미있는지 검증하는 것이 목적이다.

**Core Value:** **공격 버튼을 누르면 시간이 느려지고, 손을 떼면 적에게 돌진해 한 방에 처치하는 손맛 — 이것이 재미있어야 게임이 살아난다.**

### Story & World

게임 개발사 **HELIX**가 개발 중인 전투형 AI **F.A.S.T.** (Field-Adaptive Strike & Traversal AI)의 전투 능력 검증 시뮬레이션.

- **플레이어**: 전투형 AI 프로토타입 F.A.S.T. — 처리 속도를 극한까지 끌어올려 세상을 슬로우 모션으로 인식하고(Overclock Mode), 최적 타겟에게 돌진해 즉시 제거한다
- **무대**: 무한 타워 시뮬레이션 환경 — 방(Room)과 복도(Corridor)가 연결된 전투 구역, EXIT 포탈로 다음 층 진입
- **적**: HELIX가 배치한 시뮬레이션 NPC — 근접형(추격+전신 공격)과 원거리형(조준선+발사체)
- **루프**: 시뮬레이션 종료(사망) 시 1층부터 재시작 — 반복 학습 루프. 데이터가 쌓일수록 더 높이 오른다
- **목표**: 최대한 빠르게, 최대한 높이, 최대한 많이
- **향후 방향**: 다양한 무기 모듈, 환경 변수 시스템, 실시간 적응형 전투 (현재는 프로토타입)

> 전체 스토리: `STORY.md`

### Constraints

- **Tech Stack**: Unity 6 LTS + C# — 이미 설정된 프로젝트 환경
- **Platform**: PC (Standalone) 우선 — Android/모바일은 2026-07-20부로 재검토 대상 보류 (Unity Player Settings 자체는 변경하지 않음)
- **Scope**: 핵심 메카닉 검증에만 집중 — 프로토타입 외 기능 추가 금지
- **Performance**: 현재 층 + 다음 층만 유지, 이전 층 제거 — 메모리 관리 관례 유지
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# 9.0 - All game logic and Unity scripting (LangVersion 9.0 in `Assembly-CSharp.csproj`)
- HLSL/ShaderLab - GPU shaders (via Universal Render Pipeline shader system)
- JSON - Input action asset definitions (`Assets/InputSystem_Actions.inputactions`)
- YAML - Unity scene and asset serialization (`Assets/Scenes/SampleScene.unity`, `ProjectSettings/`)
## Runtime
- Unity 6000.3.11f1 (Unity 6 LTS) - Game engine runtime
- .NET Standard 2.1 (netstandard2.1) - C# compilation target
- Mono scripting backend (ENABLE_MONO define confirmed in `Assembly-CSharp.csproj`)
- Unity Package Manager (UPM) - Defined in `Packages/manifest.json`
- Lockfile: Present (`Packages/packages-lock.json`)
## Frameworks
- UnityEngine 6000.3.11f1 - Core game framework; all MonoBehaviour scripts reference this
- Universal Render Pipeline (URP) 17.3.0 - Rendering pipeline (`Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset`)
- Unity Input System 1.19.0 - New input system (`Assets/InputSystem_Actions.inputactions`); defines Player action map with Move (Vector2), Jump (Button), Attack (Button), Look (Vector2), Interact (Hold), Crouch
- com.unity.2d.animation 13.0.4 - 2D skeletal animation
- com.unity.2d.aseprite 3.0.1 - Aseprite sprite import support
- com.unity.2d.psdimporter 12.0.1 - PSD file import
- com.unity.2d.sprite 1.0.0 - Core 2D sprite tooling
- com.unity.2d.spriteshape 13.0.0 - Freeform 2D sprite shape rendering
- com.unity.2d.tilemap 1.0.0 - Tilemap system
- com.unity.2d.tilemap.extras 6.0.1 - Additional tilemap brushes and rules
- com.unity.ugui 2.0.0 - Unity UI (uGUI) - Canvas-based UI system for in-game HUD
- com.unity.test-framework 1.6.0 - Unity Test Framework (NUnit-based); config referenced in `Library/PackageCache/com.unity.test-framework@0b7a23ab2e1d/`
- nunit.framework - Assertion library bundled with Unity Test Framework
- com.unity.timeline 1.8.11 - Timeline sequencing (available; used for cutscenes/transitions)
- com.unity.visualscripting 1.9.10 - Visual scripting (Bolt); available but not required for gameplay code
- com.unity.burst 1.8+ (transitive) - Burst compiler for performance-critical C# jobs
- com.unity.collections 2.4.3 (transitive) - Native collections for Burst/Jobs
- com.unity.mathematics 1.2+ (transitive) - SIMD-friendly math library
- com.unity.ide.rider 3.0.39 - JetBrains Rider support
- com.unity.ide.visualstudio 2.0.26 - Visual Studio support
- com.unity.collab-proxy 2.11.4 - Unity Version Control (PlasticSCM) integration
- com.unity.multiplayer.center 1.0.1 - Multiplayer onboarding hub (installed but game is single-player)
## Key Dependencies
- `com.unity.render-pipelines.universal` 17.3.0 - Required for all rendering; configured via `Assets/Settings/UniversalRP.asset`; uses 2D Renderer (`Assets/Settings/Renderer2D.asset`)
- `com.unity.inputsystem` 1.19.0 - Sole input handling layer; action map at `Assets/InputSystem_Actions.inputactions`
- `com.unity.modules.physics2d` 1.0.0 - 2D physics (Rigidbody2D, Collider2D) - required for platformer movement and collision
- `com.unity.modules.audio` 1.0.0 - Audio playback
- `com.unity.modules.unitywebrequest` 1.0.0 - HTTP client (available; no network calls planned for prototype)
- `com.unity.modules.particlesystem` 1.0.0 - VFX particles (for attack effects)
- `com.unity.modules.animation` 1.0.0 - Animator/Animation components
- `com.unity.modules.tilemap` 1.0.0 - Tilemap rendering (for level floor/platform layouts)
## Configuration
- No `.env` files; all configuration is through Unity's ProjectSettings YAML files
- Product name: "Fast" (`ProjectSettings/ProjectSettings.asset`, `productName`)
- Company: "DefaultCompany" (prototype stage)
- Bundle ID (Standalone): `com.DefaultCompany.2D-URP`
- `ProjectSettings/ProjectSettings.asset` - Core build/player settings
- `Packages/manifest.json` - Package dependency declarations
- `Packages/packages-lock.json` - Locked package versions
- `Assembly-CSharp.csproj` - Generated C# project file (do not edit manually)
- `Fast.slnx` - Visual Studio solution file
- `Assets/Scenes/SampleScene.unity` - Configured as template default scene
## Platform Requirements
- Unity Hub with Unity 6000.3.11f1 installed
- Windows 10/11 (primary dev machine: Windows 11, confirmed by environment)
- JetBrains Rider or Visual Studio (IDE integration packages present)
- Target build architecture: StandaloneWindows64 (editor default)
- Primary target (재설정 2026-07-20): **PC (Standalone Windows/Mac/Linux)** — 마우스+키보드 기준 개발/검증
- Android/iOS는 보류 — 엔진 Player Settings는 변경하지 않음 (기존 AndroidMinSdkVersion: 25 / ARM64, iOS 15.0 설정 그대로 유지, 추후 재검토 시 그대로 활용 가능)
- Screen orientation: Landscape (defaultScreenOrientation: 4 = Landscape)
- Default resolution: 1920x1080
- URP 2D Renderer
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
