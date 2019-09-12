namespace ElastiBuild
{
    public class ArtifactPackage
    {
        public string Name { get; }
        public string Location { get; }

        public ArtifactPackage(string name_, string location_)
        {
            Name = name_;
            Location = location_;
        }
    }
}
