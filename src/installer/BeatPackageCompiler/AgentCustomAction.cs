using System;
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
                string install_args = string.Empty;

                if (!string.IsNullOrEmpty(session["INSTALLARGS"]))
                    install_args = session["INSTALLARGS"];
                else
                    session.Log("No INSTALLARGS detected");

                string install_folder = Path.Combine(session["INSTALLDIR"], session["exe_folder"]);

                System.Diagnostics.Process process = new System.Diagnostics.Process();
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
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            session.Log("Running command: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments);
            process.Start();
            session.Log("stderr of the process:");
            session.Log(process.StandardError.ReadToEnd());
            process.WaitForExit();
        }

        private static void RemoveFolder(Session session, string folder)
        {
            try
            {
                new DirectoryInfo(folder).Delete(true);
                session.Log("Successfully removed foler: " + folder);
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
                string binary_path = @"c:\\Program Files\\Elastic\\Agent\\elastic-agent.exe";
                if (!File.Exists(binary_path))
                {
                    session.Log("Cannot find file: " + binary_path + ", skipping uninstall action");
                    return ActionResult.Success;
                }

                string install_args = string.IsNullOrEmpty(session["INSTALLARGS"]) ? "" : session["INSTALLARGS"];
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = binary_path;
                process.StartInfo.Arguments = "uninstall -f " + install_args;
                StartProcess(session, process);

                session.Log("Agent uninstall return code:" + process.ExitCode);
                return process.ExitCode == 0 ? ActionResult.Success : ActionResult.Failure;
            }
            catch (Exception ex)
            {
                session.Log(ex.ToString());
                return ActionResult.Failure;
            }
        }
    }
}
