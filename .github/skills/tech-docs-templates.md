# Documentation Templates

Load with `tech-docs.md` when creating or restructuring a project doc, before drawing a diagram, and on `/docs-repair`. Rules (doc set, reading tiers, Sync check) live in `tech-docs.md`; this file only gives the shapes.

Copy the depth, not the content. Every section is required unless marked optional. A section that does not apply says why in one line; never `n/a` alone. Links follow Section 4.6 (clickable, relative). Diagrams: § Diagrams at the end of this file.

## README.md

```markdown
# {Name}

{One paragraph: what it is, who it is for, what problem it removes.}

## Quickstart

{Prerequisites with minimum versions. Install command. Smallest run. Expected output.}

## Usage

{Main commands or API entry points; option table or link to the reference.}

## Build and test

{Exact commands from the tech skill, Release configuration.}

## Documentation

[Architecture](docs/architecture.md) · [Concepts](docs/concepts.md) · [Domain](docs/domain/index.md) · [Spec](spec/index.md) · [Agents](docs/agents.md) · [Decisions](docs/decisions/) · [Changelog](CHANGELOG.md)
{List only the docs that exist.}

## License
```

## docs/architecture.md

The Overview is tier 1: every agent reads it before it acts. Keep it on one screen.

````markdown
# Architecture — {system}

## Overview

{Two or three sentences: what the system does, for whom, where it runs.}

