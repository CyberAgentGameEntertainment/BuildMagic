// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace BuildMagic.BuiltinTaskGenerator;

public class UnityCsReferenceRepository : IDisposable
{
    private readonly ILogger<UnityCsReferenceRepository> _logger;
    private readonly string _name;
    private readonly string _path;
    private readonly Repository _repo;

    public UnityCsReferenceRepository(ILogger<UnityCsReferenceRepository> logger, RepositoryStore store)
    {
        var name = "UnityCsReference";
        var url = "https://github.com/Unity-Technologies/UnityCsReference.git";
        _logger = logger;
        _name = name;
        _repo = store.GetOrClone(name, url, out _path);
        _logger.LogInformation($"UnityCsReference: {_path}");
    }

    public string Path => _path;

    #region IDisposable Members

    public void Dispose()
    {
        _repo.Dispose();
    }

    #endregion

    public IEnumerable<UnityVersion> GetVersions()
    {
        foreach (var tag in _repo.Tags)
            if (UnityVersion.TryParse(tag.FriendlyName, out var version))
                yield return version;
    }

    public void Checkout(UnityVersion version)
    {
        _logger.LogInformation($"Checking out version {version} for {_name}...");
        var canonicalName = $"refs/tags/{version}";
        Commands.Checkout(_repo, canonicalName, new CheckoutOptions
        {
            CheckoutModifiers = CheckoutModifiers.Force
        });
    }

    public void UpdateSubmodule(string name)
    {
        _logger.ZLogInformation($"updating submodule: {name}");
        _repo.Submodules.Update(name, new SubmoduleUpdateOptions
        {
            Init = true,
            FetchOptions =
            {
                OnProgress = message =>
                {
                    _logger.ZLogInformation($"{message}");
                    return true;
                }
            },
            OnCheckoutProgress = (path, steps, totalSteps) =>
            {
                _logger.ZLogInformation($"{path} (step {steps} / {totalSteps})");
            }
        });
    }

    public void Fetch()
    {
        var options = new FetchOptions();
        options.TagFetchMode = TagFetchMode.Auto;

        options.OnProgress += message =>
        {
            _logger.LogInformation(message);
            return true;
        };

        var remote = _repo.Network.Remotes["origin"];
        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);

        _logger.LogInformation($"Fetching {_name}...");

        Commands.Fetch(_repo, remote.Name, refSpecs, options, "Fetch");

        _logger.LogInformation("Fetched.");
    }
}
