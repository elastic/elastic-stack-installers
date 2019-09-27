using System;
using System.Text.RegularExpressions;

namespace ElastiBuild
{
    public class ArtifactPackage
    {
        public string TargetName { get; }
        public string Version { get; }
        public string SemVer { get; }
        public string Architecture { get; }
        public string FileName { get; }
        public string Location { get; }

        public ArtifactPackage(string name_, string location_)
        {
            FileName = name_;
            Location = location_;

            var rx = new Regex(
                @"(?<target>[^-]+)-" +
                @"(?<semver>(?<version>\d+\.\d+\.\d+)(-[^-]+)?)-" +
                @"(?<os>[^-]+)-" +
                @"(?<arch>[^\.]+)",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture);

            var rxVersion = rx.Match(FileName);

            if (rxVersion.Groups.Count != 6)
                throw new Exception("Unable to parse package file name: " + FileName);

            TargetName = rxVersion.Groups["target"].Value;
            Version = rxVersion.Groups["version"].Value;
            SemVer = rxVersion.Groups["semver"].Value;
            Architecture = rxVersion.Groups["arch"].Value;
        }
    }
}
