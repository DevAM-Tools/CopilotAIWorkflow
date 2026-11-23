# Web UI Rules

Load when a **product** web UI is in scope (Blazor, Rust+HTML, Python web, HTML/CSS in an app), including **planning** before files exist. Stack extras: `tech-blazor.md` plus the language skill. Journeys and debug: `tech-playwright.md`. Do **not** load this skill for `illustrations/**` (`workflow-illustrate.md`).

Implements Section 4.8 in `copilot-instructions.md` for **product** web UIs.

## Visual language

- Keep one look across pages (type, color, spacing, chrome).
- **Dark mode** ships by default. Set `color-scheme: dark` (or the stack equivalent).
- Build a light theme only when the user asks for one. Then ship both themes from one token set and a way to switch between them.
- ❗ The user asked for two themes but did not say how the theme is chosen (the system setting, a switch in the UI, or both; whether the choice is remembered): ask it as Decision class before planning the switch. Never pick the switch silently.
- Build no theme or palette the user did not ask for.
- ❗ Every color is a token in the app sheet. Offer a named data palette for marks, badges, and series; components and plugins take their colors from it, never from a literal.
- ❗ The dark theme is low in contrast and desaturated, so nothing glares: the page lies on a very dark gray, never on black, and text and marks use muted hues. Check it with these bounds:
  - Base surface: each of red, green, and blue between 18 and 32 (`#121212` to `#202020`).
  - Text and mark colors: their red, green, and blue values differ by at most 128 (desaturated), and their contrast to the surface they lie on is between 4.5:1 and 13:1.
  - Lines and focus rings: at least 3:1.
- A saturated color glares next to muted text even when it passes 4.5:1; the 128 bound catches it. Pure white on black (21:1) glares; the 13:1 bound catches it.
- ❗ One automated test walks every color token of the dark theme and fails on a color outside these bounds. The token list pairs each text or mark color with its surface. Add a new color to the token list first, so the test sees it.
- Record the design language in `docs/concepts.md` § Design language: tokens, the data palette, and each recurring pattern (for example the entry with a state mark). When the project keeps a gallery page, it shows each pattern. Look details are not requirements: `workflow-requirements.md` § What gets an ID.
- CSS and JS file placement: sections below.

```text
Wrong: --accent copied into home.css, export.css, settings.css
Right: --accent in app.css; this card’s grid in card.css
```

```text
Wrong: #ffffff text on #000000 — black page, contrast 21:1, glaring.
Right: #d4d4d4 text on #181818 — dark gray page, contrast 12.0:1.
Wrong: #4da3ff on #181818 for a widget mark — channel spread 178, saturated, glaring next to muted text.
Right: #7cb7e8 on #181818 — channel spread 108, contrast 8.3:1.
```

## Responsive (mandatory)

❗ Every product UI is **responsive**. That is the default, not a polish pass. Desktop-only layout is allowed only when the user **explicitly** waives it.

- Ship a real phone layout, then enhance at the locked breakpoints. Do not design desktop first and squeeze it.
- Supported width **floor is 320 CSS pixels**. Content, primary actions, and chrome stay usable from that floor up.
- ❗ Do **not** require page-level horizontal scroll. Code blocks, charts, and maps may scroll **inside** their own region. Tables may not (§ Lists and tables).
- `overflow-x: hidden` on `body` / the viewport is not a responsive layout. Fix the overflowing box.
- Stack on phone. Do not hide primary actions off-canvas with no visible control and no alternative.
- Breakpoints are for **layout changes** (stack vs columns, header vs side nav). Do not add a query just to bump font size or padding by a few pixels — use `clamp()` / fluid spacing instead.
- Include `<meta name="viewport" content="width=device-width, initial-scale=1">` (or the stack equivalent). Without it, CSS breakpoints do not apply on phones.

```text
Wrong: min-width 1100px app; phone users pinch-zoom
Right: column flex on phone; row + side nav from 1280px
```

## Breakpoints

Lock these values. Do not invent extra bands unless the user asks. Custom properties **cannot** drive `@media` conditions: repeat the **literal pixels** in CSS and in `matchMedia`.

