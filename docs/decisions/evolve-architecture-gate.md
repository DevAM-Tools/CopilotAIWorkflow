# Decision — The person approves the architecture at Gate A; architecture diagrams are SVG

**Status:** accepted

**Context:** `/evolve` kept existing documentation true: the plan's Impact search, the `Docs` field of each step, the Sync check, the review **Docs** hunt, and `/dream`. It did not make the architecture exist, and nobody approved it:

- A new project got only `spec/`. A missing `docs/architecture.md` pointed to `/docs-repair`, and a change-scoped review filed it as a Refactoring finding, so a project could grow for many slices without an architecture doc.
- The Spec delta listed public API, limits, and dependencies, but not the component split. The Lead chose components and their allowed dependencies silently in the plan; a decision record was required only when a documented boundary broke.
- Diagrams were optional. A person saw no picture of the architecture at any gate.
- The Builder handoff check covered tests and code, not the docs of the step.

The component split is the choice that is most expensive to change later. The maintainer also found Mermaid diagrams of architecture large and hard to read: Mermaid picks its own layout, grows wide and tall, and crosses edges.

**Decision:**

- The Spec delta has an **Architecture** line: the owning component of each new requirement, new or changed Overview rows with **May depend on**, a component diagram, and each choice between real alternatives with the rejected options. Without an architecture doc it holds the full Overview. Gate A approves it.
- The approved line binds the plan. The step that creates a component writes its Overview row, its section, its arrows in the component diagram, and a decision record per approved choice. The first slice creates `README.md` and `docs/architecture.md`. A component or dependency the line lacks is Decision class and becomes a card. An architecture change is never size S.
- `docs/architecture.md` carries a component diagram from 2 components on, with exactly the **May depend on** arrows, in rows by dependency depth, and a flow diagram for each flow of more than 3 steps or across components.
- Architecture diagrams and their Spec delta draft are hand-written SVG, never Mermaid.
- The Builder handoff check, the review **Docs** hunt, and `/dream` check the Overview and the diagrams. A missing `README.md` or `docs/architecture.md` next to production code is an Error. The Gate B briefing links the component diagram.

**Rejected:**

- The agent decides the architecture and only documents it: cheaper by one Spec delta line, but the most expensive choice to undo would reach no person.
- Keep diagrams optional: the existing rules kept present diagrams true but never made one exist.
- Mermaid for architecture: its layout grows wide and tall with crossing edges; an SVG keeps the rows and arrows where the author puts them.
- A generator for the SVG: Section 4.7 forbids new tools and scripts without approval, and a hand-written SVG of a few boxes is enough.

**Consequences:** Every Spec delta costs one more line, and the first one of a project costs a full Overview. Each new component costs a hand-edited SVG; the component diagram and the **May depend on** column state one fact in two forms and must change in the same edit. A person sees the architecture at Gate A and Gate B. Watch: whether hand-written SVGs drift from the Overview; the review **Docs** hunt compares them.

**Enforced in:** [`workflow-evolve.md`](../../.github/skills/workflow-evolve.md) Stage 2, Gate A, Stage 4, § Builder; [`tech-docs.md`](../../.github/skills/tech-docs.md) § Doc set, § Diagrams, § Review severity; [`tech-docs-templates.md`](../../.github/skills/tech-docs-templates.md) § docs/architecture.md, § Diagrams; [`tech-reporting.md`](../../.github/skills/tech-reporting.md) § Spec delta, § Briefing; [`workflow-review.md`](../../.github/skills/workflow-review.md) **Docs** hunt; [`workflow-dream.md`](../../.github/skills/workflow-dream.md) § 5.
