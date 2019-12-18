using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ElastiBuild.Commands;
using ElastiBuild.Extensions;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.BullseyeTargets
{
    public class FindPackageTarget : BullseyeTargetBase<FindPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx, string targetName)
        {
            var cmd = ctx.GetCommand();
            bool forceSwitch = (cmd as ISupportForceSwitch)?.ForceSwitch ?? false;

            if (!forceSwitch && !ctx.Config.ProductNames.Any(t => t.ToLower() == targetName))
                throw new InvalidDataException($"Invalid product '{targetName}'");

            var bitness = (cmd as ISupportBitnessChoice).Bitness;
            var containerId = (cmd as ISupportRequiredContainerId).ContainerId;

            if (!QualifiedVersion.FromString(containerId, out var qv))
                throw new InvalidDataException($"Bad container id: '{containerId}'");

            Directory.CreateDirectory(ctx.InDir);

            if (!forceSwitch)
                await Console.Out.WriteLineAsync($"Searching local directory {ctx.InDir} ...");

            var packageList = forceSwitch

                // When --force in effect, we always lookup from artifacts-api
                ? new List<ArtifactPackage>()

                // Lookup in the bin/in first
                : new DirectoryInfo(ctx.InDir)
                    .GetFiles(MagicStrings.Files.AllDotZip, SearchOption.TopDirectoryOnly)
                    .Select(fi =>
                    {
                        if (!ArtifactPackage.FromFilename(fi.Name, out var ap))
                            return null;

                        if (ap.TargetName != targetName)
                            return null;

                        if (ap.SemVer != qv.SemVer)
                            return null;

                        if (!fi.Name.Contains("-windows", StringComparison.OrdinalIgnoreCase))
                            return null;

                        return ap;
                    })
                    .Where(ap =>
                    {
                        return (ap != null) && bitness switch
                        {
                            eBitness.x86 => ap.Is32Bit,
                            eBitness.x64 => ap.Is64Bit,
                            eBitness.both => true,
                            _ => false
                        };
                    })
                    .ToList();

            var actionPrefix = "<BUGBUG> ";

            ArtifactPackage ap = null;

            if (packageList.Count == 0)
            {
                // No local packages found, try Artifacts API
                await Console.Out.WriteLineAsync($"Searching Artifacts API for {targetName}-{containerId} ...");

                packageList = (await ArtifactsApi.FindArtifact(targetName, filter =>
                {
                    filter.ContainerId = containerId;
                    filter.Bitness = bitness;
                })).ToList();

                if (packageList.Count == 0)
                {
                    await Console.Out.WriteLineAsync("ERR: Nothing found");

                    // TODO: should we skip or stop the build?
                    throw new Exception();
                }
                else
                    actionPrefix = "Will fetch file: ";
            }
            else
                actionPrefix = "Using local file: ";

            if (packageList.Count > 1)
            {
                await Console.Out.WriteLineAsync("WARN: More than one possibility for product:");
                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    packageList
                        .Select(itm => "  " + itm.FileName)
                    ));
            }

            ap = packageList.First();

            if (ap == null)
                throw new Exception("BUGBUG: ap = null");

            ctx.SetArtifactPackage(ap);

            await Console.Out.WriteLineAsync(actionPrefix + ap.FileName);
            return;
        }
    }
}
