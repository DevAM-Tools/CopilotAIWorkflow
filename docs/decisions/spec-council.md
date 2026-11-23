# Decision — A full council checks every draft Spec delta for gaps

**Status:** accepted

**Context:** Before Gate A, the only check of requirements was Coverage (`workflow-requirements.md` Stage 7). It proves that nothing the person said was lost. It cannot find what the person did not say: an operator who dictates "create and list exports" is not asked what happens on a full disk, on delete, or when two users act at once. The multi-view Sweep ran in `/plan`, after approval, and the council never ran on its own. A gap found while building costs a new Spec delta and another round at Gate A.

The maintainer weighed three options:

- A) A short gap check in every requirements run, and the full council only on the first Spec delta of a project.
- B) The full council on every draft Spec delta.
- C) The short gap check only.

**Decision:** B. `workflow-council.md` has a Spec mode, always Full:

- The Skeptic walks thirteen gap classes per touched area: errors, limits, security and privacy, concurrency, lifecycle, states, data, people, operation, agent access, consistency, testability, and architecture. Each class ends as covered by an ID, out of scope, or a gap.
- The other views ask whether the set solves the job, which unsaid obligation a person would expect, whether a first-time reader understands each entry, and whether each Done-when can run.
- The chairman answers **Complete** or **Gaps found** and gives each gap one action: `add`, `fix`, `ask`, or `out`. No gap is dropped because it is small.
- `/requirements` runs it as Stage 8 after Coverage. `/evolve` runs it in Stage 2 on the whole draft, test cards and the **Architecture** line included, and again when a Gate A change adds a requirement, changes the scope, or changes the architecture.
- The Spec delta carries a **Council** line with the verdict, the link to the council file, and where each gap landed. No Spec delta reaches approval without it.

**Rejected:**

- A: cheaper per run, and the agent recommended it; the maintainer chose the full check on every delta over the saving.
- C: the cheapest, but the first architecture would get no critical look.
- A council in a subagent: Section 4.14 forbids it; the council is structured self-critique in the same session, with the Skeptic written first.

**Consequences:** Each Spec delta costs a full council: noticeably more tokens and time, also for small changes. A size S fix skips Stage 2 and with it the council. Requirements the council adds are marked `Source: Council`, so the person approves them knowingly. The council is the same agent that wrote the draft; writing the Skeptic first and the fixed gap classes compensate in part. Watch: whether the council finds gaps that the person then rejects as `out` most of the time; then option A becomes the cheaper equal.

**Enforced in:** [`workflow-council.md`](../../.github/skills/workflow-council.md) § Spec; [`workflow-requirements.md`](../../.github/skills/workflow-requirements.md) Stage 8; [`workflow-evolve.md`](../../.github/skills/workflow-evolve.md) Stage 2 and Gate A; [`tech-reporting.md`](../../.github/skills/tech-reporting.md) § Spec delta; [`copilot-instructions.md`](../../.github/copilot-instructions.md) §5.1.
