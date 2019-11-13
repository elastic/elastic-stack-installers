using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Infra;

namespace ElastiBuild.BullseyeTargets
{
    public class UnpackPackageTarget : BullseyeTargetBase<UnpackPackageTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            var ap = ctx.GetArtifactPackage();
            await ArtifactsApi.UnpackArtifact(ctx, ap);
        }
    }
}
