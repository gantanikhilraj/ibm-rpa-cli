﻿using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand
    {
        //[RequiresProject]
        //internal class DeployCommand : Command
        //{
        //    public const string CommandName = "deploy";
        //    public DeployCommand() : base(CommandName, "Deploys the project to an environment.")
        //    {
        //        var target = new Argument<string>("target", "The target environment name to deploy the project.");
        //        var source = new Option<string?>("--source", "Specifies the source environment. If this is not specified, the current environment will be used.");

        //        AddArgument(target);
        //        AddOption(source);
        //        this.SetHandler(HandleAsync, source, target,
        //           Bind.FromLogger<DeployCommand>(),
        //           Bind.FromServiceProvider<IRpaClientFactory>(),
        //           Bind.FromServiceProvider<Project2>(),
        //           Bind.FromServiceProvider<InvocationContext>());
        //    }

        //    private async Task HandleAsync(string? sourceAlias, string targetAlias, ILogger<DeployCommand> logger, IRpaClientFactory clientFactory,
        //        Project2 project, InvocationContext context)
        //    {
        //        var cancellation = context.GetCancellationToken();
        //        var service = new DeployService(logger, clientFactory, project);
        //        await service.DeployAsync(sourceAlias, targetAlias, cancellation);
        //    }
        //}
    }
}