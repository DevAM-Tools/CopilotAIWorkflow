# InvariantStringHelper changelog

All notable changes to the **InvariantStringHelper** NuGet library are documented here.
Repository-wide and agent-workflow notes live in the root [CHANGELOG.md](../../CHANGELOG.md).

## 1.0.0

### New

- `InvariantText.Invariant`: `Invariant($"…")` formats an interpolated string under `CultureInfo.InvariantCulture` and allocates only the result string.
- `InvariantInterpolatedStringHandler`: the compiler-built handler; wraps `DefaultInterpolatedStringHandler`, so holes keep the framework's format strings, alignment, and `ISpanFormattable` path.
- Targets `net6.0` and `net10.0`. No package dependencies. `netstandard2.0` is not supported.
- CSharpStyleChecker CSC009 accepts the call and names it in its messages, from CSharpStyleChecker 1.2.0. Version 1.1.0 reports the call as CSC009.

Usage:

```csharp
global using static InvariantStringHelper.InvariantText;

string message = Invariant($"Rejection rate: {rate:P1}, count={count}");
```
