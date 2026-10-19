# CLAUDE.md

The project guidance for coding agents lives in AGENTS.md so that every agent reads the same instructions:

@AGENTS.md

## Claude Code notes

- Build, test and pack in the SDK container when `dotnet` isn't on the PATH (see "Build and test" in AGENTS.md). Mount the repo read-only and use `--artifacts-path` so nothing root-owned is left behind.
- Treat pushing to GitHub, commenting on or closing issues, changing repository settings (e.g. creating the `nuget` environment) and dispatching `publish.yml` as outward-facing actions: confirm with the user first.
