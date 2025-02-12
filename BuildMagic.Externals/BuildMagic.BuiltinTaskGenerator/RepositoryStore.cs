// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.IO;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Clones the repository
/// </summary>
public class RepositoryStore
{
    private readonly ILogger<RepositoryStore> _logger;
    private readonly string _path;

    public RepositoryStore(ILogger<RepositoryStore> logger, string path)
    {
        _logger = logger;
        _path = path;
    }

    public Repository GetOrClone(string name, string url, out string path)
    {
        path = Path.Combine(_path, name);

        Directory.CreateDirectory(path);

        if (Repository.IsValid(path))
        {
            _logger.LogInformation($"{name}: {path}");
        }
        else
        {
            _logger.LogInformation($"Cloning {url} into {path}...");
            Repository.Clone(url, path, new CloneOptions
            {
                OnCheckoutProgress = (s, steps, totalSteps) =>
                {
                    _logger.ZLogInformation($"{s} ({steps} / {totalSteps})");
                },
                IsBare = false
            });
            _logger.LogInformation("Cloned.");
        }

        return new Repository(path);
    }
}
