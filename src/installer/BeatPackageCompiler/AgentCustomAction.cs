using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.PackageCompiler.Beats
{
    public class AgentCustomAction
    {
        [CustomAction]
        public static ActionResult InstallAction(Session session)
        {
            try
            {
                string install_args = !string.IsNullOrEmpty(session["INSTALLARGS"]) ? session["INSTALLARGS"] : "";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.WorkingDirectory = Path.Combine(session["INSTALLDIR"], session["exe_folder"]);
                process.StartInfo.FileName = "elastic-agent.exe";
                process.StartInfo.Arguments = "install -f " + install_args;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                session.Log("Agent install return code:" + process.ExitCode);

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
                string install_args = !string.IsNullOrEmpty(session["INSTALLARGS"]) ? session["INSTALLARGS"] : "";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = @"c:\\Program Files\\Elastic\\Agent\\elastic-agent.exe";
                process.StartInfo.Arguments = "uninstall -f " + install_args;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                session.Log("Agent uninstall return code:" + process.ExitCode);
            } catch (Exception ex) {
                // IMPORTANT! Uninstall will be done as best effort..
                // We don't want to fail the MSI uninstall in case there is an issue with the agent uninstall command.
                session.Log(ex.ToString());
            }

            return ActionResult.Success;
        }
    }
}
