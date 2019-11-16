using System.IO;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public class BuildConfiguration
    {
        public static ElastiBuildConfig Read(string fileName)
        {
            var ser = new Serializer();
            using (var yamlConfig = File.OpenRead(fileName))
            {
                var config = ser.Deserialize<ElastiBuildConfig>(yamlConfig);
                return config;
            }
        }
    }
}
