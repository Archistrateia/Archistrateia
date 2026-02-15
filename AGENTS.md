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

## Notes

- This repository uses Godot scene-based test runners. `dotnet test` does not execute the real suite here.
- In restricted sandboxes, headless Godot commands may require elevated execution permissions.
