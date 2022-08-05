﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace Joba.IBM.RPA
{
    static class Program
    {
        public static Task<int> Main(string[] args)
        {
            //https://www.youtube.com/watch?v=JNDgcBDZPkU
            //https://docs.microsoft.com/en-us/dotnet/standard/commandline/
            //example: https://github.com/dotnet/command-line-api/issues/1776#issuecomment-1165482490
            //bind class: https://github.com/dotnet/command-line-api/issues/1750#issuecomment-1152707726

            Directory.CreateDirectory(Constants.LocalFolder);

            var command = new RootCommand("");
            command.AddCommand(new ConfigureCommand());
            command.SetHandler(ShowHelp);

            var parser = new CommandLineBuilder(command)
                .UseDefaults()
                .AddMiddleware(Middleware)
                //.UseExceptionHandler(OnException)
                .Build();

            return parser.InvokeAsync(args);
        }

        private static void OnException(Exception exception, InvocationContext context)
        {
            //TODO:
        }

        private static void ShowHelp(InvocationContext context)
        {
            using var output = context.Console.Out.CreateTextWriter();
            var helpContext = new HelpContext(context.HelpBuilder,
                                              context.ParseResult.CommandResult.Command,
                                              output,
                                              context.ParseResult);

            context.HelpBuilder.Write(helpContext);
        }

        private static async Task Middleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            if (context.ParseResult.CommandResult != context.ParseResult.RootCommandResult)
            {
                await LoadProfileAsync(context);
            }

            await next(context);
        }

        private static async Task LoadProfileAsync(InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            //ServerConfig server;
            //var serverFile = new FileInfo(Constants.ServerFilePath);
            //if (serverFile.Exists)
            //    server = JsonSerializer.Deserialize<ServerConfig>(await File.ReadAllTextAsync(serverFile.FullName, cancellation), Constants.SerializerOptions);
            

            //context.BindingContext.AddService(s => server);
        }
    }
}