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
        public static ActionResult MyAction(Session session)
        {
            string install_args = !string.IsNullOrEmpty(session["INSTALLARGS"]) ? session["INSTALLARGS"] : "";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WorkingDirectory = Path.Combine(session["INSTALLDIR"], session["exe_folder"]);
            process.StartInfo.FileName = "elastic-agent.exe";
            process.StartInfo.Arguments = "install -f " + install_args;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            return ActionResult.Success;
        }
    }
}
