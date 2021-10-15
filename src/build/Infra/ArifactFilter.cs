using System;
using ElastiBuild.Commands;
using Elastic.Installer;

namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string TargetName { get; }
        public string ContainerId { get; set; }

        public string QueryString =>
            ",windows,zip,x86_64"
            + (TargetName.EndsWith(MagicStrings.Files.DashOssSuffix, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : ("," + MagicStrings.Files.DashOssSuffix))
            ;

        public ArtifactFilter(string targetName) =>
            TargetName = targetName;
    }
}
