using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class BuildInstallerTarget : BullseyeTargetBase<BuildInstallerTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var ap = ctx.GetArtifactPackage();

            var args = string.Join(' ', new string[]
            {
                "--package=" + Path.GetFileNameWithoutExtension(ap.FileName).Quote(),
                ((cmd as ISupportWxsOnlySwitch).WxsOnly ? "--wxs-only" : string.Empty),
            });

            var compilerPath = Path
                .Combine(ctx.CompilerDir, MagicStrings.Beats.CompilerName + MagicStrings.Ext.DotExe)
                .Quote();

            await SimpleExec.Command.RunAsync(compilerPath, args, ctx.InDir);
        }
    }
}
