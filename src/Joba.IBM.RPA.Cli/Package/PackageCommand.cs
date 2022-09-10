﻿namespace Joba.IBM.RPA.Cli
{
    internal partial class PackageCommand : Command
    {
        public PackageCommand() : base("package", "Manages package dependencies")
        {
            AddCommand(new InstallPackageCommand());
            AddCommand(new UpdatePackageCommand());
            AddCommand(new RestorePackageCommand());
            AddCommand(new UninstallPackageCommand());
        }
    }
}