| Band | CSS | Reference devices | Layout duty |
|------|-----|-------------------|-------------|
| **phone** | no query (default) | iPhone SE 1st gen (320), SE 2nd/3rd gen (375) | one column; cards; stacked chrome |
| **tablet** | `@media (min-width: 744px)` | iPad Mini (744 portrait, 1133 landscape) | two columns; tables that fit |
| **notebook** | `@media (min-width: 1280px)` | 14-inch notebook (1280–1536) | full chrome; side nav |
| **desktop** | `@media (min-width: 1920px)` | Full HD; 28–32-inch 4K at 200 % | more columns or panels |
| **ultrawide** | `@media (min-width: 2560px)` | 4K at 150 %; 34–49-inch ultrawide | per § Width policy |

Write **mobile-first** `min-width` queries. Never define the phone layout with `max-width`.

```css
/* Right — phone is the default; larger bands add layout */
.toolbar { display: flex; flex-direction: column; gap: 0.75rem; }

@media (min-width: 744px) {
  .toolbar { flex-direction: row; align-items: center; }
}

@media (min-width: 1280px) {
  .shell { grid-template-columns: 16rem minmax(0, 1fr); }
}
```

```css
/* Wrong — desktop-first; phone is an afterthought */
.toolbar { display: flex; flex-direction: row; }

@media (max-width: 743px) {
  .toolbar { flex-direction: column; }
}
```

**Verify** with these viewports (Playwright: `tech-playwright.md`):

| Band | Viewport | Required |
|------|----------|----------|
| phone | `320 × 568` | Always |
| tablet | `744 × 1133` | Always |
| notebook | `1440 × 900` | Always |
| desktop | `1920 × 1080` | The page changes at 1920 |
| ultrawide | `3440 × 1440` | The page changes at 2560 |

## Width policy

❗ Decide the width per page type in the plan. Neither a narrow column for everything nor stretched text by default.

| Page type | Width |
|-----------|-------|
| Reading text | Lines ≤ ~75ch, centered |
| Forms | ≤ ~40rem |
| Data, lists, dashboards, editors | Use the full width: add columns or panels at each larger band |

Offer ultrawide layouts where the application gains from them. Otherwise cap the shell and center it.

## Density

❗ A data UI is compact by default: most of the screen shows data, also on a phone. Chrome above the content (menu, page top row, headers) takes less than a quarter of the viewport height on a phone.

- ❗ Get density from form before pixels: one line per entry with shortened text, icon buttons in place of text buttons, a navigation rail that collapses to icons, cards of two or three lines.
- Write the scale below once as tokens in the app sheet, in `rem`, so it scales with the text size setting. The values hold at the default text size.
- Never go below the scale to gain space. Never raise a size above it without a reason in a comment at the token; “air” is no reason.
- The pointer class picks the column: `@media (pointer: coarse)` for a finger, the default for a mouse. Never the screen width: a tablet at 1133 px wide has a finger.

Text rows give the font size; every other row gives the height.

| Element | Mouse | Finger |
|---------|-------|--------|
| Body text | 12–13 px | Same |
| Secondary text (second lines, keys, badges) | ≥ 11 px | Same |
| Smallest text anywhere (count pills only) | 10 px | 10 px |
| List row, one line | 24 px | 36 px |
| List entry with a second line | 40 px | 48 px |
| List header | 24 px | 36 px |
| Icon button, text button, input | 28 px | 40 px |
| Icon button inside a row | 24 px | 40 px hit area |
| Menu entry | 28 px | 40 px |
| Page top row, widget or card header | 34–40 px | 40–44 px |
| Status line (text, not a target) | 22–24 px | Same |

**Target size**

- A pointer target is at least 24 × 24 CSS px with a mouse and at least 40 × 40 CSS px with a finger. 24 px is the WCAG 2.2 floor (2.5.8, AA); 44 px (2.5.5, AAA) costs a fifth of the rows on a phone; 40 px is the smallest size that stays easy to hit in a header with several buttons side by side.
- A list row that spans its list may be 36 px high with a finger.
- A small icon may keep its drawn size when the hit area around it meets the target size. Hit areas of neighbors never overlap.

```css
/* Right — one token set in app.css; the pointer class switches it */
:root { --row: 1.5rem; --control: 1.75rem; --target: 1.5rem; }
@media (pointer: coarse) {
  :root { --row: 2.25rem; --control: 2.5rem; --target: 2.5rem; }
}

/* Wrong — the screen width picks the size: a tablet at 1133 px gets mouse targets */
@media (max-width: 743px) { :root { --control: 2.5rem; } }
```

