# Decision — Product web UIs are compact, dark, and low in glare

**Status:** accepted

**Context:** Product web UIs built with the kit showed too little data per screen, also on a phone: roomy rows, 44 × 44 px finger targets everywhere, and chrome that took a large share of the phone height. Plans and reviews also replaced the patterns of a mockup (an entry with a state, for example, by a table) without asking. Dark themes glared: pure white text on black reaches 21:1, and saturated accents stand out next to muted text even when they pass 4.5:1.

**Decision:**

- A data UI is compact by default, also on a phone. One density scale, written once as `rem` tokens, sets body text 12–13 px, one-line rows 24 px with a mouse and 36 px with a finger, and controls 28 px and 40 px. The pointer class picks the size, never the screen width. Menu, page top row, and headers stay under a quarter of the phone height.
- Targets are at least 24 × 24 px with a mouse and 40 × 40 px with a finger.
- Toolbar, header, and list-entry actions are icon buttons with a tooltip; a component takes header actions as declared tools. Every shortened text shows its full value in a tooltip, up to 600 characters or 12 lines.
- A list of things with a state is a list of entries with a state mark; a phone card has a title and one or two lines.
- Every color is a token. The dark theme lies on a very dark gray (`#121212` to `#202020`); text and mark colors have a channel spread of at most 128 and a contrast of 4.5:1 to 13:1. One test walks every token.
- A light theme ships only on request, then with a way to switch; when the person did not say how the theme is chosen, the agent asks.
- `/plan` names the mockup page each step builds; replacing a mockup pattern is Decision class with a decision record, and `/review` checks content parity with the mockup at the phone and the notebook viewport.

**Rejected:**

- The size chosen by screen width: a tablet used with a finger would get mouse-sized targets.
- Pure white on black: 21:1 glares; the 13:1 bound excludes it.
- Saturated accent colors: they glare next to muted text although they pass 4.5:1; the 128 channel-spread bound excludes them.
- A light theme by default: the kit builds no theme the person did not ask for.
- A silent replacement of a mockup pattern by the plan: the person's design choice would be lost without a trace.

**Consequences:** Pages show more rows per screen, and every size and color comes from one token set that a test checks. Finger targets are 4 px smaller than before; `tech-playwright.md` asserts them with touch emulation. A project that wants a roomier or brighter UI records that as its own decision. Each mockup deviation costs a decision record.

**Enforced in:** [`tech-web.md`](../../.github/skills/tech-web.md) § Visual language, § Density, § Lists and tables, § Icons and tooltips; [`tech-playwright.md`](../../.github/skills/tech-playwright.md) § Specs; [`workflow-plan.md`](../../.github/skills/workflow-plan.md) (mockup pages); [`workflow-review.md`](../../.github/skills/workflow-review.md) **UI** hunt; [`copilot-instructions.md`](../../.github/copilot-instructions.md) Section 4.8.
