// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace CoverageGap.Tool;

internal static class ProjectCompilationLoader
{
    public static Task<Compilation?> CreateAsync(
        string projectPath,
        bool skipBuild,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return CreateAsync(projectPath, skipBuild, _ResolveConfiguration(null));
    }

    public static Task<Compilation?> CreateAsync(
        string projectPath,
        bool skipBuild,
        string configuration)
    {
        TryCreate(projectPath, skipBuild, configuration, out Compilation? compilation, out string? _);
        return Task.FromResult(compilation);
    }

    public static bool TryCreate(
        string projectPath,
        bool skipBuild,
        string configuration,
        out Compilation? compilation,
        out string? error)
    {
        compilation = null;
        error = null;

        string fullProjectPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? ".";
        string projectName = Path.GetFileNameWithoutExtension(fullProjectPath);

        if (!skipBuild
            && !_EnsureBuilt(fullProjectPath, projectDirectory, configuration, out error))
        {
            return false;
        }

        string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
        {
            error = "Run 'dotnet restore' first (missing project.assets.json).";
            return false;
        }

        string? targetFramework = _ReadTargetFramework(fullProjectPath, assetsPath, out string? tfmError);
        if (!string.IsNullOrEmpty(tfmError))
        {
            error = tfmError;
            return false;
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            error = "Target framework not found in project.";
            return false;
        }

        List<MetadataReference> references = _ResolveReferences(assetsPath, targetFramework, projectDirectory, configuration);
        SyntaxTree[] trees = _LoadSyntaxTrees(fullProjectPath, projectDirectory);
        if (trees.Length == 0)
        {
            error = "No C# source files found for project.";
            return false;
        }

        compilation = CSharpCompilation.Create(projectName, trees, references);
        return true;
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
    private static bool _EnsureBuilt(
        string fullProjectPath,
        string projectDirectory,
        string configuration,
        out string? error)
    {
        error = null;
        int restoreCode = _RunDotNet($"restore \"{fullProjectPath}\"", projectDirectory, out string? restoreError);
        if (restoreCode != 0)
        {
            error = restoreError ?? "dotnet restore failed.";
            return false;
        }

        int buildCode = _RunDotNet(
            $"build \"{fullProjectPath}\" -c {configuration} --no-restore -v:q",
            projectDirectory,
            out string? buildError);
        if (buildCode != 0)
        {
            error = buildError ?? "dotnet build failed.";
            return false;
        }

        return true;
    }

    [ExcludeFromCodeCoverage]
    private static int _RunDotNet(string arguments, string workingDirectory, out string? error)
    {
        error = null;
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)!;
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        string stderr = stderrTask.GetAwaiter().GetResult();
        stdoutTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
        {
            error = stderr.Trim();
        }

        return process.ExitCode;
    }

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
        try
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using FileStream stream = File.OpenRead(projectPath);
            using XmlReader reader = XmlReader.Create(stream, settings);
            XDocument document = XDocument.Load(reader);
            XElement? tfm = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "TargetFramework");
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
        catch (Exception ex) when (ex is XmlException or IOException)
        {
            error = $"Failed to read project file: {ex.Message}";
            return null;
        }
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
        try
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using FileStream stream = File.OpenRead(projectPath);
            using XmlReader reader = XmlReader.Create(stream, settings);
            XDocument document = XDocument.Load(reader);

            foreach (XElement element in document.Descendants())
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
        catch (XmlException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static List<MetadataReference> _CreateReferences(IEnumerable<string> paths)
    {
        List<MetadataReference> references = new List<MetadataReference>();
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
