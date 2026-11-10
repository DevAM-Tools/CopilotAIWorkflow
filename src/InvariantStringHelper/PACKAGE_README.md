# InvariantStringHelper

Invariant-culture string interpolation for .NET 6 and later that allocates only the result string.

```xml
<PackageReference Include="InvariantStringHelper" Version="1.*" />
```

```csharp
global using static InvariantStringHelper.InvariantText;

string message = Invariant($"Rejection rate: {rate:P1}, count={count}");
```

No dependencies. `netstandard2.0` is not supported. Together with **CSharpStyleChecker** use 1.2.0 or later: 1.1.0 reports the call as CSC009.

Details: [README — InvariantStringHelper](https://github.com/DevAM/CopilotAIWorkflow/blob/main/README.md#invariantstringhelper). Changes: [CHANGELOG](https://github.com/DevAM/CopilotAIWorkflow/blob/main/src/InvariantStringHelper/CHANGELOG.md).

## License

MIT. See [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
