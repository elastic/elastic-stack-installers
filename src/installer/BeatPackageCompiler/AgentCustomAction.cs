using System;
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
                    return ActionResult.Success;

                string install_args = session["INSTALLARGS"];
                string install_folder = Path.Combine(session["INSTALLDIR"], session["exe_folder"]);
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.WorkingDirectory = install_folder;
                process.StartInfo.FileName = "elastic-agent.exe";
                process.StartInfo.Arguments = "install -f " + install_args;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                session.Log("Agent install return code:" + process.ExitCode);

                if (process.ExitCode == 0)
                {
                    // If agent got installed properly, we can go ahead and remove all the files installed by the MSI (best effort)
                    new DirectoryInfo(install_folder).Delete(true);
                }

                return process.ExitCode == 0 ? ActionResult.Success : ActionResult.Failure;
            }
            catch
            {
                return ActionResult.Failure;
            }
        }

        [CustomAction]
        public static ActionResult UnInstallAction(Session session)
        {
            try
            {
                // If there are no (un)install args, we stop here
                if (string.IsNullOrEmpty(session["INSTALLARGS"]))
                    return ActionResult.Success;

                string install_args = session["INSTALLARGS"];
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = @"c:\\Program Files\\Elastic\\Agent\\elastic-agent.exe";
                process.StartInfo.Arguments = "uninstall -f " + install_args;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

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