## Lists and tables

- A list of things that have a state (messages, jobs, log entries, plugins) is a list of entries, not a column table. An entry holds a state mark (a colored stripe plus the state word, never color alone), a main line, an optional muted second line, trailing facts, and icon actions.
- Use a column table for uniform records that a person compares by column.
- Details that do not fit an entry open on demand: an expander inside the entry for structured parts, or a fold-out panel at the bottom of the page for the full record and long text. The panel is closed until a person opens it.

❗ A table only where its columns fit. On smaller screens the same data is a card list that scrolls vertically only. Design the card first.

- ❗ A card on a small screen is designed: the primary field is its title, actions are an icon row, and the other fields sit on one or two lines. Never one label–value line per column. A card is not taller than three table rows until a person expands it.
- ❗ Never let a table scroll horizontally. Never hide a column on small screens without showing its value in the card or in its expanded view.
- Default CSS is the card layout. Raise it to a table with a **container query** at the width where all columns fit: the sum of their minimum widths. That width is a property of the table, not a new band.
- Keep one markup. Set `data-label` only on a field whose value does not name itself (a bare number or date); the card shows it as a prefix. Set `role="table"`, `row`, `columnheader`, `cell` where a `display` change removes table semantics.
- Sorting and filtering stay available in card mode; column headers are gone there.

```css
.list-slot { container-type: inline-size; }

/* Default: cards. Line 1: the primary field and the actions. Then the other fields flow, muted. */
.data-table, .data-table tbody { display: block; }
.data-table thead { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); }
.data-table tr { display: flex; flex-wrap: wrap; column-gap: 0.75rem; }
.data-table td { display: block; min-width: 0; color: var(--text-muted); }
.data-table td.primary { flex: 1 1 0; order: -2; color: var(--text); font-weight: 600; }
.data-table td.actions { order: -1; }
/* A full-width empty item after the actions breaks the line */
.data-table tr::after { content: ""; order: -1; flex-basis: 100%; }
.data-table td[data-label]::before { content: attr(data-label) " "; }

/* All five columns need 52rem */
@container (min-width: 52rem) {
  .data-table { display: table; }
  .data-table thead { display: table-header-group; position: static; width: auto; height: auto; clip-path: none; }
  .data-table tbody { display: table-row-group; }
  .data-table tr { display: table-row; }
  .data-table tr::after { content: none; }
  .data-table td { display: table-cell; color: inherit; }
  .data-table td::before { content: none; }
}
```

## Overflow

❗ No element paints outside its box, at any band.

- Set `min-width: 0` on flex and grid children that hold text. Prefer `minmax(0, 1fr)`.
- Set `overflow-wrap: anywhere` on user text, IDs, paths, and URLs.
- No fixed width on content. Media: `max-width: 100%`.
- `overflow: hidden` to hide an overflow is a defect, unless it truncates with an ellipsis and the full value stays reachable by tooltip.

## Icons and tooltips

- ❗ Actions in toolbars, page top rows, widget and card headers, and list entries are icon buttons with a tooltip. Text stays on dialog buttons, on the one primary action of a page, where no icon is unambiguous, and on a destructive or irreversible action that runs without a confirmation dialog.
- ❗ A component that lets its user add header actions takes them as declared tools (icon, name, tooltip, state) and renders them itself. It never hands out a free markup slot for buttons.
- ❗ Every icon button has an accessible name (`aria-label`). The icon is `aria-hidden`.
- ❗ Every shortened text shows its full value in a tooltip, up to 600 characters or 12 lines, whichever comes first. Past the bound the tooltip says that more exists, and a details view holds the rest.
- A tooltip may inform, not only name: an entry’s full text, a status with its reason, a badge’s meaning.
- A tooltip has a maximum width and height and stays inside the viewport.
- ❗ A tooltip opens on hover and on keyboard focus. It stays open while the pointer rests on the element or on the tooltip itself, so the reader can move onto it to read or select. It closes when the pointer leaves both, when focus leaves the element, and on `Escape`. A tooltip that stays open after the pointer left is a defect. A native `title` alone fails keyboard and touch.
- ❗ Touch has no hover: open the tooltip on long-press, and never put information the task needs only in a tooltip on a touch layout.
- One icon set, inline SVG. One icon, one meaning across the app. An icon package is a dependency (Section 4.7).
- Icon button sizes and hit areas: § Density.

