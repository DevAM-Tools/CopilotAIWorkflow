# Illustrate Workflow

Load on `/illustrate`, or when a plan/requirements/concept needs an interactive HTML illustration. Apply `copilot-instructions.md` Sections 2–4. Do not implement product code unless the user asked for the illustration only.

**Purpose:** Make one item of interest easier to understand. Slideshow or interactive demo. Self-contained so a person can zip and share it.

## Stage Order

1. Confirm scope
2. Choose form
3. Build
4. Verify

## Stage 1 — Confirm scope

- Item of interest: plan, requirement set, architecture, API, flow, or named concept.
- Read that source **in full**.
- Output path: user path, else `illustrations/<slug>.html`, or `illustrations/<slug>/` if more than one file is justified.

## Stage 2 — Choose form

- Default: **one HTML file**. Extra files only when they clearly help; then keep them in one folder.
- Self-contained: relative paths only; no build step; no npm; no bundler.
- SVG inlined or as sibling `.svg` files is welcome.
- Animation and interactivity are welcome when they **explain**. Do not decorate.
- External libraries: user approval, except the allowlist below (pinned URL + version).

**Allowlist** (no extra consent):

| Lib | URL |
|-----|-----|
| Pico CSS 2.0.6 | `https://cdn.jsdelivr.net/npm/@picocss/pico@2.0.6/css/pico.min.css` |
| Mermaid 11.4.1 | `https://cdn.jsdelivr.net/npm/mermaid@11.4.1/dist/mermaid.min.js` |
| three.js 0.170.0 | `https://cdn.jsdelivr.net/npm/three@0.170.0/build/three.module.js` |
| three OrbitControls 0.170.0 | `https://cdn.jsdelivr.net/npm/three@0.170.0/examples/jsm/controls/OrbitControls.js` |
| Babylon.js 7.54.0 | `https://cdn.jsdelivr.net/npm/babylonjs@7.54.0/babylon.js` |

Pin **exact** version in the URL. Same three.js version for core and OrbitControls. 3D is for explaining structure (API graph, process, layout) — not decoration.

Anything else (D3, highlight.js, another 3D engine) → Grill Me: name, what it does, why needed, license.

Docs for the agent: [MDN HTML](https://developer.mozilla.org/en-US/docs/Web/HTML), [MDN SVG](https://developer.mozilla.org/en-US/docs/Web/SVG), [Mermaid](https://mermaid.js.org/intro/), [three.js](https://threejs.org/docs/), [Babylon.js](https://doc.babylonjs.com/).

## Stage 3 — Build

- Semantic HTML. Keyboard usable. Responsive layout (Section 4.8). Visible strings in **English** (Section 4.6).
- 3D: ES modules + import map for three.js, or a script tag for Babylon.js. Keep the camera + one explanatory object; do not ship a game.
- Inline SVG for diagrams that must ship offline-after-zip (Mermaid needs the CDN).
- Copyright header on `.html` / `.svg` per `tech-solution.md` when that skill is loaded; otherwise `<!-- {COPYRIGHT} -->`.

### Slides

When the illustration is a slideshow, **all** of the following are required:

1. **Slide overview** — a visible list or grid of every slide (title + index). Clicking a row goes to that slide.
2. **Navigation** — previous / next, and a position indicator (`3 / 12`). Keyboard: `←` `→` (and `Home` / `End` when easy).
3. **One slide visible at a time** in the main stage; overview may stay on the side or as a drawer.
4. Overview stays usable on a narrow viewport (stack it; do not clip).

```html
<nav aria-label="Slide overview">
  <ol>
    <li><a href="#slide-1">Intent</a></li>
    <li><a href="#slide-2">Public API</a></li>
  </ol>
</nav>
<p><button type="button">Previous</button> <span>2 / 8</span> <button type="button">Next</button></p>
```

## Stage 4 — Verify

Verify with **Playwright** (`tech-playwright.md`). Do not eyeball. Load that skill.

- Spec next to the illustration when tests will be kept: `illustrations/<slug>.spec.ts` or inside the folder. Otherwise a one-off local Playwright run / screenshot against `file://` or a static server (`tech-playwright.md`).
- Assert: page loads; for slides, overview count, next/prev, position text; for 3D, canvas (or WebGL) is visible and not empty.
- Zip test: the folder or file must work after copy, without repo root.
- No network except allowlisted CDNs (or none, if SVG-only).

```bash
pnpm exec playwright test illustrations/slug.spec.ts
pnpm exec playwright screenshot --viewport-size=1280,720 file:///…/illustrations/slug.html
```

## Completion

Status table, path, how to open, risks ≤5. Chat: path only.
