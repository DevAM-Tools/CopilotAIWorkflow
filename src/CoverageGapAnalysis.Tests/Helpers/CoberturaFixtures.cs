// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGapAnalysis.Tests.Helpers;

internal static class CoberturaFixtures
{
    internal const string PartialBranchXml =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage branch-rate="0.5" line-rate="0.5" version="1.9">
          <packages>
            <package name="Sample" branch-rate="0.5" line-rate="0.5">
              <classes>
                <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="0.5" line-rate="0.5">
                  <methods>
                    <method name="Bar" signature="()">
                      <lines>
                        <line number="10" hits="1" branch="True" condition-coverage="50% (1/2)">
                          <conditions>
                            <condition number="0" type="jump" coverage="50%" />
                            <condition number="1" type="jump" coverage="100%" />
                          </conditions>
                        </line>
                      </lines>
                    </method>
                  </methods>
                  <lines>
                    <line number="10" hits="1" branch="True" condition-coverage="50% (1/2)">
                      <conditions>
                        <condition number="0" type="jump" coverage="50%" />
                        <condition number="1" type="jump" coverage="100%" />
                      </conditions>
                    </line>
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    internal const string FullCoverageXml =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage branch-rate="1" line-rate="1" version="1.9">
          <packages>
            <package name="Sample" branch-rate="1" line-rate="1">
              <classes>
                <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                  <lines>
                    <line number="10" hits="3" branch="True" condition-coverage="100% (2/2)">
                      <conditions>
                        <condition number="0" type="jump" coverage="100%" />
                      </conditions>
                    </line>
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    internal const string ExitGapXml =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage branch-rate="1" line-rate="1" version="1.9">
          <packages>
            <package name="Sample" branch-rate="1" line-rate="1">
              <classes>
                <class name="Sample.Foo" filename="C:\repo\src\Sample\Foo.cs" branch-rate="1" line-rate="1">
                  <lines>
                    <line number="20" hits="0" branch="False" />
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    internal static string WriteTemporaryFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cobertura-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, content);
        return path;
    }
}
