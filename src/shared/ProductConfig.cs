using System;
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

        [YamlMember("mutable_dirs", SerializeMemberMode.Content)]
        public List<string> MutableDirs { get; }

        [YamlMember("upgrade_code")]
        public Guid UpgradeCode { get; set; }

        [YamlMember("service")]
        public bool IsWindowsService { get; set; } = true;

        public ProductConfig()
        {
            MutableDirs = new List<string>();
        }
    }
}
