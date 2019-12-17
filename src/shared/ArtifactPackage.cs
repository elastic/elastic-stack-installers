using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ElastiBuild.Extensions;

namespace Elastic.Installer
{
    public class ArtifactPackage
    {
        public string TargetName { get; }
        public string Version { get; }
        public string Qualifier { get; }
        public string Snapshot => IsSnapshot ? MagicStrings.Ver.Snapshot : string.Empty;
        public string Architecture { get; }
        public string FileName { get; }
        public string Url { get; }

        public string SemVer => string.Join("-", new[]
        {
            Version, Qualifier, Snapshot
        }.Where(str => !str.IsEmpty()));

        public bool IsSnapshot { get; private set; }
        public bool IsOss { get; private set; }
        public string CanonicalTargetName { get; private set; }

        public bool Is32Bit => Architecture == MagicStrings.Arch.x86;
        public bool Is64Bit => Architecture == MagicStrings.Arch.x86_64;
        public bool IsDownloadable => !Url.IsEmpty();
        public bool IsQualified => !Qualifier.IsEmpty();

        public static bool FromUrl(string url, out ArtifactPackage artifactPackage) =>
            FromFilenameOrUrl(Path.GetFileName(url), url, out artifactPackage);

        public static bool FromFilename(string fileName, out ArtifactPackage artifactPackage) =>
            FromFilenameOrUrl(fileName, null, out artifactPackage);

        static readonly Regex rx = new Regex(
            /* 0 full capture, 7 groups total */
            /* 1 */ @$"(?<target>[^-]+({MagicStrings.Files.DashOssSuffix})?)" +
            /* 2 */ @$"-(?<version>\d+\.\d+\.\d+)" +
            /* 3 */ @$"(-(?<qualifier>(?!\b(?:{MagicStrings.Ver.Snapshot})\b)[^-]+))?" +
            /* 4 */ @$"(-(?<snapshot>{MagicStrings.Ver.Snapshot}))?" +
            /* 5 */ @$"-(?<os>[^-]+)" +
            /* 6 */ @$"-(?<arch>[^\.]+)",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        static bool FromFilenameOrUrl(string fileName, string url, out ArtifactPackage artifactPackage)
        {
            artifactPackage = null;

            var rxVersion = rx.Match(fileName);
            if (rxVersion.Groups.Count < 7)
                return false;

            artifactPackage = new ArtifactPackage(rxVersion.Groups, fileName, url);
            return true;
        }

        ArtifactPackage(GroupCollection rxGroups, string fileName, string url)
        {
            FileName = fileName;
            Url = url;

            TargetName = rxGroups["target"].Value.ToLower();
            Version = rxGroups["version"].Value.ToLower();
            Qualifier = rxGroups["qualifier"].Value.ToLower();
            Architecture = rxGroups["arch"].Value.ToLower();
            IsSnapshot = !rxGroups["snapshot"].Value.IsEmpty();

            IsOss = TargetName.EndsWith(
                MagicStrings.Files.DashOssSuffix,
                StringComparison.OrdinalIgnoreCase);

            CanonicalTargetName = IsOss
                ? TargetName.Substring(0, TargetName.Length - MagicStrings.Files.DashOssSuffix.Length)
                : TargetName;
        }
    }
}
