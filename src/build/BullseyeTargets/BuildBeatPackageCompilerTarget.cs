using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class BuildBeatPackageCompilerTarget : BullseyeTargetBase<BuildBeatPackageCompilerTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var compilerSrcDir = Path
                .Combine(
                    ctx.SrcDir,
                    "installer",
                    MagicStrings.Beats.CompilerName)
                .Quote();

            await SimpleExec.Command.RunAsync(
                "dotnet",
                $"build {compilerSrcDir} --configuration Release --output {ctx.CompilerDir.Quote()}");
        }
    }
}
