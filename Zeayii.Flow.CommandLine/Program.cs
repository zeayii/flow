using System.CommandLine;
using System.Text.Json;
using Zeayii.Flow.CommandLine.Default;
using Zeayii.Flow.CommandLine.Localization;
using Zeayii.Flow.CommandLine.Options;
using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Extensions;

StringProvider.Configure(args);

var rootCommand = new RootCommand(Resources.GetString("RootDescription"));
var planJsonArgument = new Argument<FileInfo>("tasks-descriptor-json-file-path")
{
    Description = Resources.GetString("PlanArgumentDescription"), HelpName = "Tasks descriptor json file path",
    Arity = ArgumentArity.ExactlyOne
}.AcceptLegalFilePathsOnly().AcceptExistingOnly();

var langOption = new Option<string>("--lang", "-l")
{
    Required = false, Description = Resources.GetString("LangDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => string.Empty
};

var concurrencyOption = new Option<int>("--task-concurrency", "-tc")
{
    Required = false, Description = Resources.GetString("TaskConcurrencyDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 3
};
concurrencyOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var innerConcurrencyOption = new Option<int>("--subtask-concurrency", "-sc")
{
    Required = false, Description = Resources.GetString("SubtaskConcurrencyDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 3
};
innerConcurrencyOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var retryAttemptsOption = new Option<int>("--retry-attempts", "-ra")
{
    Required = false, Description = Resources.GetString("RetryAttemptsDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 5
};
retryAttemptsOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var blockSizeOption = new Option<int>("--block-size", "-bs")
{
    Required = false, Description = Resources.GetString("BlockSizeDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 256
};
blockSizeOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var conflictPolicyOption = new Option<ConflictPolicy>("--conflict-policy", "-cp")
{
    Required = false, Description = Resources.GetString("ConflictPolicyDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => ConflictPolicy.Resume
};

var taskFailurePolicyOption = new Option<TaskFailurePolicy>("--task-failure-policy", "-tfp")
{
    Required = false, Description = Resources.GetString("TaskFailurePolicyDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => TaskFailurePolicy.Continue
};

var dryRunOption = new Option<bool>("--dry-run", "-dr")
{
    Required = false, Description = Resources.GetString("DryRunDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => false
};

var noUiOption = new Option<bool>("--no-ui", "-nu")
{
    Required = false, Description = Resources.GetString("NoUiDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => false
};

var uiRefreshOption = new Option<int>("--ui-refresh", "-ur")
{
    Required = false, Description = Resources.GetString("UiRefreshDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 200
};
uiRefreshOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiLogMaxOption = new Option<int>("--ui-log-max", "-ulm")
{
    Required = false, Description = Resources.GetString("UiLogMaxDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 3000
};
uiLogMaxOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiLogLevelOption = new Option<LogLevel>("--ui-log-level", "-ull")
{
    Required = false, Description = Resources.GetString("UiLogLevelDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => LogLevel.Information
};

var logDirectoryOption = new Option<DirectoryInfo>("--log-directory", "-ld")
{
    Required = false, Description = Resources.GetString("LogDirectoryDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "flow"))
};

var fileLogLevelOption = new Option<LogLevel>("--file-log-level", "-fll")
{
    Required = false, Description = Resources.GetString("FileLogLevelDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => LogLevel.None
};

var uiFailMaxOption = new Option<int>("--ui-fail-max", "-ufm")
{
    Required = false, Description = Resources.GetString("UiFailMaxDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 2000
};
uiFailMaxOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiDoneMaxOption = new Option<int>("--ui-done-max", "-udm")
{
    Required = false, Description = Resources.GetString("UiDoneMaxDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 200
};
uiDoneMaxOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiTaskMaxOption = new Option<int>("--ui-task-max", "-utm")
{
    Required = false, Description = Resources.GetString("UiTaskMaxDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 50_000
};
uiTaskMaxOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiPageSizeOption = new Option<int>("--ui-page-size", "-ups")
{
    Required = false, Description = Resources.GetString("UiPageSizeDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 20
};
uiPageSizeOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiFailShowOption = new Option<int>("--ui-fail-show", "-ufs")
{
    Required = false, Description = Resources.GetString("UiFailShowDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 50
};
uiFailShowOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

var uiDoneShowOption = new Option<int>("--ui-done-show", "-uds")
{
    Required = false, Description = Resources.GetString("UiDoneShowDescription"), HelpName = "value",
    AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 50
};
uiDoneShowOption.Validators.Add(result =>
{
    if (result.GetValueOrDefault<int>() <= 0)
    {
        result.AddError($"{result.Option.Name} {Resources.GetString("ValidationGreaterThanZero")}");
    }
});

rootCommand.Arguments.Add(planJsonArgument);
rootCommand.Options.Add(langOption);
rootCommand.Options.Add(concurrencyOption);
rootCommand.Options.Add(innerConcurrencyOption);
rootCommand.Options.Add(retryAttemptsOption);
rootCommand.Options.Add(blockSizeOption);
rootCommand.Options.Add(conflictPolicyOption);
rootCommand.Options.Add(taskFailurePolicyOption);
rootCommand.Options.Add(dryRunOption);
rootCommand.Options.Add(noUiOption);
rootCommand.Options.Add(uiRefreshOption);
rootCommand.Options.Add(uiLogMaxOption);
rootCommand.Options.Add(uiLogLevelOption);
rootCommand.Options.Add(logDirectoryOption);
rootCommand.Options.Add(fileLogLevelOption);
rootCommand.Options.Add(uiFailMaxOption);
rootCommand.Options.Add(uiDoneMaxOption);
rootCommand.Options.Add(uiTaskMaxOption);
rootCommand.Options.Add(uiPageSizeOption);
rootCommand.Options.Add(uiFailShowOption);
rootCommand.Options.Add(uiDoneShowOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    try
    {
        var appOptions = OptionsBuilder.BuildApplicationOptions(
            parseResult,
            planJsonArgument,
            concurrencyOption,
            innerConcurrencyOption,
            retryAttemptsOption,
            blockSizeOption,
            taskFailurePolicyOption,
            conflictPolicyOption,
            dryRunOption,
            noUiOption,
            uiRefreshOption,
            uiLogMaxOption,
            uiLogLevelOption,
            logDirectoryOption,
            fileLogLevelOption,
            uiFailMaxOption,
            uiDoneMaxOption,
            uiTaskMaxOption,
            uiPageSizeOption,
            uiFailShowOption,
            uiDoneShowOption
        );

        if (!appOptions.PlanPath.Exists)
        {
            ConsoleOutput.WriteErrorLine($"Plan file not found: {appOptions.PlanPath.FullName}");
            return 1;
        }

        var plans = await JsonFileLoader.LoadPlanAsync(appOptions.PlanPath, cancellationToken);
        var coreOptions = OptionsBuilder.BuildCoreOptions(appOptions);
        var presentationOptions = OptionsBuilder.BuildPresentationOptions(appOptions);
        var tasks = OptionsBuilder.ToTaskRequests(appOptions, plans);

        var services = new ServiceCollection();
        services.AddSingleton(coreOptions);
        services.AddSingleton<TaskTransferEngine>();

        if (appOptions.EnableTui)
        {
            services.AddPresentation(presentationOptions);
        }
        else
        {
            services.AddSingleton<IPresentationManager, NoOpPresentationManager>();
        }

        await using var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<TaskTransferEngine>();
        var ui = provider.GetRequiredService<IPresentationManager>();
        var logSink = provider.GetService<ITuiLogSink>();

        if (appOptions.EnableTui)
        {
            await ui.StartAsync(cancellationToken);
        }

        try
        {
            if (appOptions.DryRun)
            {
                if (!appOptions.EnableTui)
                {
                    DryRunExecutor.PrintConsolePreview(tasks, appOptions);
                    return 0;
                }

                await DryRunExecutor.PopulatePreviewAsync(tasks, appOptions, ui, logSink, cancellationToken);
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return 0;
                }
            }

            return await engine.RunAsync(tasks, ui, coreOptions, cancellationToken);
        }
        finally
        {
            if (appOptions.EnableTui)
            {
                await ui.StopAsync();
            }
        }
    }
    catch (OperationCanceledException)
    {
        return 1;
    }
    catch (JsonException ex)
    {
        ConsoleOutput.WriteErrorLine($"Invalid plan file: {ex.Message}");
        return 2;
    }
    catch (Exception ex)
    {
        ConsoleOutput.WriteErrorLine(ex.Message);
        return 3;
    }
});

return await rootCommand.Parse(args).InvokeAsync();


