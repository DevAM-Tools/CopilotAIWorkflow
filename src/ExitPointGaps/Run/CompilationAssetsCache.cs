// Copyright © 2026 DevAM. Licensed under the MIT License. See LICENSE in the repository root.

namespace ExitPointGaps;

/// <summary>Caches parsed <c>project.assets.json</c> reference paths per project invocation.</summary>
internal static class CompilationAssetsCache
{
    private static readonly ConcurrentDictionary<AssetsCacheKey, CachedAssets> _Cache = new();

    /// <summary>Loads or returns cached compilation assets for a project.</summary>
    /// <param name="projectDirectory">Project directory.</param>
    /// <param name="projectPath">Absolute project path.</param>
    /// <param name="configuration">Build configuration.</param>
    /// <param name="assets">Cached assets when successful.</param>
    /// <param name="error">Error message when loading fails.</param>
    /// <returns><see langword="true"/> when assets are available.</returns>
    public static bool TryGetOrLoad(
        string projectDirectory,
        string projectPath,
        string configuration,
        out CachedAssets? assets,
        out string? error)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentException.ThrowIfNullOrEmpty(projectPath);
        ArgumentException.ThrowIfNullOrEmpty(configuration);

        assets = null;
        error = null;

        string assetsPath = Path.Combine(projectDirectory, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
        {
            error = "Run 'dotnet restore' first (missing project.assets.json).";
            return false;
        }

        string? targetFramework = _ReadTargetFramework(projectPath, assetsPath, out error);
        if (!string.IsNullOrEmpty(error))
        {
            return false;
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            error = "Target framework not found in project.";
            return false;
        }

        AssetsCacheKey key = new AssetsCacheKey(projectDirectory, targetFramework, configuration);
        CachedAssets cached = _Cache.GetOrAdd(
            key,
            static (cacheKey, state) => _LoadAssets(state.AssetsPath, cacheKey),
            new LoadState(assetsPath));
        assets = cached;
        return true;
    }

    private static CachedAssets _LoadAssets(string assetsPath, AssetsCacheKey key)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(assetsPath));
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (document.RootElement.TryGetProperty("targets", out JsonElement targets)
            && targets.TryGetProperty(key.TargetFramework, out JsonElement target))
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

        string projectDirectory = key.ProjectDirectory;
        string outputDir = Path.Combine(projectDirectory, "bin", key.Configuration, key.TargetFramework);
        if (Directory.Exists(outputDir))
        {
            foreach (string dll in Directory.EnumerateFiles(outputDir, "*.dll"))
            {
                paths.Add(dll);
            }
        }

        List<string> referencePaths = paths.OrderBy(static path => path, StringComparer.OrdinalIgnoreCase).ToList();
        return new CachedAssets(key.TargetFramework, referencePaths);
    }

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

    private readonly record struct AssetsCacheKey(string ProjectDirectory, string TargetFramework, string Configuration);

    private readonly record struct LoadState(string AssetsPath);

    /// <summary>Immutable cached reference paths for one project configuration.</summary>
    /// <param name="TargetFramework">Resolved target framework moniker.</param>
    /// <param name="ReferencePaths">Absolute metadata reference paths.</param>
    internal sealed record CachedAssets(string TargetFramework, IReadOnlyList<string> ReferencePaths);
}
