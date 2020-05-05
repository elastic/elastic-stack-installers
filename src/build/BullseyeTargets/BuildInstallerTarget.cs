using System;
using System.Threading.Tasks;

namespace ElastiBuild.BullseyeTargets
{
    public class BuildInstallerTarget : BullseyeTargetBase<BuildInstallerTarget>
    {
        public static Task RunAsync(BuildContext ctx)
        {
            Console.WriteLine("Build Done");
            return Task.CompletedTask;
        }
    }
}
