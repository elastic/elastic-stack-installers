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
                    // The MSI install cache is left behind when installation fails and must be removed manually
                    string installDir = session["INSTALLDIR"].TrimEnd('\\');
                    string beatsRoot = Path.GetDirectoryName(Path.GetDirectoryName(installDir));
                    RemoveFolder(session, beatsRoot);

                    // The agent binary is left behind when installation fails and must be removed manually
                    RemoveFile(session, @"C:\Program Files\Elastic\Agent\elastic-agent.exe");

                    // The agent's managed uninstall key is left behind when installation fails and must be removed manually
                    // TODO(samuevl): remove when https://github.com/elastic/elastic-agent/pull/13705 is released
                    RemoveManagedUninstallKey(session);
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

        private static void RemoveFile(Session session, string file)
        {
            try
            {
                bool existedBefore = File.Exists(file);
                session.Log("RemoveFile: File.Exists(" + file + ") = " + existedBefore);
                File.Delete(file);
                bool existsAfter = File.Exists(file);
                session.Log("RemoveFile: after delete, File.Exists = " + existsAfter);
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

        private static void RemoveManagedUninstallKey(Session session)
        {
            try
            {
                const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Elastic Agent";
                using (var existing = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    session.Log("RemoveManagedUninstallKey: key exists before delete = " + (existing != null));
                }
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                using (var afterDelete = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    session.Log("RemoveManagedUninstallKey: key exists after delete = " + (afterDelete != null));
                }
            }
            catch (Exception ex)
            {
                session.Log("Failed to remove agent-managed uninstall registry key: " + ex.ToString());
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
