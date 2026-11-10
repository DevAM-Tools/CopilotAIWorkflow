# Decision — Invariant interpolation goes through the InvariantStringHelper package

**Status:** accepted

**Context:** Every interpolated string needs the invariant culture (rule CSC009). `FormattableString.Invariant` allocates a `FormattableString` and an `object[]` and boxes value arguments before the result string is built. `string.Create(CultureInfo.InvariantCulture, $"…")` and `StringBuilder.Append(CultureInfo.InvariantCulture, $"…")` bind an interpolated string handler and allocate only the result, but they are long at every call site (Section 4.4 of [`copilot-instructions.md`](../../.github/copilot-instructions.md)). The handler overloads exist from .NET 6; `netstandard2.0` has none. Roslyn analyzers and generators must stay on `netstandard2.0` and cannot take a run-time dependency.

**Decision:** On .NET 6 and later, write `Invariant($"…")` from the InvariantStringHelper package: a wrapper around `DefaultInterpolatedStringHandler` created with the invariant culture; the result string is the only allocation. CSC009 accepts it, `string.Create`, and `StringBuilder.Append` with the invariant culture, and reports `FormattableString.Invariant` wherever `string.Create` with a handler binds. `netstandard2.0` projects keep `FormattableString.Invariant`.

**Rejected:**
- `string.Create(CultureInfo.InvariantCulture, $"…")` as the one form — correct but long at every call site.
- Handler source in the agent instructions — costs every agent's context for work that never touches it.
- `global using static System.Globalization.CultureInfo;` with `string.Create(InvariantCulture, …)` — shortens only a part and leaves two idioms.
- A helper that takes a `string` — the compiler builds the string with the current culture before the helper runs.
- `FormattableString.Invariant` everywhere — allocates three objects per call and boxes arguments.
- `netstandard2.0` support now — no `DefaultInterpolatedStringHandler` there; an own handler would allocate per formatted value. The public API is shaped so it can be added later.
- Bundling the helper into CSharpStyleChecker — that package is a development dependency without run-time assemblies.

**Consequences:** Projects on .NET 6 and later reference one small package and import one `using static`. A multi-targeting project that includes `netstandard2.0` needs `#if NET6_0_OR_GREATER` around the package call or `StringBuilder.Append`. The analyzer finds the helper by metadata name, so renaming the type, the method, or the handler changes the analyzer and its pin test.

**Enforced in:** `InvariantCultureFormattingAnalyzer` in [`src/CSharpStyleChecker/Analyzers/InvariantCultureFormattingAnalyzer.cs`](../../src/CSharpStyleChecker/Analyzers/InvariantCultureFormattingAnalyzer.cs); `InvariantText` in [`src/InvariantStringHelper/InvariantText.cs`](../../src/InvariantStringHelper/InvariantText.cs); rule text in [`tech-csharp.md`](../../.github/skills/tech-csharp.md) § Style.
