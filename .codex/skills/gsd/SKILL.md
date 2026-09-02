---
name: gsd
description: Run Get Shit Done (GSD) project planning, execution, verification, debugging, and progress workflows. Use when the user invokes $gsd or asks to operate the installed GSD framework.
---

# GSD

Use the installed Get Shit Done framework as the source of truth. This skill is a thin entrypoint; do not duplicate or improvise GSD procedures.

## Route the request

Interpret the first argument after `$gsd` as the workflow name and preserve the remaining arguments exactly.

Examples:

- `$gsd progress --forensic` routes to `progress` with `--forensic`.
- `$gsd plan-phase 10` routes to `plan-phase` with `10`.
- `$gsd execute-phase 10` routes to `execute-phase` with `10`.
- `$gsd help` routes to `help`.

If no workflow name is provided, route to `progress` when `.planning/` exists; otherwise route to `help`.

## Execute the workflow

1. Resolve the GSD home from the active Codex home. In the standard installation it is `C:/Users/MSI/.codex/get-shit-done`.
2. Before taking any task action, read `references/mandatory-initial-read.md` from the GSD home.
3. Resolve the requested workflow under `workflows/<workflow-name>.md`.
4. If that exact workflow does not exist, read `workflows/help.md` and use its documented canonical command or alias. Do not invent a command.
5. Read the selected workflow completely, including every file it places in a `<required_reading>` block, then follow it faithfully with the remaining arguments.
6. Treat GSD's registered `gsd-*` agents and `bin/gsd-tools.cjs` as internal implementation details. Spawn or invoke them only when the selected workflow requires it.

## Boundaries

- Preserve the user's authorization boundaries; selecting a GSD workflow does not authorize unrelated mutations or external actions.
- Preserve existing `.planning/` state and resume completed work according to the selected workflow.
- Do not replace a full GSD workflow with an abbreviated local interpretation.
- For a trivial request that the user explicitly wants handled outside GSD, do not force this skill.
