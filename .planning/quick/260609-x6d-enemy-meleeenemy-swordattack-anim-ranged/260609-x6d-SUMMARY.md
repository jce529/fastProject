---
quick_id: 260609-x6d
status: completed
commits:
  - 3b625ea
  - e86fa00
date: 2026-06-14
---

# Quick Task 260609-x6d: SUMMARY

두 Enemy(MeleeEnemy, RangedEnemy)의 Animator Controller에 `Attack` 스테이트와 `isAttacking` Trigger 파라미터를 추가하고, 공격 코드의 정확한 타이밍에 트리거를 발사하도록 연결했다.

## 완료된 작업

### T1: MeleeEnemyAnimator.controller 수정 (commit 3b625ea)

- `m_AnimatorParameters`에 `isAttacking` (Type 9 = Trigger) 추가
- `AnimatorStateMachine.m_ChildStates`에 Attack 스테이트 추가 (position: {x: 305, y: 195, z: 0})
- `m_AnyStateTransitions`에 AnyState→Attack 추가 (isDead 다음)
- 새 블록 3개 추가:
  - `&4500000000000000010` — Attack AnimatorState (Motion: SwordAttack.anim, GUID `12482399ed2e0d34188669fc946e0f8c`)
  - `&4500000000000000012` — Attack→Idle Transition (hasExitTime=true, exitTime=0.9, duration=0, target: Idle `2042404990840053610`)
  - `&4500000000000000011` — AnyState→Attack Transition (condition: isAttacking trigger, duration=0)
- Attack 스테이트의 `m_Transitions`에 Attack→Idle (`4500000000000000012`) 연결

### T2: RangedEnemyAnimator.controller 수정 (commit 3b625ea)

- `m_AnimatorParameters`에 `isAttacking` (Type 9 = Trigger) 추가
- `AnimatorStateMachine.m_ChildStates`에 Attack 스테이트 추가 (position: {x: 235, y: 130, z: 0})
- `m_AnyStateTransitions`에 AnyState→Attack 추가
- 새 블록 3개 추가:
  - `&4500000000000000020` — Attack AnimatorState (Motion: GunFire.anim, GUID `725f2bcdba0d61b4ab2b87af4fe7666d`)
  - `&4500000000000000022` — Attack→Idle Transition (hasExitTime=true, exitTime=0.9, duration=0, target: Idle `-5563441268072642978`)
  - `&4500000000000000021` — AnyState→Attack Transition (condition: isAttacking trigger, duration=0)
- Attack 스테이트의 `m_Transitions`에 Attack→Idle (`4500000000000000022`) 연결

### T3: MeleeEnemy.cs — isAttacking 트리거 추가 (commit e86fa00)

`TelegraphAndAttack` 코루틴에서 `_state = EnemyState.Attack;` 바로 다음, 멜리 히트박스 활성화 블록 이전에 추가:

```csharp
_state = EnemyState.Attack;
GetComponent<Animator>()?.SetTrigger("isAttacking");

// Activate melee hitbox briefly (D-07, D-08)
```

### T4: RangedEnemy.cs — isAttacking 트리거 추가 (commit e86fa00)

`TelegraphAndFire` 코루틴에서 `FireProjectile(aimDir, origin);` 호출 직전에 추가:

```csharp
if (_aimLine != null) _aimLine.enabled = false;

GetComponent<Animator>()?.SetTrigger("isAttacking");
FireProjectile(aimDir, origin);
```

## 변경된 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Animations/Enemies/MeleeEnemyAnimator.controller` | isAttacking 파라미터, Attack 스테이트, 트랜지션 2개 추가 |
| `Assets/Animations/Enemies/RangedEnemyAnimator.controller` | isAttacking 파라미터, Attack 스테이트, 트랜지션 2개 추가 |
| `Assets/Scripts/Enemy/MeleeEnemy.cs` | TelegraphAndAttack에 SetTrigger("isAttacking") 1줄 추가 |
| `Assets/Scripts/Enemy/RangedEnemy.cs` | TelegraphAndFire에 SetTrigger("isAttacking") 1줄 추가 |

## Commits

- `3b625ea` — feat(animator): add Attack state + isAttacking trigger to MeleeEnemy and RangedEnemy animators
- `e86fa00` — feat(enemy): fire isAttacking trigger at attack moment in MeleeEnemy and RangedEnemy

## Deviations from Plan

None - plan executed exactly as written.

작은 메모: 새 YAML 블록 추가 시 기존 Walk 스테이트 끝부분(`m_Tag:`, `m_SpeedParameter:` 등)의 트레일링 공백이 의도치 않게 제거될 뻔했으나, diff 확인 후 원본 트레일링 공백을 그대로 복원하여 기존 코드에 대한 불필요한 변경(diff noise)을 방지했다 (Surgical Changes 원칙).

## Known Stubs

없음. 두 Animator 모두 Attack 스테이트, 트랜지션, 트리거 발사 코드가 모두 연결되어 있어 플레이 시 즉시 동작 확인 가능.

## Verification

- T1: `Assets/Animations/Enemies/MeleeEnemyAnimator.controller`에서 `isAttacking`, `Attack`, `4500000000000000010/11/12` 모두 확인됨
- T2: `Assets/Animations/Enemies/RangedEnemyAnimator.controller`에서 `isAttacking`, `Attack`, `GunFire` GUID(`725f2bcdba0d61b4ab2b87af4fe7666d`), `4500000000000000020/21/22` 모두 확인됨
- T3: `MeleeEnemy.cs` line 172 — `GetComponent<Animator>()?.SetTrigger("isAttacking");`이 `_state = EnemyState.Attack;` 바로 아래 위치
- T4: `RangedEnemy.cs` line 203 — `GetComponent<Animator>()?.SetTrigger("isAttacking");`이 `FireProjectile(aimDir, origin);` 바로 위 위치

## 다음 단계 (Unity Editor 필요)

Unity Editor에서 두 Animator를 열어 Attack 스테이트와 트랜지션이 그래프 상에 올바르게 표시되는지, SwordAttack.anim / GunFire.anim 모션이 정상 연결되었는지 시각적으로 확인 필요. 플레이 모드에서 MeleeEnemy/RangedEnemy 공격 시 Attack 애니메이션이 재생되는지 확인 필요.

## Self-Check: PASSED

All 4 modified files exist and both commits (3b625ea, e86fa00) found in git log.
