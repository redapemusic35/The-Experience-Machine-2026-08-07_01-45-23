# Run ledger
| date | task | result | files | summary |
|---|---|---|---|---|
| 2026-10-19 | setup | done | CLAUDE_BACKLOG.md, CLAUDE_RUNBOOK.md | Backlog, runbook and ledger added; scheduled runs start next |
| 2026-10-20 | W1 | review | Docs/walk-system-audit.md, CLAUDE_BACKLOG.md | Audit done: Chalmers runs the same DialogueTrigger as Socrates (no separate script); 15 hard-coded refs listed |
| 2026-10-21 | D1 | review | .github/workflows/deploy-webgl.yml, Assets/Editor/WebGLBuild.cs, Docs/deploy-webgl.md, CLAUDE_BACKLOG.md | Manual-trigger GameCI WebGL build → Pages at /play with Gzip + decompression fallback; Jim needs Unity secrets + Pages setup (D2) |
| 2026-10-21 | X6, X7, D1 | done | Assets/New Terrain.asset (+2 terrain assets), .gitattributes, Assets/Editor/WebGLBuild.cs, .github/workflows/deploy-webgl.yml | Interactive: restored CR-damaged terrain data (player fell through ground); raised WebGL max memory to 2048 MB (OOM); site push rebases and retries |
| 2026-10-22 | X8 | review | Assets/Editor/WebGLBuild.cs, Tools/webgl_add_phone_build.py, .github/workflows/deploy-webgl.yml, Docs/deploy-webgl.md, CLAUDE_BACKLOG.md | Interactive: dual DXT/ASTC build; first run's phone file was still DXT (subtarget not passed); fixed and added an ASTC check that blocks publishing |
| 2026-10-22 | X4b, X8 | done | CLAUDE_BACKLOG.md | X4b done by Jim (dialogue on F, live); X8 verified on the live build (ASTC in the phone data file); D7 unblocked |
| 2026-10-22 | X2, X4c | review | Assets/Dialogue/DialogueTrigger.cs, Assets/StarterAssets/InputSystem/StarterAssets.inputactions, StarterAssetsInputs.cs, FirstPersonController.cs | Interactive: overlay hidden outside debug builds; arrow keys turn (new Turn action), A/D strafe; compile-checked in Jim's editor, 0 errors |
| 2026-10-22 | Y1 | review | Tools/validate_yarn.py, CLAUDE_BACKLOG.md | Interactive: Yarn validator (missing targets, duplicates, stray ---, unclosed, orphans, dash lines); current dialogue 0 errors / 1 warning |
| 2026-10-24 | — | skipped: weekly cap | .claude-runs/ledger.md | G2: 2+ completed runs in the last 7 days (W1 10-20, D1 10-21, plus interactive rows); G3 would also fail (Y1, X2, X4c in review) |
