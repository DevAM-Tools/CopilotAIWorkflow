# Decision — Requirements-driven loop with three human gates

**Status:** accepted

**Context:** The kit had strong single workflows but no way to develop a product continuously from dictated requirements. Three problems stood out. Agents stopped on small decisions. A person had to read plans and long briefings, and human review capacity limits throughput. A typical planning run loaded about 35,000 tokens of instructions, too much for a 100,000-token local model.

Findings that shaped the design:

- Human review is the bottleneck. DORA: small batches amplify the benefit of AI adoption and act as a [safety net](https://dora.dev/capabilities/working-in-small-batches/).
- Spec tools drown reviewers in repetitive Markdown and fit small changes badly ([Böckeler](https://martinfowler.com/articles/exploring-gen-ai/sdd-3-tools.html)).
- Long-running agents need a durable feature list, a progress file, one feature per session, and verification as a user ([Anthropic](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)).
- Conditional rules scale where always-on rules do not; deterministic steps must always run; more than two CI rounds has diminishing returns ([Stripe](https://stripe.dev/blog/minions-stripes-one-shot-end-to-end-coding-agents)).
- A compiled, indexed, regularly linted wiki beats re-deriving knowledge ([Karpathy](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f)). The gist defines **lint**; the “dream” pass comes from its comments and from the community.
- Tools for agents need few task-level operations, high-signal output, and actionable errors ([Anthropic](https://www.anthropic.com/engineering/writing-tools-for-agents)).

**Decision:**

- `/evolve` composes the existing workflows. A person acts only at Gate A (Spec delta), Gate E (decision cards), and Gate B (briefing). The single workflows stay usable alone.
- Requirements and test content live in a tracked `spec/`, per area, with stable IDs. They outlive plans.
- The trace lives only in `spec/` and points at code. One home per link; coverage is derived by search.
- Section 4.16 sorts every open point into Covered, Detail, Assumption, or Decision. Only Decision reaches a person, and other work continues meanwhile.
- Human-facing artifacts have length budgets. Plans and findings stay detailed and are agent handoffs.
- Work is sized S, M, or L, so a small fix skips the plan.
- `/dream` consolidates domain knowledge, requirements, test content, and the trace. It never changes production code.
- Always-on text shrinks; mechanisms move to skills that a stage names explicitly. Templates load at the writing stage. The Sweep leaves the council skill.

**Rejected:**

- IDs in production code or comments: puts tracking data into the product and contradicts Section 4.1. IDs in test code were rejected here at first and later allowed, one marker per test: [spec-trace](spec-trace.md).
- JSON for requirements (Anthropic found models overwrite it less): a person reviews these entries. Closed fields and § Editing shared knowledge address the risk instead.
- A person approves every plan: the review load this loop exists to remove. A project can add `plan` to `Gates:`.
- One long append-only domain file: agents append without reading and duplicate.
- A separate `.agents/skills/` tree: `.claude/skills/` is already read by Claude Code, Cursor, and VS Code.
- An MCP server for every product: a CLI or REST surface is enough for most; MCP stays a thin adapter.

**Consequences:** Integrity checks run by search, so a weak agent can miss a hit; a dedicated checker tool would make them deterministic. Language skills lost only examples that repeated their rule; `tech-csharp.md` stays the largest load of a C# run. Existing `requirements/req_*.md` files keep working as user-supplied sources.

**Enforced in:** [`workflow-evolve.md`](../../.github/skills/workflow-evolve.md), [`tech-spec.md`](../../.github/skills/tech-spec.md) § Integrity checks, [`copilot-instructions.md`](../../.github/copilot-instructions.md) Section 4.16.
