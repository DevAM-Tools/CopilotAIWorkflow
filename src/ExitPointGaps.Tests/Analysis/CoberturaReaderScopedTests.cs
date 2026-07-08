// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps.Tests.Analysis;

/// <summary>Scoped Cobertura reader tests.</summary>
public sealed class CoberturaReaderScopedTests
{
    [Test]
    public async Task TryReadScoped_FiltersOutOfScopePackages()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1">
              <packages>
                <package name="GapSample" branch-rate="1">
                  <classes>
                    <class name="GapSample.Calculator" filename="src/GapSample/Calculator.cs" branch-rate="1">
                      <lines>
                        <line number="10" hits="1" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
                <package name="Other" branch-rate="0">
                  <classes>
                    <class name="Other.Thing" filename="src/Other/Thing.cs" branch-rate="0">
                      <lines>
                        <line number="20" hits="0" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        string path = CoberturaFixtures.WriteTemporaryFile(xml);
        BranchGapScope scope = new BranchGapScope(["GapSample"]);

        bool fullSuccess = CoberturaReader.TryRead(path, out CoberturaDocument? full, out string? _);
        bool scopedSuccess = CoberturaReader.TryReadScoped(path, scope, out CoberturaDocument? scoped, out string? _);

        await Assert.That(fullSuccess).IsTrue();
        await Assert.That(scopedSuccess).IsTrue();
        await Assert.That(full!.Lines.Count).IsEqualTo(2);
        await Assert.That(scoped!.Lines.Count).IsEqualTo(1);
        await Assert.That(scoped.Lines.ContainsKey("src/GapSample/Calculator.cs")).IsTrue();
    }
}
