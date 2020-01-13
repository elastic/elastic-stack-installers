using System.Linq;
using System.Text.RegularExpressions;
using ElastiBuild.Extensions;

namespace Elastic.Installer
{
    public class QualifiedVersion
    {
        public string Version { get; }
        public string Qualifier { get; }
        public string Snapshot => IsSnapshot ? MagicStrings.Ver.Snapshot : string.Empty;

        public string SemVer => string.Join("-", new[]
        {
            Version, Qualifier, Snapshot
        }.Where(str => !str.IsEmpty()));

        public bool IsSnapshot { get; private set; }
        public bool IsQualified => !Qualifier.IsEmpty();

        public static bool FromString(string value, out QualifiedVersion containerProperties)
        {
            containerProperties = null;

            var rxVersion = rx.Match(value);
            if (rxVersion.Groups.Count < 4)
                return false;

            containerProperties = new QualifiedVersion(rxVersion.Groups);
            return true;
        }

        static readonly Regex rx = new Regex(
            /* 0 full capture, 4 groups total */
            /* 1 */ @$"(?<version>\d+\.\d+(\.\d+)?)" +
            /* 2 */ @$"(-(?<qualifier>(?!\b(?:{MagicStrings.Ver.Snapshot})\b)[^-]+))?" +
            /* 3 */ @$"(-(?<snapshot>{MagicStrings.Ver.Snapshot}))?",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        QualifiedVersion(GroupCollection rxGroups)
        {
            Version = rxGroups["version"].Value.ToLower();
            Qualifier = rxGroups["qualifier"].Value.ToLower();
            IsSnapshot = !rxGroups["snapshot"].Value.IsEmpty();
        }
    }
}
