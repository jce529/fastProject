# Deferred Items — Phase 999.3 (player-portal-effect-rework)

Out-of-scope discoveries found during plan execution. Not fixed automatically per SCOPE BOUNDARY (only issues directly caused by the current task's changes are auto-fixed).

## 999.3-03 Task 2 checkpoint (round 2 tuning pass)

**File:** `Assets/Prefabs/World/ExitPortal/ExitPortal.prefab`
**Found during:** Working directory check while committing the `_vortexWorldRadius` tuning fix (Task 2 checkpoint, this plan's changes never touch this file).
**Observed diff:** Collider component changed from `CircleCollider2D` (m_Radius: 0.25) to `CapsuleCollider2D` (m_Size: {0.4059583, 0.4080408}), plus `m_LocalScale` changed from `{1, 1, 1}` to `{1, 2, 1}`.
**Status:** Left untouched — unrelated to this plan's scope (portal vortex material/effect tuning). Likely a side effect of Unity Editor session activity (Play mode / Inspector interaction) rather than an intentional plan change.
**Action needed:** If this was an accidental Editor edit, revert it manually or via `git checkout -- Assets/Prefabs/World/ExitPortal/ExitPortal.prefab`. If it was intentional (e.g., collider shape fix), it should go through its own GSD task/plan.

**Resolution:** Confirmed with user (2026-07-15) — intentional collider/scale adjustment. Committed as-is.
