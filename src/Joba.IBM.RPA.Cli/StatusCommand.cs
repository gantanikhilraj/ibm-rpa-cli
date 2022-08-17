﻿namespace Joba.IBM.RPA.Cli
{
    class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Inspects the status of the project and files in the current environment")
        {
            var fileName = new Argument<string>("fileName", () => string.Empty, "The specific wal file name");

            AddArgument(fileName);
            this.SetHandler(Handle, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private void Handle(string fileName, Project project, InvocationContext context) =>
           Handle(fileName, project);

        public void Handle(Project project) => Handle(string.Empty, project);

        public void Handle(string fileName, Project project)
        {
            ExtendedConsole.WriteLine($"Project {project.Name:blue}, on environment {project.CurrentEnvironment.Alias:blue}");

            if (!string.IsNullOrEmpty(fileName))
            {
                var wal = project.GetFile(fileName);
                if (wal == null)
                    throw new Exception($"The file '{fileName}' does not exist");

                var renderer = new WalFileRenderer();
                renderer.Render(wal);
            }
            else
            {
                var renderer = new WalFileRenderer();
                foreach (var wal in project.GetFiles())
                    renderer.Render(wal);
            }
        }
    }

    class WalFileRenderer
    {
        public void Render(WalFile wal)
        {
            var color = wal.Version.HasValue ? ConsoleColor.Green : ConsoleColor.Red;
            var version = wal.Version.HasValue ? wal.Version.Value.ToString("D5") : "local";
            using (ExtendedConsole.BeginForegroundColor(color))
            {
                ExtendedConsole.WriteLineIndented($"{version} {wal.Info.Name}");
            }
        }
    }
}