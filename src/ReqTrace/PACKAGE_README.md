# ReqTrace

Dotnet tool that checks that every requirement reaches a test that ran and passed. It reads the requirements and test cards in `spec/`, finds the card markers in your C#, Python, and Rust tests, compares everything with the state a person accepted in `spec/trace.lock`, and reads test results from TRX files. Only new or changed entries fail; old gaps are counted. Part of [CopilotAIWorkflow](https://github.com/DevAM/CopilotAIWorkflow).

```bash
dotnet new tool-manifest
dotnet tool install ReqTrace --version 0.*
dotnet tool run reqtrace check --results artifacts/trace-results
```

Exit codes: `0` no error · `1` at least one error, each with file, line, and fix · `2` usage or unreadable input.

| Command | Use it |
|---------|--------|
| `reqtrace check [--results <file-or-folder>] [--json]` | After every change to the spec or to tests |
| `reqtrace show <ID>` | Before changing a requirement or test: what depends on it |
| `reqtrace report` | At a gate: paste its output |
| `reqtrace accept [--allow-gap "<reason>"]` | Only after a person approved the state |
| `reqtrace confirm <TEST-ID> --note "<what was checked>"` | After checking that a card still proves a changed requirement |
| `reqtrace syntax [requirement\|card\|marker\|ignore]` | After a form finding, or before the first entry: prints the form the tool reads, also outside a repository |

Test markers, one per test:

```csharp
[Test, Property("Card", "TEST-EXPORT-CREATE")]
```

```python
@pytest.mark.card("TEST-EXPORT-CREATE")
```

```rust
// card: TEST-EXPORT-CREATE
```

Spec format: `reqtrace syntax`, and in full [tech-spec.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/.github/skills/tech-spec.md). Setup and use in agent workflows: [tech-reqtrace.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/.github/skills/tech-reqtrace.md). Reference: [docs/agents.md](https://github.com/DevAM/CopilotAIWorkflow/blob/main/docs/agents.md). Changes: [CHANGELOG](https://github.com/DevAM/CopilotAIWorkflow/blob/main/src/ReqTrace/CHANGELOG.md).

## Requirements

.NET 10 SDK. Verified on Windows x64 and Linux x64; macOS and ARM64 are not verified yet.

## License

MIT. See [LICENSE](https://github.com/DevAM/CopilotAIWorkflow/blob/main/LICENSE).
