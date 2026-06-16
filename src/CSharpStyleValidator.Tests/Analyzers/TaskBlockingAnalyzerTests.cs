// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

using System.Threading.Tasks;
using CSharpStyleValidator.Analyzers;
using CSharpStyleValidator.Diagnostics;
using CSharpStyleValidator.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;

namespace CSharpStyleValidator.Tests.Analyzers;

/// <summary>Tests for <see cref="TaskBlockingAnalyzer"/>.</summary>
public sealed class TaskBlockingAnalyzerTests
{
    private const string Usings = "using System;\nusing System.Threading.Tasks;\n";

    [Test]
    public async Task TaskBlocking_Await_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public async Task M()
                {
                    await Task.Delay(1);
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_Wait_ReportsCsv004()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M()
                {
                    Task.Delay(1).{|#0:Wait|}();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TaskBlocking).WithLocation(0));
    }

    [Test]
    public async Task TaskBlocking_Result_ReportsCsv004()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public int M()
                {
                    return Task.FromResult(1).{|#0:Result|};
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TaskBlocking).WithLocation(0));
    }

    [Test]
    public async Task TaskBlocking_NonTaskWait_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private sealed class Holder
                {
                    public void Wait() { }
                }

                public void M()
                {
                    new Holder().Wait();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_WaitOnNonMemberInvocation_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                public void M(System.Action wait)
                {
                    wait();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_ResultOnNonTaskType_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private sealed class Holder
                {
                    public int Result { get; set; }
                }

                public int M()
                {
                    return new Holder().Result;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_ForeignTaskTypeResult_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private sealed class ForeignTask
                {
                    public int Result { get; set; }
                }

                public int M()
                {
                    return new ForeignTask().Result;
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_InstanceWaitMethod_NoDiagnostic()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private void Wait()
                {
                }

                public void M()
                {
                    Wait();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(source);
    }

    [Test]
    public async Task TaskBlocking_WaitOnTaskField_ReportsCsv004()
    {
        const string source = Usings + """
            namespace N;
            public class C
            {
                private readonly Task _Task = Task.CompletedTask;

                public void M()
                {
                    _Task.Wait();
                }
            }
            """;

        await AnalyzerVerifier.VerifyAsync<TaskBlockingAnalyzer>(
            source,
            new DiagnosticResult(DiagnosticDescriptors.TaskBlocking).WithSpan(10, 15, 10, 19));
    }
}
