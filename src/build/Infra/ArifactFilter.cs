namespace ElastiBuild.Infra
{
    public class ArtifactFilter
    {
        public string TargetName { get; }
        public string ContainerId { get; set; }

        public string QueryString =>
            ",windows,zip,-oss,x86_64";

        public ArtifactFilter(string targetName) =>
            TargetName = targetName;
    }
}
