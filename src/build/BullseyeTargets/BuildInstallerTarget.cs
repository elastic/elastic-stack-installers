using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class BuildInstallerTarget : BullseyeTargetBase<BuildInstallerTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            await Console.Out.WriteLineAsync("Build Done");
        }
    }
}
