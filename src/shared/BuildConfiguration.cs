using System.IO;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public class BuildConfiguration
    {
        public static ElastiBuildConfig Read(string fileName)
        {
            using var yamlConfig = File.OpenRead(fileName);
            var config = new Serializer().Deserialize<ElastiBuildConfig>(yamlConfig);
            return config;
        }
    }
}
