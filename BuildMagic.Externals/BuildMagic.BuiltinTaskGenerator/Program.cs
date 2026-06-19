// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.IO;
using BuildMagic.BuiltinTaskGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

// Cache locations can be overridden via environment variables so that CI can persist them
// (e.g. commit the analysis library into the repository, or cache the UnityCsReference clone).
var repositoryStorePath = Environment.GetEnvironmentVariable("BUILDMAGIC_REPO_PATH") is { Length: > 0 } repoPath
    ? repoPath
    : Path.Combine(Path.GetTempPath(), "BuildMagic.BuiltinTaskGenerator");

var libraryPath = Environment.GetEnvironmentVariable("BUILDMAGIC_LIBRARY_PATH") is { Length: > 0 } libPath
    ? libPath
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BuildMagic.BuiltinTaskGenerator", "library");

var builder = ConsoleApp.CreateBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettings>(context.Configuration);
        services.AddSingleton<UnityApiAnalyzer>();
        services.AddSingleton<RepositoryStore>(provider =>
            new RepositoryStore(provider.GetService<ILogger<RepositoryStore>>()!, repositoryStorePath));
        services.AddSingleton<UnityCsReferenceRepository>();
        services.AddSingleton<AnalysisLibrary>(provider =>
            new AnalysisLibrary(provider.GetService<ILogger<AnalysisLibrary>>()!, libraryPath));
    })
    .ConfigureLogging((context, loggingBuilder) =>
    {
        loggingBuilder.ClearProviders()
            .SetMinimumLevel(LogLevel.Trace)
            .AddZLoggerConsole();
    });

var app = builder.Build();
app.AddCommands<App>();
app.Run();
