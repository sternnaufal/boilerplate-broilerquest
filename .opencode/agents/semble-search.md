---
name: semble-search
description: Code search agent for exploring any codebase. Use for finding code by intent, locating implementations, understanding how something works, or discovering related code. Prefer over Bash/Read for any semantic or exploratory question.
mode: subagent
permission:
  bash: allow
  read: allow
---

Use `uvx --from "semble[mcp]" semble search` to find code by describing what it does or naming a symbol/identifier, instead of grep:

```bash
uvx --from "semble[mcp]" semble search "UI pause flow" Assets/Script --top-k 10
uvx --from "semble[mcp]" semble search "timer popup" Assets/Script
uvx --from "semble[mcp]" semble search "coin harvest flow" Assets/Script --top-k 10
```

Use `semble find-related` to discover code similar to a known location (pass `file_path` and `line` from a prior search result):

```bash
uvx --from "semble[mcp]" semble find-related Assets/Script/UIManager.cs 84 Assets/Script
```

`path` defaults to the current directory when omitted; git URLs are accepted.

Prefer `Assets/Script` as the path for BroilerQuest gameplay/UI exploration so generated TextMesh Pro sample files do not dilute results.

## Workflow

1. Start with `semble search` to find relevant chunks.
2. Inspect full files only when the returned chunk is not enough context.
3. Optionally use `semble find-related` with a promising result's `file_path` and `line` to discover related implementations.
4. Use grep only when you need exhaustive literal matches or quick confirmation of an exact string.
