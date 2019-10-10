using System;
using System.Text.RegularExpressions;

namespace Elastic.Installer
{
    public class ArtifactPackage
    {
        public string TargetName { get; }
        public string Version { get; }
        public string SemVer { get; }
        public string Architecture { get; }
        public string FileName { get; }
        public string Location { get; }

        public bool IsDownloadable => 
            !string.IsNullOrWhiteSpace(Location);

        public bool Is32bit => Architecture == MagicStrings.Arch.x86;
        public bool Is64Bit => Architecture == MagicStrings.Arch.x86_64;

        public ArtifactPackage(string name_)
            : this(name_, null)
        { }

        public ArtifactPackage(string fileName_, string location_)
        {
            FileName = fileName_;
            Location = location_;

            var rxVersion = rx.Match(FileName);
            if (rxVersion.Groups.Count != 6)
                throw new Exception("Unable to parse package file name: " + FileName);

            TargetName = rxVersion.Groups["target"].Value?.ToLower();
            Version = rxVersion.Groups["version"].Value?.ToLower();
            SemVer = rxVersion.Groups["semver"].Value?.ToLower();
            Architecture = rxVersion.Groups["arch"].Value?.ToLower();
        }

        static readonly Regex rx = new Regex(
                @"(?<target>[^-]+)-" +
                @"(?<semver>(?<version>\d+\.\d+\.\d+)(-[^-]+)?)-" +
                @"(?<os>[^-]+)-" +
                @"(?<arch>[^\.]+)",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}
