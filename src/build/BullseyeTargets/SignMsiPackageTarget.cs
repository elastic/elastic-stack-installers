using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;

namespace ElastiBuild.BullseyeTargets
{
    public class SignMsiPackageTarget : SignToolTargetBase<SignMsiPackageTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var SignToolExePath = Path.Combine(
                ctx.ToolsDir,
                MagicStrings.Dirs.Cert,
                MagicStrings.Files.SignToolExe);

            var (certPass, SignToolArgs) = MakeSignToolArgs(ctx, target);

            var ap = ctx.GetArtifactPackage();

            SignToolArgs += Path
                .Combine(ctx.OutDir, target,
                    Path.GetFileNameWithoutExtension(ap.FileName) + MagicStrings.Ext.DotMsi)
                .Quote();

            await Console.Out.WriteLineAsync(SignToolExePath + " ");
            await Console.Out.WriteLineAsync(SignToolArgs.Replace(certPass, "[redacted]"));
            await Command.RunAsync(SignToolExePath, SignToolArgs, noEcho: true);
        }
    }
}
