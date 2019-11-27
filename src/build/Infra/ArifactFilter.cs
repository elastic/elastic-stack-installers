using System;
using ElastiBuild.Commands;
using Elastic.Installer;

namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string Target { get; }
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }

        public string QueryString =>
            ",windows"
            + (Target.EndsWith("-oss", StringComparison.OrdinalIgnoreCase) ? string.Empty : ",-oss")
            + (Bitness == eBitness.both
                ? string.Empty
                : (Bitness == eBitness.x86
                    ? (",-" + MagicStrings.Arch.x86_64)
                    : string.Empty))
            ;

        public ArtifactFilter(string target) =>
            Target = target;
    }
}
