using System.Diagnostics;

namespace ElastiBuild.Infra
{
    public class ArtifactContainer
    {
        public string Name { get; }
        public bool IsAlias { get; }
        public bool IsBranch { get; }
        public bool IsVersion { get; }

        internal ArtifactContainer(string name, bool isBranch = false, bool isVersion = false, bool isAlias = false)
        {
            Debug.Assert(isBranch || isVersion || isAlias,
                $"Must specify one of {nameof(isBranch)}, {nameof(isVersion)}, {nameof(isAlias)}, ");

            Name = name;
            IsBranch = isBranch;
            IsVersion = isVersion;
            IsAlias = isAlias;
        }
    }
}