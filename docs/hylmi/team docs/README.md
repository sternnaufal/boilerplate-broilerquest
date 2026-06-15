# Team Docs

This folder contains teammate-facing documentation for BroilerQuest work that should remain easy to read without the full AI chat context.

## Current Documents

- `bubble_expiry_per_level_2026-06-10.md`
  - Current level-aware bubble expiry timing rules (Starter = 0s/disabled, Beginner = 20s, Intermediate = 15s).
  - Config parameters inside `GameConstants.cs` and Unity inspector values per scene.

- `bugfix_refactor_log_2026-06-08.md`
  - Current restored bugfix and refactor log after pulling `gakusahnama`.
  - Use this first when checking which gameplay/save/UI fixes must be preserved.

- `bubble_expiry_update_2026-06-08.md`
  - Legacy/older behavior and QA notes for the Starter care bubble expiry timer (uniform 10s rule).
  - Retained for historical reference of puzzle/pause integration.

- `update_2026-06-05_codex_commandcode_handover.md`
  - Earlier Codex + Command Code handover document.
  - Kept as historical context for the AI-assisted workflow.

## Folder Split

- `docs/hylmi/team docs/`
  - Human-readable docs for teammates.
  - Bugfix logs, QA checklist, workflow notes, balancing notes, and handover summaries.

- `docs/hylmi/ai docs/`
  - Older AI-generated planning, patch, handover, and verification archives.
  - Useful for history, but not the first place teammates should read.

## Rules for New Team Docs

- Keep the wording practical and specific.
- Include affected files and validation status.
- Separate confirmed bugs from future recommendations.
- Mention Unity asset edits clearly when a prefab or scene was touched.
- Do not hide editor noise. Call it out as noise so it does not accidentally get committed.
