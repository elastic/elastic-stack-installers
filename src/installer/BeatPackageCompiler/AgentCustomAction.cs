using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

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

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = Path.Combine(session["INSTALLDIR"], "elastic-agent.exe");
                process.StartInfo.Arguments = "install -f " + install_args;
                StartProcess(session, process);

                session.Log("Agent install return code:" + process.ExitCode);

                if (process.ExitCode == 0)
                {
                    // If agent got installed properly, we can go ahead and remove all the files installed by the MSI (best effort)
                    RemoveFolder(session, session["INSTALLDIR"]);

                    // The agent handles its own lifecycle and should not expose a standard MSI uninstall entry
                    // in the Windows registry
                    RemoveMSIUninstallKey(session);
                }
                else
                {
                    // The agent binary is left behind when installation fails and must be removed manually
                    RemoveFile(session, @"C:\Program Files\Elastic\Agent\elastic-agent.exe");

                    // The data folder is left behind when installation fails and must be removed manually
                    RemoveFolder(session, Path.Combine(session["INSTALLDIR"], "data"));
                }

                return process.ExitCode == 0 ? ActionResult.Success : ActionResult.Failure;
            }
            catch (Exception ex)
            {
                session.Log("Exception: " + ex.ToString());
                RemoveFolder(session, Path.Combine(session["INSTALLDIR"], "data"));
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

        private static void RemoveFile(Session session, string file)
        {
            try
            {
                File.Delete(file);
                session.Log("Successfully removed file: " + file);
            }
            catch (Exception ex)
            {
                session.Log("Failed to remove file: " + file + ", exception: " + ex.ToString());
            }
        }

        private static void RemoveMSIUninstallKey(Session session)
        {
            try
            {
                string productCode = session["ProductCode"];
                string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + productCode;
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                session.Log("Removed ARP registry key: HKLM\\" + keyPath);
            }
            catch (Exception ex)
            {
                session.Log("Failed to remove ARP registry key: " + ex.ToString());
            }
        }

        [CustomAction]
        public static ActionResult UpgradeAction(Session session)
        {
            // FindRelatedProducts also finds advertised products. RemoveExistingProducts removes them, so only an installed one blocks the install.
            foreach (var code in session["WIX_UPGRADE_DETECTED"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (new ProductInstallation(code).IsInstalled)
                {
                    session.Log("Detected an agent upgrade via MSI, which is not supported. Aborting.");
                    return ActionResult.Failure;
                }
                session.Log("Related product " + code + " is advertised, not installed. RemoveExistingProducts removes it.");
            }
            return ActionResult.Success;
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
