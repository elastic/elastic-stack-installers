using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Installer;

namespace QA.Core
{
    public class MSIInstaller
    {
        public readonly string ProductName;
        public readonly ProductConfig ProductConfig;
        public readonly string InstallerPath;
        public readonly string LogsPath;
        public bool RunElevated = true;

        public MSIInstaller(string productName, ProductConfig productConfig, string installerPath, string logsPath)
        {
            this.ProductName = productName;
            this.ProductConfig = productConfig;
            this.InstallerPath = installerPath;
            this.LogsPath = logsPath;
        }

        private Process Run(List<String> args)
        {
            Process process = new Process();
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.Arguments = String.Join(" ", args);

            if (RunElevated)
                process.StartInfo.Verb = "runas";

            process.Start();
            return process;
        }

        public Process Install(bool quiet = true, bool waitForCompletion = true, int timeoutSeconds = 60)
        {
            List<String> args = new List<String>() {
                $"/i {InstallerPath}",
                $"/l!*vx {LogsPath}\\{ProductName}-install.log"
            };

            if (quiet)
                args.Add("/quiet");

            Process process = Run(args);

            if (waitForCompletion)
                process.WaitForExit(timeoutSeconds * 1000);

            return process;
        }

        public Process Uninstall(bool quiet = true, bool waitForCompletion = true, int timeoutSeconds = 60)
        {
            List<String> args = new List<String>() {
                $"/x {InstallerPath}",
                $"/l!*vx {LogsPath}\\{ProductName}-uninstall.log"
            };

            if (quiet)
                args.Add("/quiet");

            Process process = Run(args);

            if (waitForCompletion)
                process.WaitForExit(timeoutSeconds * 1000);

            return process;
        }
    }
}
