using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class CompileMsiTarget : BullseyeTargetBase<CompileMsiTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var cmd = ctx.GetCommand();
            var ap = ctx.GetArtifactPackage();

            var args = string.Join(' ', new string[]
            {
                "--package=" + Path.GetFileNameWithoutExtension(ap.FileName).Quote(),
                ((cmd as ISupportWxsOnlySwitch).WxsOnly ? "--wxs-only" : string.Empty),
                "--keep-temp-files",
            });

            var compilerPath = Path
                .Combine(ctx.CompilerDir, MagicStrings.Beats.CompilerName + MagicStrings.Ext.DotExe)
                .Quote();

            await SimpleExec.Command.RunAsync(compilerPath, args, ctx.InDir);
        }
    }
}
