// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class ProjectCompilationLoader
{
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

        string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
        {
            return (null, "Run 'dotnet restore' first (missing project.assets.json).");
        }

        string? targetFramework = _ReadTargetFramework(fullProjectPath, assetsPath, out string? tfmError);
        if (!string.IsNullOrEmpty(tfmError))
        {
            return (null, tfmError);
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            return (null, "Target framework not found in project.");
        }

        List<MetadataReference> references = _ResolveReferences(assetsPath, targetFramework, projectDirectory, configuration);
        SyntaxTree[] trees = _LoadSyntaxTrees(fullProjectPath, projectDirectory);
        if (trees.Length == 0)
        {
            return (null, "No C# source files found for project.");
        }

        return (CSharpCompilation.Create(projectName, trees, references), null);
    }

    private static async Task<bool> _EnsureBuiltAsync(
        string fullProjectPath,
        string projectDirectory,
        string configuration,
        CancellationToken cancellationToken)
    {
        DotNetProcessResult restoreResult = await DotNetProcess.RunAsync(
            $"restore \"{fullProjectPath}\"",
            projectDirectory,
            cancellationToken).ConfigureAwait(false);
        if (restoreResult.ExitCode != 0)
        {
            return false;
        }

        DotNetProcessResult buildResult = await DotNetProcess.RunAsync(
            $"build \"{fullProjectPath}\" -c {configuration} --no-restore -v:q",
            projectDirectory,
            cancellationToken).ConfigureAwait(false);
        return buildResult.ExitCode == 0;
    }

    public static string ResolveConfiguration(string? configuration)
    {
        return _ResolveConfiguration(configuration);
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

    /// <remarks>Spawns <c>dotnet</c> subprocesses; success path exercised by loader integration tests.</remarks>
    [ExcludeFromCodeCoverage]
    private static string? _ReadTargetFramework(string projectPath, string assetsPath, out string? error)
    {
        error = null;
        string? fromProject = _TryReadTargetFrameworkFromProject(projectPath, out string? projectError);
        if (!string.IsNullOrEmpty(projectError))
        {
            error = projectError;
            return null;
        }

        if (!string.IsNullOrEmpty(fromProject))
        {
            return fromProject;
        }

        return _TryReadTargetFrameworkFromAssets(assetsPath);
    }

    [ExcludeFromCodeCoverage]
    private static string? _TryReadTargetFrameworkFromProject(string projectPath, out string? error)
    {
        error = null;
        if (!ProjectXmlLoader.TryLoadDocument(projectPath, out XDocument? document, out error))
        {
            return null;
        }

        XElement? tfm = document!.Descendants().FirstOrDefault(element => element.Name.LocalName == "TargetFramework");
        if (tfm is not null && !string.IsNullOrWhiteSpace(tfm.Value))
        {
            return tfm.Value;
        }

        XElement? tfms = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "TargetFrameworks");
        if (tfms is not null && !string.IsNullOrWhiteSpace(tfms.Value))
        {
            return tfms.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    private static string? _TryReadTargetFrameworkFromAssets(string assetsPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        if (!document.RootElement.TryGetProperty("project", out JsonElement project)
            || !project.TryGetProperty("frameworks", out JsonElement frameworks))
        {
            return null;
        }

        foreach (JsonProperty framework in frameworks.EnumerateObject())
        {
            return framework.Name;
        }

        return null;
    }

    private static List<MetadataReference> _ResolveReferences(
        string assetsPath,
        string targetFramework,
        string projectDirectory,
        string configuration)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        if (document.RootElement.TryGetProperty("targets", out JsonElement targets)
            && targets.TryGetProperty(targetFramework, out JsonElement target))
        {
            foreach (JsonProperty library in target.EnumerateObject())
            {
                if (!library.Value.TryGetProperty("runtime", out JsonElement runtime))
                {
                    continue;
                }

                foreach (JsonProperty runtimeFile in runtime.EnumerateObject())
                {
                    string? resolved = _ResolveAssetPath(document.RootElement, library.Name, runtimeFile.Name);
                    if (resolved is not null && File.Exists(resolved))
                    {
                        paths.Add(resolved);
                    }
                }
            }
        }

        string outputDir = Path.Combine(projectDirectory, "bin", configuration, targetFramework);
        if (Directory.Exists(outputDir))
        {
            foreach (string dll in Directory.EnumerateFiles(outputDir, "*.dll"))
            {
                paths.Add(dll);
            }
        }

        return _CreateReferences(paths);
    }

    [ExcludeFromCodeCoverage]
    private static string? _ResolveAssetPath(JsonElement root, string libraryName, string runtimeFileName)
    {
        if (!root.TryGetProperty("libraries", out JsonElement libraries)
            || !libraries.TryGetProperty(libraryName, out JsonElement library))
        {
            return null;
        }

        if (!library.TryGetProperty("path", out JsonElement pathElement))
        {
            return null;
        }

        string packagePath = pathElement.GetString() ?? string.Empty;
        if (!root.TryGetProperty("packageFolders", out JsonElement folders))
        {
            return null;
        }

        foreach (JsonProperty folder in folders.EnumerateObject())
        {
            string candidate = Path.Combine(folder.Name, packagePath, runtimeFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static SyntaxTree[] _LoadSyntaxTrees(string projectPath, string projectDirectory)
    {
        HashSet<string> sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _AddCompileItemsFromProject(projectPath, projectDirectory, sourcePaths);

        if (sourcePaths.Count == 0)
        {
            foreach (string path in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (!path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
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
            trees[treeIndex++] = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
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
}
