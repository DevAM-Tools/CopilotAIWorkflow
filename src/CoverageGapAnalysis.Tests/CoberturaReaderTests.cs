// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests;

/// <summary>Tests for <see cref="CoberturaReader"/>.</summary>
public sealed class CoberturaReaderTests
{
    [Test]
    public async Task TryRead_ValidXml_ReturnsDocument()
    {
        string path = CoberturaFixtures.WriteTemporaryFile(CoberturaFixtures.PartialBranchXml);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(document).IsNotNull();
        await Assert.That(document!.PackageBranchRates["Sample"]).IsEqualTo(0.5d);
    }

    [Test]
    public async Task TryRead_MissingFile_ReturnsError()
    {
        bool success = CoberturaReader.TryRead("missing.cobertura.xml", out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task TryRead_EmptyPath_ReturnsError()
    {
        bool success = CoberturaReader.TryRead(string.Empty, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task TryRead_MalformedXml_ReturnsError()
    {
        string path = CoberturaFixtures.WriteTemporaryFile("<coverage");

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsFalse();
        await Assert.That(document).IsNull();
        await Assert.That(error).IsNotNull();
    }

    [Test]
    public async Task TryRead_DuplicateLineEntries_KeepsHighestHitCount()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage branch-rate="1">
              <packages>
                <package name="Sample" branch-rate="1">
                  <classes>
                    <class name="Sample.Type" filename="/src/Sample.cs">
                      <methods>
                        <method name="M" signature="()">
                          <lines>
                            <line number="10" hits="1" branch="False" />
                          </lines>
                        </method>
                      </methods>
                      <lines>
                        <line number="10" hits="0" branch="False" />
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;

        string path = CoberturaFixtures.WriteTemporaryFile(xml);

        bool success = CoberturaReader.TryRead(path, out CoberturaDocument? document, out string? error);

        await Assert.That(success).IsTrue();
        await Assert.That(error).IsNull();
        await Assert.That(document!.Lines["/src/Sample.cs"][10].Hits).IsEqualTo(1);
    }
}
