# Decision — A cheaper Builder subagent builds plan steps under `/evolve`

**Status:** accepted

**Context:** One `/evolve` run plans, builds, tests, reviews, and documents. Running all of it on the strongest model is expensive, although plans are already written for a weaker executing agent: a locked `How`, Before/After, and test cards leave no choice ([`workflow-plan.md`](../../.github/skills/workflow-plan.md)). Building costs the most tokens: reading code, editing, and build-and-test loops. Judgment sits in intake, the spec, the plan, the test design, and the reviews.

What the tools offer (checked against their docs when this was decided):

- Claude Code: the `Agent` tool takes a `model` parameter; agent files in `.claude/agents/` take `model:`. Subagents load `CLAUDE.md` and `AGENTS.md`.
- VS Code Copilot: `runSubagent` accepts a model, or uses a custom agent's `model:` (`.github/agents/*.agent.md`), only up to the parent's cost tier. The Copilot harness, the default since VS Code 1.143, delegates with `task` and documents no model choice.
- Copilot CLI: only an agent file sets the model; repository instructions reach a custom subagent only with `include-custom-instructions: true`.
- Cursor: the Task tool with an agent file's `model:`; it falls back to another model without telling when the chosen one is not available.

**Decision:**

- The `/evolve` session is the **Lead** and runs on the strongest model. It does intake, the Spec delta, the gates, the test cards, the plan, every review, Prove as a user, and Heal.
- For size M and L, a **Builder** subagent on a cheaper model writes the test code from the cards, the production code from `How`, the code tests from the written code, the docs from `Docs`, and runs `Verify`, one step at a time.
- The person names the Builder model in the `/evolve` request, or the first Spec delta asks once. Gate A writes it to `spec/state.md` as `Builder:`.
- The Lead never trusts the Builder's report: it checks the diff against the brief, reads each new test against its card, checks that each changed member has its code tests, runs `Verify` itself, and notes a model other than the chosen one.
- Without a subagent on another model, the Lead builds alone, as before.

**Rejected:**

- One model for the whole run: no saving.
- Agent definition files per tool (`.claude/agents/`, `.github/agents/`, `.cursor/agents/`) with the model in them: the person chose to describe the rule in the workflow and let each tool start its subagent its own way. The files would hold a tool-specific model name in the repository and need extra settings per tool.
- A cheap session that delegates planning to a strong subagent: intake, gates, and reviews need the strong model in the session, and reviews may not run in a subagent (Section 4.14).
- The Builder writes no tests: the person allowed it. The Lead designs the tests as cards with **Content** and checks each written test against its card, which a tool check cannot do.

**Consequences:** The saving depends on the tool honoring the chosen model; the Builder's report names the model it ran on, and the run report shows a mismatch. In the VS Code Copilot harness no model choice is documented today, so the Lead may build alone there. The Lead's context grows less per step, so long runs compact less often. A test written to fit its own code stays possible; the card check and the review **Meaning** hunt are the guards.

**Enforced in:** [`workflow-evolve.md`](../../.github/skills/workflow-evolve.md) § Builder, [`workflow-implement.md`](../../.github/skills/workflow-implement.md) Stage 2, [`tech-reporting.md`](../../.github/skills/tech-reporting.md) § Spec delta, [`copilot-instructions.md`](../../.github/copilot-instructions.md) §5.1.