## CSS

Before adding a rule, choose the highest file that still stays correct: **app** → **layout / page** → **this component**. Do not copy the same rule into every page.

| Layer | Lives in | Owns |
|-------|----------|------|
| App | `wwwroot/app.css`, `styles.css`, or stack global sheet | tokens, reset, `color-scheme`, typography, shared utilities |
| Layout / page | layout or page stylesheet next to that component | shell grid, header, nav, footer |
| Component | `Name.razor.css`, `name.css` beside the component | that control only |

- Blazor component sheets: `tech-blazor.md` (`::deep` rules stay there).
- Do not add Bootstrap, Tailwind, or another CSS framework to “get” breakpoints. Use this table in the app’s own CSS unless the user approved a dependency.
- Prefer flex / grid, `gap`, `minmax(0, 1fr)`, and `clamp()` for type and spacing.
- Page chrome uses the viewport queries above. Components that sit in a **variable-width slot** (card in a sidebar or in the main column) should use **container queries** (`@container`) so they follow their parent, not the window.
- Do not put layout in `style=""` attributes. Do not inject breakpoint CSS from JavaScript.
- Keep selectors short. Restyle via classes on markup you own, not long descendant chains into child components.

```css
/* Wrong — component copies app tokens and guesses a new breakpoint */
.card { --accent: #7dd; }
@media (min-width: 900px) { .card { grid-template-columns: 1fr 1fr; } }

/* Right — tokens in app.css; this card follows its container.
   The slot that hosts the card must declare itself a container,
   or the @container rule never matches. */
.card-slot { container-type: inline-size; }
.card { display: grid; gap: 1rem; }
@container (min-width: 768px) {
  .card { grid-template-columns: 1fr 1fr; }
}
```

## JavaScript

JavaScript is for **behavior** (events, fetch, dialogs, charts). CSS owns **layout**. Do not use JS as a responsive engine.

- Colocate: app startup script for boot; `Name.razor.js` / `name.js` beside the component. Blazor isolation: `tech-blazor.md`.
- Do not toggle `mobile` / `desktop` classes from `window.resize` or `innerWidth`.
- When JS **must** know the band (canvas, virtualized lists, moving a node CSS cannot reorder), use `matchMedia` with the **same pixels** as the table. Listen to `change`; do not poll `resize`.
- Prefer CSS (`flex-direction`, `grid-template-areas`, `order`) over moving DOM nodes at a breakpoint.
- Do not put CSS in JS strings. Do not sniff the user agent to pick a layout.
- Forward abort/cancel into in-flight requests when the stack provides a token (`AbortSignal`, `CancellationToken`).

```js
// Wrong
window.addEventListener("resize", () => {
  document.body.classList.toggle("mobile", innerWidth < 744);
});

// Right — JS branches only when CSS cannot
const notebook = window.matchMedia("(min-width: 1280px)");
notebook.addEventListener("change", syncChartSize);
syncChartSize(notebook);
```

## Accessibility

- Put `aria-*` on interactive controls when visible text does not already name the control or its state.
- Pair every input with a visible label. Show errors next to the field.
- Make the UI keyboard-usable: sensible tab order, visible focus, `Enter` / `Escape` where expected.

## Limit settings

An end-user setting for a trust-boundary limit (Section 4.2) explains itself where the user changes it.

- Label with the unit. Help text next to the field: what it limits, why it exists, the default, the allowed range, and the risk of raising or lowering it.
- Mark an **open by default** limit visibly. Offer a way back to the default.
- Set `min` / `max` / `step` on the input and validate again on the server. An out-of-range value shows the range next to the field.

```text
Wrong: "Max size: [____]"
Right: "Max import size (MB): [1024]  1–65536 · default 1024.
        Stops one file from filling memory. Higher values need more RAM."
```

## Markup and behavior

- Use semantic HTML (`header`, `nav`, `main`, `button`). Do not use `div` + click as a button.

```html
<!-- Wrong -->
<div onclick="save()">Save</div>
<!-- Right -->
<button type="button">Save</button>
```

- Validate on the server. Do not trust the browser (Section 4.2).

## Testing

Journeys, viewports, and debug: `tech-playwright.md`. Integrated browser: Section 4.8. Case design and suite size: `tech-test.md`.
