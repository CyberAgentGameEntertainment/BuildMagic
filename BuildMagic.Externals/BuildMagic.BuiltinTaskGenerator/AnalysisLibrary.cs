// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Manages the cache of the result of analysis
/// </summary>
public class AnalysisLibrary
{
    private readonly ILogger<AnalysisLibrary> _logger;

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BuildMagic.BuiltinTaskGenerator", "library");

    private readonly Dictionary<UnityVersion, AnalysisResult> _results = new();

    public AnalysisLibrary(ILogger<AnalysisLibrary> logger)
    {
        _logger = logger;
        _logger.ZLogInformation($"Using library path: {_path}");
        Directory.CreateDirectory(_path);
    }

    public async IAsyncEnumerable<(UnityVersion, AnalysisResult)> LoadAllAsync(Func<UnityVersion, bool> versionFilter,
        CancellationToken ct)
    {
        Directory.CreateDirectory(_path);
        var files = Directory.GetFiles(_path);

        var versions = files.Select(f =>
                UnityVersion.TryParse(Path.GetFileNameWithoutExtension(f), out var version)
                    ? (UnityVersion?)version
                    : null)
            .WhereNotNull().Order();

        foreach (var version in versions.Where(versionFilter))
        {
            var (result, isNew) = await GetForVersionAsync(version, false, ct);
            yield return (version, result);
        }
    }

    public async Task<(AnalysisResult result, bool isNew)> GetForVersionAsync(UnityVersion version, bool forceCreate,
        CancellationToken ct)
    {
        AnalysisResult? result;
        var isNew = false;
        if (!_results.TryGetValue(version, out result))
        {
            var path = GetPath(version);

            if (!forceCreate && File.Exists(path)) result = await LoadAsync(path, ct);

            if (result == null)
            {
                result = new AnalysisResult();
                isNew = true;
            }

            _results[version] = result;
        }

        return (result, isNew);
    }

    public async Task SaveAsync(UnityVersion version, CancellationToken ct)
    {
        await SaveAsync(version, _results[version], ct);
    }

    private async Task SaveAsync(UnityVersion version, AnalysisResult data, CancellationToken ct)
    {
        await using var fs = File.Open(GetPath(version), FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(fs, data, cancellationToken: ct);
    }

    private async Task<AnalysisResult?> LoadAsync(string path, CancellationToken ct)
    {
        await using var fs = File.Open(path, FileMode.Open, FileAccess.Read);
        var result = await JsonSerializer.DeserializeAsync<AnalysisResult>(fs, cancellationToken: ct);
        if (result?.SerializedVersion < AnalysisResult.CurrentSerializedVersion) return null;
        return result;
    }

    private string GetPath(UnityVersion version)
    {
        return Path.Combine(_path, $"{version}.json");
    }
}
