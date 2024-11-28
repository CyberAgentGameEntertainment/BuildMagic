// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.IO;
using BuildMagic.BuiltinTaskGenerator;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

MSBuildLocator.RegisterDefaults();

var builder = ConsoleApp.CreateBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppSettings>(context.Configuration);
        services.AddSingleton<UnityApiAnalyzer>();
        services.AddSingleton<RepositoryStore>(provider =>
            new RepositoryStore(provider.GetService<ILogger<RepositoryStore>>()!,
                Path.Combine(Path.GetTempPath(), "BuildMagic.BuiltinTaskGenerator")));
        services.AddSingleton<UnityCsReferenceRepository>();
        services.AddSingleton<AnalysisLibrary>();
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
