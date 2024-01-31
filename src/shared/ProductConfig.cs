using System.Collections.Generic;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public sealed class ProductConfig
    {
        [YamlMember("description")]
        public string Description { get; set; } = "(add 'description' field to config.yaml)";

        [YamlMember("published_name")]
        public string PublishedName { get; set; } = "Elastic Beats";

        [YamlMember("published_url")]
        public string PublishedUrl { get; set; } = "https://www.elastic.co/products/beats";

        [YamlMember("published_binaries", SerializeMemberMode.Content)]
        public List<string> PublishedBinaries { get; }

        [YamlMember("mutable_dirs", SerializeMemberMode.Content)]
        public List<string> MutableDirs { get; }

        [YamlMember("service")]
        public bool IsWindowsService { get; set; } = true;

        public ProductConfig()
        {
            PublishedBinaries = new List<string>();
            MutableDirs = new List<string>();
        }
    }
}
