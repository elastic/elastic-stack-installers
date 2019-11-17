using System;
using System.Collections.Generic;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public sealed class ElastiBuildConfig
    {
        [YamlMember("timestamp_url")]
        public string TimestampUrl { get; set; }

        [YamlMember("products")]
        public Dictionary<string, ProductConfig> Products { get; set;  }

        public ProductConfig GetProductConfig(string targetName)
        {
            if (!Products.TryGetValue(targetName, out ProductConfig pc))
                throw new ArgumentException($"Unable to find product '{targetName}'");

            return pc;
        }

        [YamlIgnore]
        public IEnumerable<string> ProductNames => Products?.Keys;
    }
}
