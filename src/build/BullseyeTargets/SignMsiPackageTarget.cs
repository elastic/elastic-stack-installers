using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;

namespace ElastiBuild.BullseyeTargets
{
    public class SignMsiPackageTarget : SignToolTargetBase<SignMsiPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var ap = ctx.GetArtifactPackage();

            var SignToolExePath = Path.Combine(
                ctx.ToolsDir,
                MagicStrings.Dirs.Cert,
                MagicStrings.Files.SignToolExe);

            bool signed = false;
            int tryCount = ctx.Config.TimestampUrls.Count;

            for (int tryNr = 0; tryNr < tryCount; ++tryNr)
            {
                var timestampUrl = ctx.Config.TimestampUrls[tryNr];
                var (certPass, SignToolArgs) = MakeSignToolArgs(ctx, timestampUrl);

                SignToolArgs += Path
                    .Combine(ctx.OutDir, ap.CanonicalTargetName,
                        Path.GetFileNameWithoutExtension(ap.FileName) + MagicStrings.Ext.DotMsi)
                    .Quote();

                await Console.Out.WriteLineAsync(SignToolExePath + " ");
                await Console.Out.WriteLineAsync(SignToolArgs.Replace(certPass, "[redacted]"));

                try
                {
                    await Command.RunAsync(SignToolExePath, SignToolArgs, noEcho: true);
                    signed = true;
                    break;
                }
                catch (Exception /*ex*/)
                {
                    await Console.Out.WriteLineAsync(
                        $"Error: timestap server {timestampUrl} is unavailable, " +
                        $"{tryCount - tryNr - 1} server(s) left to try.");
                }
            }

            if (!signed)
                throw new Exception("Error: None of the timestamp servers available.");
        }
    }
}
