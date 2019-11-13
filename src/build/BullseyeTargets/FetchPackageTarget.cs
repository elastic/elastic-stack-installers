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
            var localPath = await ArtifactsApi.FetchArtifact(ctx, ap);
            await Console.Out.WriteLineAsync("Saved " + localPath);
        }
    }
}
