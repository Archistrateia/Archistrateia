# AGENTS.md

## Codex Init For This Repo

1. Validate tooling:
   - `dotnet --version`
   - `ls /Applications/Godot_mono.app/Contents/MacOS/Godot`
2. Build once:
   - `dotnet build`
3. Run tests with the project runner, not `dotnet test`:
   - Full suite: `./run_tests.sh all --show-failures-only`
   - Machine-readable: `./run_tests.sh all --ai-output`
   - Single phases: `./run_tests.sh nunit`, `./run_tests.sh scenes`, `./run_tests.sh ui`
4. Validate key gameplay interaction behaviors when touching input code:
   - Edge mouse scrolling delay + arrow-key override behavior (covered by `ViewportControllerScrollBehaviorTest`)
   - Inspect-mode hover info toggle via `I` key (covered by `HoverInfoModeTest`)
5. For major refactors, prefer these targeted verification suites in addition to phase runs:
   - Phase transitions and side-effects: `PhaseTransitionCoordinatorTest`
   - Viewport input/state isolation: `ViewportControllerRefactoringTest`
   - Movement state isolation: `MovementValidationLogicTest`, `MovementCoordinatorTest`

## Notes

- This repository uses Godot scene-based test runners. `dotnet test` does not execute the real suite here.
- In restricted sandboxes, headless Godot commands may require elevated execution permissions.
- In-game tooltip behavior is inspect-mode gated: press `I` to toggle hover info mode on/off.
- Architecture guardrails:
  - Keep phase side-effects centralized through `PhaseTransitionCoordinator` (avoid duplicate phase handling paths).
  - Prefer injected state/services over static mutable state (`HexGridViewState`, movement system injection).
  - Keep shared UI geometry in `UILayoutMetrics`.
