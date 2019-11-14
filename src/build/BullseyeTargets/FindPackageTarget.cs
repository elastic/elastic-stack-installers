using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Infra;

namespace ElastiBuild.BullseyeTargets
{
    public class FindPackageTarget : BullseyeTargetBase<FindPackageTarget>
    {
        public static async Task RunAsync(IElastiBuildCommand cmd, BuildContext ctx, string target)
        {
            if (!ctx.Config.ProductNames.Any(t => t.ToLower() == target))
                throw new InvalidDataException($"Invalid product '{target}'");

            var items = await ArtifactsApi.FindArtifact(target, async filter =>
            {
                filter.ContainerId = (cmd as ISupportRequiredContainerId).ContainerId;
                filter.ShowOss = (cmd as ISupportOssSwitch).ShowOss;
                filter.Bitness = (cmd as ISupportBitnessChoice).Bitness;

                await Console.Out.WriteLineAsync(
                    $"Searching {filter.ContainerId} for {target} ...");
            });

            if (items.Count() > 1)
            {
                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items
                        .Select(itm => "  " + itm.FileName)
                    ));

                throw new Exception("More than one possibility for package. Try specifying --bitness.");
            }

            var ap = items.Single();
            ctx.UseArtifactPackage(ap);
        }
    }
}
