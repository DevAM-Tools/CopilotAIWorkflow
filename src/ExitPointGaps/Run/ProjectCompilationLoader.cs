// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

internal static class ProjectCompilationLoader
{
    #region Public API

    public static async Task<Compilation?> CreateAsync(
        string projectPath,
        bool skipBuild,
        CancellationToken cancellationToken = default)
    {
        (Compilation? compilation, string? _) = await TryCreateAsync(
            projectPath,
            skipBuild,
            _ResolveConfiguration(null),
            cancellationToken).ConfigureAwait(false);
        return compilation;
    }

    public static async Task<Compilation?> CreateAsync(
        string projectPath,
        bool skipBuild,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        (Compilation? compilation, string? _) = await TryCreateAsync(
            projectPath,
            skipBuild,
            configuration,
            cancellationToken).ConfigureAwait(false);
        return compilation;
    }

    public static async Task<(Compilation? Compilation, string? Error)> TryCreateAsync(
        string projectPath,
        bool skipBuild,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        string fullProjectPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? ".";
        string projectName = Path.GetFileNameWithoutExtension(fullProjectPath);

        if (!skipBuild
            && !await _EnsureBuiltAsync(fullProjectPath, projectDirectory, configuration, cancellationToken).ConfigureAwait(false))
        {
            return (null, "dotnet restore or build failed.");
        }

        if (!CompilationAssetsCache.TryGetOrLoad(
                projectDirectory,
                fullProjectPath,
                configuration,
                out CompilationAssetsCache.CachedAssets? cachedAssets,
                out string? assetsError))
        {
            return (null, assetsError);
        }

        List<MetadataReference> references = _CreateReferences(cachedAssets!.ReferencePaths);
        SyntaxTree[] trees = _LoadSyntaxTrees(fullProjectPath, projectDirectory);
        if (trees.Length == 0)
        {
            return (null, "No C# source files found for project.");
        }

        return (CSharpCompilation.Create(projectName, trees, references), null);
    }

    public static string ResolveConfiguration(string? configuration)
    {
        return _ResolveConfiguration(configuration);
    }

    #endregion

    #region Private helpers

    private static async Task<bool> _EnsureBuiltAsync(
        string fullProjectPath,
        string projectDirectory,
        string configuration,
        CancellationToken cancellationToken)
    {
        DotNetProcessResult restoreResult = await DotNetProcess.RunAsync(
            Invariant($"restore \"{fullProjectPath}\""),
            projectDirectory,
            cancellationToken).ConfigureAwait(false);
        if (restoreResult.ExitCode != 0)
        {
            return false;
        }

        DotNetProcessResult buildResult = await DotNetProcess.RunAsync(
            Invariant($"build \"{fullProjectPath}\" -c {configuration} --no-restore -v:q"),
            projectDirectory,
            cancellationToken).ConfigureAwait(false);
        return buildResult.ExitCode == 0;
    }

    private static string _ResolveConfiguration(string? configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration))
        {
            return configuration;
        }

        string? environment = Environment.GetEnvironmentVariable("Configuration");
        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        return "Release";
    }

    private static SyntaxTree[] _LoadSyntaxTrees(string projectPath, string projectDirectory)
    {
        HashSet<string> sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _AddCompileItemsFromProject(projectPath, projectDirectory, sourcePaths);

        if (sourcePaths.Count == 0)
        {
            foreach (string path in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (!path.Contains(Invariant($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"), StringComparison.OrdinalIgnoreCase)
                    && !path.Contains(Invariant($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"), StringComparison.OrdinalIgnoreCase)
                    && !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
                {
                    sourcePaths.Add(path);
                }
            }
        }

        SyntaxTree[] trees = new SyntaxTree[sourcePaths.Count];
        int treeIndex = 0;
        foreach (string path in sourcePaths)
        {
            using FileStream stream = File.OpenRead(path);
            SourceText text = SourceText.From(stream, Encoding.UTF8);
            trees[treeIndex++] = CSharpSyntaxTree.ParseText(text, path: path);
        }

        return trees;
    }

    [ExcludeFromCodeCoverage]
    private static void _AddCompileItemsFromProject(string projectPath, string projectDirectory, HashSet<string> sourcePaths)
    {
        if (!ProjectXmlLoader.TryLoadDocument(projectPath, out XDocument? document, out _))
        {
            return;
        }

        foreach (XElement element in document!.Descendants())
        {
            if (!string.Equals(element.Name.LocalName, "Compile", StringComparison.Ordinal))
            {
                continue;
            }

            XAttribute? include = element.Attribute("Include");
            if (include is null || string.IsNullOrWhiteSpace(include.Value))
            {
                continue;
            }

            if (element.Attribute("Remove") is not null)
            {
                continue;
            }

            string fullPath = Path.GetFullPath(Path.Combine(projectDirectory, include.Value));
            if (File.Exists(fullPath))
            {
                sourcePaths.Add(fullPath);
            }
        }
    }

    private static List<MetadataReference> _CreateReferences(IEnumerable<string> paths)
    {
        List<MetadataReference> references = [];
        foreach (string path in paths)
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }

        if (references.Count == 0)
        {
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        }

        return references;
    }

    #endregion
}
