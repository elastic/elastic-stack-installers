using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class FetchPackageTarget : BullseyeTargetBase<FetchPackageTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var ap = ctx.GetArtifactPackage();
            var (wasAlreadyPresent, localPath) = await ArtifactsApi.FetchArtifact(ctx, ap);

            if (wasAlreadyPresent)
                await Console.Out.WriteLineAsync("Download skipped, file exists: " + localPath);
            else
                await Console.Out.WriteLineAsync("Saved: " + localPath);
        }
    }
}
