﻿namespace Joba.IBM.RPA.Cli
{
    class EnvironmentCommand : Command
    {
        public EnvironmentCommand() : base("env", "Configures an environment")
        {
            var alias = new Argument<string>("alias", "The environment name");
            var region = new Option<string>("--region", "The region you want to connect");
            var userName = new Option<string>("--userName", "The user name, usually the e-mail, to use for authentication");
            var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication");

            AddArgument(alias);
            AddOption(region);
            AddOption(userName);
            AddOption(tenant);

            this.SetHandler(HandleAsync,
                new OptionsBinder(alias, region, userName, tenant),
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(Options options, Project project, InvocationContext context) =>
            await HandleAsync(options, project, context.GetCancellationToken());

        public async Task HandleAsync(Options options, Project project, CancellationToken cancellation)
        {
            using var regionSelector = new RegionSelector();
            var region = await regionSelector.SelectAsync(options.RegionName, cancellation);

            using var client = region.CreateClient();
            var accountSelector = new AccountSelector(client.Account);
            var account = await accountSelector.SelectAsync(options.UserName, options.TenantCode, cancellation);
            var session = await account.AuthenticateAsync(client.Account, cancellation);

            project.ConfigureEnvironmentAndSwitch(options.Alias, region, account, session);
            await project.SaveAsync(cancellation);

            ExtendedConsole.WriteLine($"Hi {session.PersonName:blue}, the environment {project.CurrentEnvironment.Alias:blue} has been configured:");
            ExtendedConsole.WriteLine($"Region {region.Name:blue}, Tenant {account.TenantCode:blue} - {session.TenantName:blue}");
            ExtendedConsole.WriteLine($"Use the {Constants.CliName:blue} {Name:blue} to configure more environments");
        }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public record struct Options(string Alias, string? RegionName = null, string? UserName = null, int? TenantCode = null);
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        class OptionsBinder : BinderBase<Options>
        {
            private readonly Argument<string> aliasArgument;
            private readonly Option<string> regionOption;
            private readonly Option<string> userNameOption;
            private readonly Option<int?> tenantCodeOption;

            public OptionsBinder(Argument<string> aliasArgument, Option<string> regionOption, Option<string> userNameOption, Option<int?> tenantCodeOption)
            {
                this.aliasArgument = aliasArgument;
                this.regionOption = regionOption;
                this.userNameOption = userNameOption;
                this.tenantCodeOption = tenantCodeOption;
            }

            protected override Options GetBoundValue(BindingContext bindingContext)
            {
                return new Options(
                    bindingContext.ParseResult.GetValueForArgument(aliasArgument),
                    bindingContext.ParseResult.GetValueForOption(regionOption),
                    bindingContext.ParseResult.GetValueForOption(userNameOption),
                    bindingContext.ParseResult.GetValueForOption(tenantCodeOption));
            }
        }
    }
}