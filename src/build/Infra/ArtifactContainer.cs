using System.Diagnostics;

namespace ElastiBuild
{
    public class ArtifactContainer
    {
        public string Name { get; }
        public bool IsAlias { get; }
        public bool IsBranch { get; }
        public bool IsVersion { get; }

        internal ArtifactContainer(string name_, bool isBranch_ = false, bool isVersion_ = false, bool isAlias_ = false)
        {
            Debug.Assert(isBranch_ || isVersion_ || isAlias_, 
                $"Must specify one of {nameof(isBranch_)}, {nameof(isVersion_)}, {nameof(isAlias_)}, ");

            Name = name_;
            IsBranch = isBranch_;
            IsVersion = isVersion_;
            IsAlias = isAlias_;
        }
    }
}
