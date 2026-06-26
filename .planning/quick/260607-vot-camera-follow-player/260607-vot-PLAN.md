---
quick_id: 260607-vot
slug: camera-follow-player
description: 카메라가 플레이어를 따라가도록 구현
date: 2026-06-07
status: complete
---

# Quick Task 260607-vot: 카메라가 플레이어를 따라가도록 구현

## Task

`CameraFollow.cs` 스크립트가 이미 존재하지만 씬의 Main Camera에 컴포넌트로 부착되지 않은 상태. 씬 파일을 직접 수정해 컴포넌트를 부착하고 Player Transform을 target으로 연결한다.

## Tasks

### Task 1: SampleScene.unity에 CameraFollow 컴포넌트 부착

- **files**: `Assets/Scenes/SampleScene.unity`
- **action**: Main Camera GameObject(`&519420028`)의 m_Component 목록에 `fileID: 519420033` 추가. 해당 fileID로 `MonoBehaviour` 항목 생성 — script GUID `a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6`, target = Player Transform(`fileID: 1394403462`), offset `{x:0, y:1, z:-10}`
- **verify**: Unity 에디터에서 Main Camera 선택 시 CameraFollow 컴포넌트가 표시되고 Target 필드에 Player가 연결됨
- **done**: ✅
