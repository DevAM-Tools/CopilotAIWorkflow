# Playwright (web UI test and debug)

Load when a web UI is in scope (Blazor, HTML, CSS, or `*.spec.ts`), when **planning** a web UI before spec files exist, or when verifying HTML illustrations. Case design and suite size: `tech-test.md`. Product look and layout: `tech-web.md`. Blazor component tests: `tech-blazor.md`. C# runner: `tech-tunit.md`.

## Role

- Browser journeys and UI debug (failed `Verify` → Playwright before redesign, Section 4.15).
- Illustrations: load / slides / canvas per `workflow-illustrate.md` only. `/review` launches the browser in **full** mode; illustrate and implement always may.
- Not for component-logic tests (bUnit or the repo’s component runner).

## Permission

Treat Playwright as a **permanent** repo tool: New Dependency Protocol. Do not `npm init` or add a stack silently. Do not wrap it in a new script without approval.

## Runner (stack-native; no `npx`)

❗ Do **not** use `npx`. Install Playwright per New Dependency Protocol, then run that stack’s CLI.

| Stack | Install (after approval) | Run tests | Install browsers |
|-------|--------------------------|-----------|------------------|
| **.NET / Blazor** | `TUnit` + `TUnit.Playwright` in a dedicated `{App}.E2E` project — not the unit `.Tests` sibling, not `Microsoft.Playwright.NUnit` / `.MSTest` / `.Xunit` | `dotnet test path/App.E2E.csproj -c Release` | After `dotnet build`: `pwsh bin/<Config>/netX/playwright.ps1 install` |
| **Python** | `playwright`, `pytest-playwright` | `pytest tests/e2e/` | `playwright install` (Python CLI) |
| **Node / TypeScript** (`*.spec.ts`) | `@playwright/test` as a **devDependency** in an approved `package.json` | `pnpm exec playwright test` · `npm exec playwright test` · or an approved `package.json` script | `pnpm exec playwright install` |
| **Rust** | No first-class Microsoft runner | Approved sidecar: Python `pytest-playwright` or Node `@playwright/test`. `playwright-rust` only if the user accepts it in the dependency protocol | That sidecar’s install command |

**.NET default:** C# UI tests are TUnit. Inherit `PageTest`. Do not add a Node Playwright suite for a .NET UI unless the user asks. Node `@playwright/test` is for Node apps, illustrations, and approved sidecars.

Do not mix unit tests and browser journeys in the same project.

## VS Code / Cursor integration

Use **Playwright Test for VS Code** (`ms-playwright.playwright`) for headed debug on **Node/TypeScript** suites only: Test Explorer, Pick Locator, Record, trace viewer, codegen. Use the workspace’s local `@playwright/test` — not `npx`.

.NET E2E: `PWDEBUG=1 dotnet test`, the IDE test runner, or .NET trace files. Do not use that extension.

## Commands

### .NET E2E (TUnit)

```bash
dotnet build path/App.E2E.csproj -c Release
pwsh bin/Release/net10.0/playwright.ps1 install
dotnet test path/App.E2E.csproj -c Release
dotnet test path/App.E2E.csproj -c Release -- --treenode-filter "/*HomePageLoads/*"
PWDEBUG=1 dotnet test path/App.E2E.csproj -c Release -- --treenode-filter "/*HomePageLoads/*"
```

```csharp
public sealed class HomePageLoads : PageTest
{
    [Test]
    public async Task ExportHeadingIsVisible()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync(url);
        await Assert.That(await Page.GetByRole(AriaRole.Heading, new() { Name = "Export" }).IsVisibleAsync()).IsTrue();
    }
}
```

MTP filter: `tech-tunit.md`. Do not pass `--filter FullyName`.

### Python

```bash
playwright install chromium
pytest tests/e2e/test_export.py -v
pytest tests/e2e/test_export.py --headed --slowmo=500
```

### Node / TypeScript (local dependency — not `npx`)

```bash
pnpm exec playwright test
pnpm exec playwright test --project=chromium e2e/export.spec.ts
pnpm exec playwright test --debug e2e/export.spec.ts
pnpm exec playwright show-trace test-results/.../trace.zip
pnpm exec playwright screenshot --viewport-size=375,667 file:///…/illustrations/slug.html
```

`npm exec` is equivalent when the repo uses npm. Use a documented `package.json` script when the repo already defines one.

Run headed debug with `--debug` or `--headed`. Trace on failure; do not sleep.

Fix common misses: wrong cwd; browsers not installed (that stack’s install command — a **machine** step, not silent `npx`); targeting Debug instead of the running app URL.

## Specs

Mechanics only. Case design and suite size: `tech-test.md`.

- One journey per spec/class when possible. Node name: `feature_scenario.spec.ts`. C#: PascalCase (`tech-tunit.md`).
- Assert what the user can see (heading, table row, error text, URL).
- **Product UI:** also run the journey at a narrow viewport and a desktop viewport unless the user waived responsiveness (`tech-web.md` / Section 4.8).
- **Illustrations:** asserts in `workflow-illustrate.md` only. Do not add extra viewports.
- Wait for locators. Do not use `waitForTimeout` as a sync strategy.
- Seed data in-process or via a documented test hook. Do not click through setup that is out of scope.

```ts
// Wrong
await page.waitForTimeout(1000);
// Right
await expect(page.getByRole("heading", { name: "Export" })).toBeVisible();
```