| Component | Folder · entry | Responsibility | May depend on |
|-----------|----------------|----------------|---------------|
| [Cli](#cli) | [`src/Cli/`](../src/Cli/) · `Program.Main` | Parses arguments; picks the command | Import |
| [Import](#import) | [`src/Import/`](../src/Import/) · `ImportCommand` | Reads invoice files; stores the valid ones | Validation |
| [Validation](#validation) | [`src/Validation/`](../src/Validation/) · `InvoiceValidator` | Checks invoice rules | — (no I/O) |

A dependency not listed in **May depend on** is a defect or needs a decision record.

![Components and who may depend on whom](images/components.svg)
Mirrors: the **May depend on** column above.

{Optional: context diagram as SVG — users, neighbor systems, trust levels.}

## Cli

{Responsibility in two sentences. Key types, one line each.}

{Flow diagram here when the flow has more than 3 steps or crosses a component: `images/cli-flow.svg` of the steps below.}

**Flow:**
1. `Program.Main` parses options with `CliParser.Parse`.
2. {Next step: symbol, what crosses to which component.}

**Invariants:**
- {Rule other code relies on.} Enforced by `{symbol}`.

**Easy to miss:** {when you change X, also change Y and Z; why the compiler does not catch it. Or `none`.}

## Cross-cutting

| Concern | Approach | Enforced in |
|---------|----------|-------------|
| Errors | {result types at boundaries; exceptions only for bugs} | {link} |
| Configuration and limits | {Section 4.2 rows or link} | |
| Security boundaries | {STRIDE summary per boundary} | |
| Concurrency | {threading model; shared state; primitive} | |
| Performance | {hot paths and budgets} | |
````

Required diagrams:

| Diagram | Required when | File · shows |
|---------|---------------|--------------|
| Component, below the Overview table | The Overview has 2 or more rows | `docs/images/components.svg` · one box per row; one arrow per **May depend on** entry, no other arrow |
| Flow, in a component section | Its **Flow** has more than 3 steps or crosses a component | `docs/images/<component>-flow.svg` · the **Flow** steps, labeled with the symbols they name |

- ❗ Draw architecture diagrams as SVG (§ Diagrams), never Mermaid: Mermaid picks its own layout and grows wide and tall with crossing edges.
- Lay out the component diagram in rows by dependency depth: components nothing depends on at the top, components without dependencies at the bottom. Arrows point down; no arrow crosses a box.
- ❗ The component diagram and the **May depend on** column state one fact in two forms. Change both in the same edit.
- A component is one Overview row: a project, crate, package, or top-level folder with one responsibility. Never a single type.

```text
Wrong: Overview rows Cli → Import → Validation; components.svg also draws an arrow Cli → Validation.
Right: components.svg draws exactly the arrows Cli → Import and Import → Validation, in three rows.
```

## docs/concepts.md

Created with the first concept. Read by heading, never in full.

```markdown
# Concepts — {system}

How the solution works inside, across components. Structure: [architecture](architecture.md). Problem terms: [domain](domain/index.md).

## {Concept}

**Idea:** {two to four sentences: the model, algorithm, or format, and why it fits the problem.}
**Where:** [{component}](architecture.md#{component}) · `{entry symbol}`
**Rules:** {what must hold; what breaks the concept}
**Rejected:** {the simpler idea and why it fails, or a link to the decision record}
```

## docs/domain/{topic}.md

Created with the first domain fact. Layout, index, inbox, and capture rules: `tech-spec.md` § Domain knowledge. Read by heading.

```markdown
# Domain — {topic}

The problem world: true even if the code were rewritten.

## Glossary

| Term | Meaning | In code | Avoid |
|------|---------|---------|-------|
| Credit note | An invoice with a negative total that cancels an earlier one | `CreditNote` | “refund” (means a payment) |

## Rules

### {Rule name}

{The rule in one or two sentences.} **Source:** {user answer, requirement, spec link § section}. **Enforced in:** `{symbol}`.

## External facts

| Fact | Source |
|------|--------|
| A currency code has three uppercase letters | [ISO 4217](https://…) § codes |
```

## docs/decisions/{slug}.md

A decision record: one decision, why it was made, what was rejected. It keeps the next agent from undoing a choice it does not understand. Some teams call it an ADR (architecture decision record).

```markdown
# Decision — {decision in a few words}

**Status:** proposed | accepted | superseded by [{slug}]({slug}.md)

**Context:** {forces, constraints, evidence; links}

**Decision:** {one paragraph}

**Rejected:** {option} — {why} · {option} — {why}

**Consequences:** {what gets easier; what gets harder; what to watch}

**Enforced in:** `{symbol}` in [{path}](../../{path})
```

No dates in the body; git records them. A superseded record keeps its text and names its successor.

## docs/guides/{slug}.md

```markdown
# Guide — {task}

**For:** {group} · **Prerequisites:** {tools, access, state}

## Steps

1. `{command}` → {expected observable}

## Verify

{Command and the observable that proves success.}

## Troubleshoot

| Symptom | Cause | Fix |
|---------|-------|-----|
```

## Diagrams

Draw SVG by default. Mermaid picks its own layout and grows wide and tall; a hand-written SVG stays compact and lets position, grouping, and color carry meaning. ASCII art: never (`tech-docs.md` § Diagrams).

| Content | Form |
|---------|------|
| Any diagram | SVG file |
| Any diagram in `docs/architecture.md`, and its draft in a Spec delta | SVG file, never Mermaid (§ docs/architecture.md) |
| A straight chain or a simple tree: ≤ 6 nodes, one direction, no crossing edge; position, grouping, and color carry nothing | Mermaid `TD` inline allowed; never `LR` |

- The Mermaid row is an exception, never a shortcut: a state machine, a cycle, a branch that joins again, layers, a timeline, or a comparison is an SVG.
- Plans and reviews: § In plans and reviews below. Briefings: `tech-reporting.md` § Briefing. Illustrations: `workflow-illustrate.md`.

```text
Wrong: an 18-node Mermaid of the layer stack with crossing edges.
Wrong: a 7-state Mermaid state machine; Mermaid places the back edges and they cross.
Right: docs/images/layers.svg with three bands; docs/images/export-states.svg with the states in one column.
Right: Mermaid TD for the four-step chain Read → Parse → Validate → Store.
```

**In plans and reviews:** a picture (SVG by default) serves the person who approves a plan or a finding. Most steps and findings need none.

- Draw one only when its point is a relation the code spreads over several places: structure (components, layers, ownership, who may call whom), data flow across 3 or more parties or a trust boundary, a state machine or lifecycle with 3 or more states, an order in time (race, interleaving, TOCTOU, lock order), a memory or buffer layout, or a Before/After of one of these.
- Use a code snippet when the point lives in one or two places: a signature, a check, a condition, a call, a value. A picture that restates a snippet is noise: cut it.
- Draw at most one picture per plan step or finding, and at most 3 per plan or review file, unless the person asks for more. Spend them first on the Target Solution, an architecture change, and Error findings on races or data flow.
- ❗ The text stays complete without the picture. An agent builds from `How`, Before/After, and Problem/Fix, never from a picture. A fact only the picture states is missing from the text.
- Label every node and arrow with a symbol from the code or the Target Solution. The caption names what the picture mirrors (§ SVG).
- Form: an SVG file; Mermaid only under the exception row of the table above. Animate the SVG when motion carries the point: a value crossing a boundary, two threads interleaving (§ Animation).

```text
Wrong: E1 "Export accepts a duplicate name" gets an SVG with a box Export.Create and an arrow to Store.Add.
       The Problem/Fix snippet already says it.
Right: E4 "Two parallel creates both pass Contains" gets an animated two-lane timeline: A Contains → false,
       B Contains → false, A Add, B Add, two rows. The code reads as sequential; the picture shows the interleaving.
Right: a plan that moves validation from Cli into Import gets one Before/After component SVG in its Target Solution.
       Its steps get none.
```

**Mermaid** (exception row only): write a fenced `mermaid` block with `flowchart TD`. Label nodes with names from the code or the domain glossary.

**SVG:**

- Store it as `docs/images/<slug>.svg`. Embed it relative to the doc: `![{what it shows, one sentence}](images/<slug>.svg)`. On the next line, write a caption that names the code it mirrors or its data source: `Mirrors: [`ImportCommand`](../src/Import/ImportCommand.cs).`
- Write it by hand. Never add a generator, a script, or a package for it (Section 4.7).
- Set `viewBox` with width ≤ 800; no fixed `width` or `height`. Add `<title>` and `<desc>` (the text equivalent). Write text as `<text>` of ≥ 14 units. Add the copyright comment per `tech-solution.md` (`<!-- {COPYRIGHT} -->` without that skill).
- ❗ Treat an SVG as active content. Never `<script>`, `<foreignObject>`, an event attribute, an external `href`, `@import`, or an embedded raster.
- Define colors as CSS variables in an inline `<style>` with a `prefers-color-scheme: dark` override. Never let color carry meaning alone: add a label or a pattern.
- Take chart values from real data (a measurement, a file, a table) and repeat them in the caption or a table below. Never invent a number or round it to fit.
- Use a raster image (PNG) only for a screenshot, with alt text and width ≤ 1200 px.
- Outside `docs/`: store a plan’s SVG as `plans/plans_<slug>_<name>.svg`, a review’s as `reviews/review_<slug>_<name>.svg`, a Spec delta's component diagram as `reviews/delta_<slug>_components.svg`, a briefing’s SVG next to it as `reviews/briefing_<slug>_<name>.svg`, an illustration as `illustrations/<slug>.svg` (`workflow-illustrate.md`).

**Animation:**

- Animate only when the motion shows a state change, an order, or a flow. Never as decoration.
- Use CSS `@keyframes` inside the SVG. Never script or SMIL: `prefers-reduced-motion` cannot stop SMIL.
- Put the final state in the base style and the start in `from`. Under `prefers-reduced-motion: reduce`, set `animation: none`: the still shows the complete message.
- Keep one cycle ≤ 10 s. Move one element at a time. Never flash more than 3 times per second.
- Tell the sequence in `<desc>`, so the animation adds nothing a reader cannot get from the text.

Copy the depth, not the content:

```svg
<!-- {COPYRIGHT} -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 120" role="img">
  <title>Import flow</title>
  <desc>The CLI hands invoice files to Import, which calls Validation. Validation does no I/O.</desc>
  <style>
    :root { --box: #e8eefc; --line: #2b4a9a; --text: #101828; }
    @media (prefers-color-scheme: dark) { :root { --box: #1d2740; --line: #8fb0ff; --text: #e6ebf5; } }
    .node { fill: var(--box); stroke: var(--line); stroke-width: 2; }
    .edge { stroke: var(--line); stroke-width: 2; marker-end: url(#arrow); }
    text { fill: var(--text); font: 14px sans-serif; text-anchor: middle; }
  </style>
  <defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto">
    <path d="M0 0 L10 5 L0 10 z" fill="var(--line)"/></marker></defs>
  <rect class="node" x="10" y="35" width="120" height="50" rx="8"/><text x="70" y="65">Cli</text>
  <rect class="node" x="180" y="35" width="120" height="50" rx="8"/><text x="240" y="65">Import</text>
  <rect class="node" x="350" y="35" width="120" height="50" rx="8"/><text x="410" y="65">Validation</text>
  <line class="edge" x1="130" y1="60" x2="178" y2="60"/><line class="edge" x1="300" y1="60" x2="348" y2="60"/>
</svg>
```

Animation: add `<circle class="packet" cx="70" cy="60" r="6"/>` after the nodes, and this fragment to the `<style>` block. The packet travels from Cli to Import and rests there.

```css
.packet { transform: translateX(170px); animation: send 4s ease-in-out infinite; }
@keyframes send { from { transform: translateX(0); } }
@media (prefers-reduced-motion: reduce) { .packet { animation: none; } }
```
