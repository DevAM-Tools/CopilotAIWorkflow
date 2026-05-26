<!-- Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root. -->

# CopilotAIWorkflow

A structured GitHub Copilot workflow that enforces planning, implementation, and review discipline through one consolidated instruction source and thin process prompts.

---

## Why this repository exists

Default chat behavior is fast but often inconsistent across sessions. This repository provides a stable execution model that keeps agent behavior deterministic and auditable.

What this setup enforces:

- No implementation before plan approval.
- Explicit workflow phases for plan, implement, review, and complex-task orchestration.
- Review loops that continue until Error findings are zero (or become explicitly blocked).
- Build and test discipline with warnings treated as defects.
- Cross-file consistency, boundary validation, and release-readiness verdicts.

---

## Current repository structure

- [`.github/copilit-instructions.md`](.github/copilit-instructions.md)
: Single source of truth for quality rules, technology rules, templates, and workflow behavior.
- [`.github/prompts/Plan.prompt.md`](.github/prompts/Plan.prompt.md)
: Thin orchestrator for planning.
- [`.github/prompts/Implement.prompt.md`](.github/prompts/Implement.prompt.md)
: Thin orchestrator for implementation.
- [`.github/prompts/Review.prompt.md`](.github/prompts/Review.prompt.md)
: Thin orchestrator for review.
- [`.github/prompts/ComplexTask.prompt.md`](.github/prompts/ComplexTask.prompt.md)
: Thin orchestrator for end-to-end plan plus implement plus review loops.
- [`CUSTOM_INSTRUCTIONS.md`](CUSTOM_INSTRUCTIONS.md)
: Repository-specific overrides that take priority.
- [`COPYRIGHT`](COPYRIGHT), [`LICENSE`](LICENSE)
: Legal and licensing artifacts.

---

## Configuration precedence

Rule precedence is:

1. [CUSTOM_INSTRUCTIONS.md](CUSTOM_INSTRUCTIONS.md)
2. [.github/copilit-instructions.md](.github/copilit-instructions.md)

The prompt files in [.github/prompts](.github/prompts) are intentionally process-only and defer all policy details to the consolidated instruction file.

---

## Workflow overview

### Plan workflow

Defined by [`.github/prompts/Plan.prompt.md`](.github/prompts/Plan.prompt.md) and rules in [`.github/copilit-instructions.md`](.github/copilit-instructions.md).

Stage sequence:

1. Gather Context
2. Grill Me
3. Write Plan Artifact

Notes:

- Scope phase is intentionally removed.
- Compatibility and breaking-change questions are handled inside Grill Me.
- Plan output is artifact-first and includes review gates.

### Implement workflow

Defined by [`.github/prompts/Implement.prompt.md`](.github/prompts/Implement.prompt.md).

Stage sequence:

1. Prepare
2. Execute Steps
3. Final Verification

Notes:

- Uses review gates and remediation loops.
- Supports resume based on checklist status.

### Review workflow

Defined by [`.github/prompts/Review.prompt.md`](.github/prompts/Review.prompt.md).

Stage sequence:

1. Define Scope
2. Load Applicable Rules
3. Gather Context
4. Review
5. Output

Notes:

- Output supports chat mode and file mode.
- Exhaustive coverage is expected before finalizing findings.

### Complex-task workflow

Defined by [`.github/prompts/ComplexTask.prompt.md`](.github/prompts/ComplexTask.prompt.md) and section 5.4 in [`.github/copilit-instructions.md`](.github/copilit-instructions.md).

Stage sequence:

1. Plan
2. Checkpoint
3. Implement/Review Loop
4. Stop Conditions
5. Resume
6. Final Report

Loop semantics:

- Each remediation iteration must execute review.
- Each review iteration is persisted.
- Success requires zero Error findings in the latest review iteration.
- Cosmetic, Refactoring, and Performance findings may be deferred.
- Blocked state triggers when the same Error-class root cause persists after two remediation attempts in the same step scope.

---

## What the consolidated instructions cover

The consolidated file [`.github/copilit-instructions.md`](.github/copilit-instructions.md) contains:

- Always-on quality contract:
: correctness, security, thread safety, performance and allocations, testing, documentation, repository and git constraints.
- Technology rules:
: C# standards, TUnit testing rules, Blazor/Razor rules, and source generator rules.
- Shared output templates:
: structured question blocks and shared plan/review block format.
- Workflow contracts:
: detailed requirements for plan, review, implement, and complex-task orchestration.

---

## Adopting this setup in another repository

1. Copy [.github/copilit-instructions.md](.github/copilit-instructions.md) and [.github/prompts](.github/prompts).
2. Copy [CUSTOM_INSTRUCTIONS.md](CUSTOM_INSTRUCTIONS.md), [COPYRIGHT](COPYRIGHT), and [LICENSE](LICENSE).
3. Adjust override rules in [CUSTOM_INSTRUCTIONS.md](CUSTOM_INSTRUCTIONS.md) for project-specific needs.
4. Validate prompt paths and instruction filenames after copy.

---

## License

Copyright © 2026 DevAM. Licensed under MIT.
See [LICENSE](LICENSE).
