using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;

namespace ElastiBuild.BullseyeTargets
{
    public class SignProductBinariesTarget : SignToolTargetBase<SignProductBinariesTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var SignToolExePath = Path.Combine(ctx.ToolsDir, MagicStrings.Files.SignToolExe);
            var (certPass, SignToolArgs) = MakeSignToolArgs(ctx, target);

            var ap = ctx.GetArtifactPackage();

            foreach (var binary in ctx.Config.GetProductConfig(target).PublishedBinaries)
            {
                var FullSignToolArgs = SignToolArgs + Path
                    .Combine(ctx.InDir, Path.GetFileNameWithoutExtension(ap.FileName), binary)
                    .Quote();

                await Console.Out.WriteAsync(SignToolExePath + " ");
                await Console.Out.WriteLineAsync(FullSignToolArgs.Replace(certPass, "[redacted]"));
                await Command.RunAsync(SignToolExePath, FullSignToolArgs, noEcho: true);
            }
        }
    }
}
