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
            var (wasAlreadyPresent, localPath) = await ArtifactsApi.FetchArtifact(ctx, ap, forceSwitch);

            if (wasAlreadyPresent)
                await Console.Out.WriteLineAsync("Download skipped, file exists: " + localPath);
            else
            {
                var fileSize = new FileInfo(localPath).Length;
                await Console.Out.WriteLineAsync($"Saved: ({fileSize.Bytes().Humanize("MB")}) {localPath}");
            }
        }
    }
}
