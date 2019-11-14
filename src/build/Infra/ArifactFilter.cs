using ElastiBuild.Commands;
using Elastic.Installer;

namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string ContainerId { get; set; }
        public bool ShowOss { get; set; }
        public eBitness Bitness { get; set; }

        public string QueryString =>
            ",windows"
            + (ShowOss ? string.Empty : ",-oss")
            + (Bitness == eBitness.both
                ? string.Empty
                : (Bitness == eBitness.x86
                    ? (",-" + MagicStrings.Arch.x86_64)
                    : string.Empty))
            ;
    }
}
