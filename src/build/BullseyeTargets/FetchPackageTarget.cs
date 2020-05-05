using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using ElastiBuild.Infra;
using Humanizer;

namespace ElastiBuild.BullseyeTargets
{
    public class FetchPackageTarget : BullseyeTargetBase<FetchPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var cmd = ctx.GetCommand();
            bool forceSwitch = (cmd as ISupportForceSwitch)?.ForceSwitch ?? false;

            var ap = ctx.GetArtifactPackage();

            long reportThreshold = 0;

            var (wasAlreadyPresent, localPath) =
                await ArtifactsApi.FetchArtifact(
                    ctx, ap, forceSwitch,
                    (bytesRead, bytesReadTotal) =>
                    {
                        reportThreshold += bytesRead;

                        // Throttle reporting
                        if (reportThreshold >= (1024 * 1024 * 5))
                        {
                            reportThreshold = 0;
                            Console.Out.WriteAsync(".");
                        }
                    },
                    bytesReadTotal =>
                    {
                        Console.Out.WriteLineAsync();
                    });

            if (wasAlreadyPresent)
                Console.WriteLine("Download skipped, file exists: " + localPath);
            else
            {
                var fileSize = new FileInfo(localPath).Length;
                Console.WriteLine($"Saved: ({fileSize.Bytes().Humanize("MB")}) {localPath}");
            }
        }
    }
}
