# Deferred Items — Phase 13

Logged during 13-04 execution (final plan of phase). These are pre-existing, out-of-scope items discovered in the working tree that are NOT caused by 13-04's task changes. Not fixed per scope-boundary rule (surgical changes — only touch what the current task changes).

## Untracked files (pre-existing before 13-04 execution started)

- `Assets/Audio.meta` — missing meta for Audio folder, likely dropped during 13-02's CC0 pack import. Should be committed in a follow-up housekeeping pass.
- `Assets/Editor/AudioManagerPrefabBuilder.cs.meta` — missing meta for the editor script committed in 13-02 (commit `0738008`). Should be committed alongside the above.
- `.planning/quick/260706-oxp-worldgenerator-camerafollow-bounds/` — unrelated quick-task directory, not part of Phase 13.

## Modified files (uncommitted, pre-existing before 13-04 execution started)

- `.vscode/settings.json`
- `Assets/Prefabs/Enemies/MeleeEnemy.prefab`
- `Assets/Prefabs/Rooms/Complex_Room/Room_AllInOne/Room_AllInOne.prefab`
- `Assets/Prefabs/Rooms/Complex_Room/Room_EdgeRun/Room_EdgeRun.prefab`
- `Assets/Prefabs/Rooms/Complex_Room/Room_Vertical_Gauntlet/Room_Vertical_Gauntlet.prefab`
- `Assets/Prefabs/World/PortalEffect/PortalEffect.prefab`
- `Assets/Scenes/SampleScene.unity`

These diffs predate this session (present in git status at conversation start) and are unrelated to Phase 13 audio work. Not reviewed or committed as part of 13-04 — flagging for a separate housekeeping/cleanup pass or the next plan/phase owner to investigate.
