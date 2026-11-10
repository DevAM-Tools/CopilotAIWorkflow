# Spec — CopilotAIWorkflow tools

The agent-workflow kit asks agents to keep requirements, test content, tests, and code connected. Nothing checks that today, and agents do not always work cleanly. The maintainer asked for a small tool that makes those connections checkable, without the rigidity that made ExitPointGaps tiresome to use.

## User groups

- Agent — runs the tool after each change; needs to know exactly what is wrong and how to fix it, without noise from unrelated work.
- Maintainer — decides at the gates; needs to see what changed in meaning and what an agent waved through, without reading every file.

## Boundary

The tool runs on a developer machine from a repository root, offline, on demand. It reads files under `spec/`, test source files, and test result files that another process wrote. It builds nothing and runs no tests. It writes only `spec/trace.lock`. Its neighbors are the test runner, which produces result files, and git, where the person reviews and commits.

## In scope

Reading the spec strictly; checking links between requirements, test content, and tests; detecting what changed since the last accepted state; reading test evidence; telling agent and maintainer the result.

## Out of scope

Judging whether a test really proves a requirement; judging whether code fits a requirement; running tests or builds.

## Areas

| Area | Requirements | Tests | Covers |
|------|--------------|-------|--------|
| Trace | [trace](requirements/trace.md) | [trace](tests/trace.md) | Checking the connections between requirements, test content, and tests |
| Gaps | [gaps](requirements/gaps.md) | [gaps](tests/gaps.md) | Pilot: the exit codes of the ExitPointGaps gate |
