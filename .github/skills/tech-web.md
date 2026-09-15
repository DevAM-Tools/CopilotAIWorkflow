# Web UI Rules

Load when a **product** web UI is in scope (Blazor, Rust+HTML, Python web, HTML/CSS in an app), including **planning** before files exist. Stack extras: `tech-blazor.md` plus the language skill. Journeys and debug: `tech-playwright.md`. Do **not** load this skill for `illustrations/**` (`workflow-illustrate.md`).

Implements Section 4.8 in `copilot-instructions.md`.

## Visual language

- Keep one look across pages (type, color, spacing, chrome).
- Design for **dark mode**. Set `color-scheme: dark` (or the stack equivalent). Do not build a light theme, theme switcher, or extra palettes unless the user asks — those are out of scope.
- Put shared rules in app / global / layout CSS. Put page- or component-only rules in the local stylesheet.
- Before adding a rule, choose: whole app, layout/page, or this component. Put it in the highest file that still stays correct. Do not copy the same rule into every page.

```text
Wrong: --accent copied into home.css, export.css, settings.css
Right: --accent in app.css; this card’s grid in card.css
```

## Layout

- ❗ Make the UI usable from a phone-width viewport up. Do not require horizontal scroll on content unless the user waives it.
- Stack on narrow viewports. Do not hide primary actions off-canvas with no alternative.

## Accessibility

- Put `aria-*` on interactive controls when visible text does not already name the control or its state.
- Pair every input with a visible label. Show errors next to the field.
- Make the UI keyboard-usable: sensible tab order, visible focus, `Enter` / `Escape` where expected.

## Markup and behavior

- Use semantic HTML (`header`, `nav`, `main`, `button`). Do not use `div` + click as a button.

```html
<!-- Wrong -->
<div onclick="save()">Save</div>
<!-- Right -->
<button type="button">Save</button>
```

- Validate on the server. Do not trust the browser (Section 4.2).
- Forward abort/cancel into in-flight requests when the stack provides a token (`AbortSignal`, `CancellationToken`). Stack: loaded UI skill.

## Testing

Journeys and debug: `tech-playwright.md`. Case design and suite size: `tech-test.md`.
