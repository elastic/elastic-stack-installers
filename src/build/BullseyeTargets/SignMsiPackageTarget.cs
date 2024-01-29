using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;
using Elastic.PackageCompiler;

namespace ElastiBuild.BullseyeTargets
{
    public class SignMsiPackageTarget : SignToolTargetBase<SignMsiPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var ap = ctx.GetArtifactPackage();

            // This package name should be aligned with the "out" directory created by
            // the BeatPackageCompiler module (as it is the one generating the MSIs to be signed)
            string shortPackageName = CmdLineOptions.MakePackageNameShort(ap.CanonicalTargetName);

            string filePath = Path.Combine(
                 ctx.OutDir,
                 shortPackageName,
                 Path.GetFileNameWithoutExtension(ap.FileName) + MagicStrings.Ext.DotMsi
            );

            await Command.RunAsync("icacls", ctx.OutDir + " /grant Users:(OI)(CI)F /T", noEcho: false);
            Console.WriteLine("Access control set on " + ctx.OutDir);

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

                SignToolArgs += filePath.Quote();

                Console.WriteLine(SignToolExePath + " ");
                Console.WriteLine(SignToolArgs.Replace(certPass, "[redacted]"));

                try
                {
                    await Command.RunAsync(SignToolExePath, SignToolArgs, noEcho: true);
                    signed = true;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Error: SigTool failed, check it's output: {ex.Message}\n" +
                        $"{tryCount - tryNr - 1} server(s) left to try.");
                }
            }

            if (!signed)
                throw new Exception("Error: Failed to sign msi after all retries.");
        }
    }
}
