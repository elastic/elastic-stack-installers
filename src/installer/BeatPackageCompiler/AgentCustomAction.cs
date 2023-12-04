﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.PackageCompiler.Beats
{
    public class AgentCustomAction
    {
        [CustomAction]
        public static ActionResult InstallAction(Session session)
        {
            try
            {
                // If there are no install args, we stop here
                // // (the MSI just copied the files, there will be no agent-install)
                if (string.IsNullOrEmpty(session["INSTALLARGS"]))
                {
                    session.Log("No INSTALLARGS provided, skipping agent install");
                    return ActionResult.Success;
                }

                string install_args = session["INSTALLARGS"];
                string install_folder = Path.Combine(session["INSTALLDIR"], session["exe_folder"]);

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.WorkingDirectory = install_folder;
                process.StartInfo.FileName = Path.Combine(install_folder, "elastic-agent.exe");
                process.StartInfo.Arguments = "install -f " + install_args;
                StartProcess(session, process);

                session.Log("Agent install return code:" + process.ExitCode);

                if (process.ExitCode == 0)
                {
                    // If agent got installed properly, we can go ahead and remove all the files installed by the MSI (best effort)
                    RemoveFolder(session, install_folder);
                }

                return process.ExitCode == 0 ? ActionResult.Success : ActionResult.Failure;
            }
            catch (Exception ex)
            {
                session.Log("Exception: " + ex.ToString());
                return ActionResult.Failure;
            }
        }

        private static void StartProcess(Session session, Process process)
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-8.0
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            session.Log("Running command: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            process.Start();
            session.Log(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }

        private static void RemoveFolder(Session session, string folder)
        {
            try
            {
                new DirectoryInfo(folder).Delete(true);
            }
            catch (Exception ex)
            {
                session.Log("Failed to remove folder: " + folder + ", exception: " + ex.ToString());
            }
        }

        [CustomAction]
        public static ActionResult UpgradeAction(Session session)
        {
            session.Log("Detected an agent upgrade via MSI, which is not supported. Aborting.");
            return ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult UnInstallAction(Session session)
        {
            try
            {
                string install_args = string.IsNullOrEmpty(session["INSTALLARGS"]) ? "" : session["INSTALLARGS"];
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = @"c:\\Program Files\\Elastic\\Agent\\elastic-agent.exe";
                process.StartInfo.Arguments = "uninstall -f " + install_args;
                StartProcess(session, process);

                session.Log("Agent uninstall return code:" + process.ExitCode);
            }
            catch (Exception ex)
            {
                // IMPORTANT! Uninstall will be done as best effort..
                // We don't want to fail the MSI uninstall in case there is an issue with the agent uninstall command.
                session.Log(ex.ToString());
            }

            return ActionResult.Success;
        }
    }
}
