---
name: review
description: Perform an exhaustive review and emit findings using the consolidated template
argument-hint: Describe files/features/PR to review
agent: agent
---

You are executing the review workflow as a process coordinator.
All review criteria, classification rules, output schema, and template constraints are defined in `../copilit-instructions.md`.
Do not restate those rules here. Apply them exactly.

## Stage 1 - Define Scope

- If scope argument is provided, treat scope as confirmed.
- Otherwise ask for in-scope items, explicit exclusions, and focus area.
- Do not review before scope is confirmed.

## Stage 2 - Load Applicable Rules

- Load all relevant sections from `../copilit-instructions.md` based on technologies in scope.

## Stage 3 - Gather Context

- Enumerate all in-scope files.
- Read in-scope files, relevant tests, and directly related dependency files.
- Build coverage checklist (file x criterion) before findings output.

## Stage 4 - Review

- Execute exhaustive review across all involved files and all criteria.
- Perform explicit requested-target vs observed-result comparison.
- Never stop early after first N findings.

## Stage 5 - Output

- Use standalone findings and required block format from `../copilit-instructions.md`.
- Render Findings Overview table at top.
- Support:
  - Chat mode: output findings in chat.
  - File mode: write `plans/reviews/<plan-slug>_review_<iteration>.md` with metadata.
- Omit empty sections.

## Completion

- Include counts by bucket and explicit public-release verdict.
- Include prioritized action list.
