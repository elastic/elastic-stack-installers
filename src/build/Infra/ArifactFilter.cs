using System;
using ElastiBuild.Commands;
using Elastic.Installer;

namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string TargetName { get; }
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }

        public string QueryString =>
            ",windows,zip"
            + (TargetName.EndsWith(MagicStrings.Files.DashOssSuffix, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : ("," + MagicStrings.Files.DashOssSuffix))
            + (Bitness == eBitness.both
                ? string.Empty
                : (Bitness == eBitness.x86
                    ? (",-" + MagicStrings.Arch.x86_64)
                    : string.Empty))
            ;

        public ArtifactFilter(string targetName) =>
            TargetName = targetName;
    }
}
