using System;
using System.Collections.Generic;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public sealed class ElastiBuildConfig
    {
        [YamlMember("timestamp_urls", SerializeMemberMode.Content)]
        public List<string> TimestampUrls { get; }

        [YamlMember("products")]
        public Dictionary<string, ProductConfig> Products { get; set; }

        [YamlIgnore]
        public IEnumerable<string> ProductNames => Products?.Keys;

        public ElastiBuildConfig()
        {
            TimestampUrls = new List<string>();
        }

        public ProductConfig GetProductConfig(string targetName)
        {
            if (!Products.TryGetValue(targetName, out ProductConfig pc))
                throw new ArgumentException($"Unable to find product '{targetName}'");

            return pc;
        }
    }
}
