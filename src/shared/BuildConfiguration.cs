using System;
using System.Collections.Generic;
using System.IO;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public class BuildConfiguration
    {
        public static BuildConfiguration Read(string fileName)
        {
            BuildConfiguration bc = null;
            var ser = new Serializer();

            using (var yamlConfig = File.OpenRead(fileName))
            {
                bc = new BuildConfiguration
                {
                    fileName = fileName,
                    packageMap = ser.Deserialize<Dictionary<string, PackageInfo>>(yamlConfig)
                };
            }

            return bc;
        }

        public IEnumerable<string> ProductNames => packageMap?.Keys;

        public PackageInfo GetPackageInfo(string targetName)
        {
            if (!packageMap.TryGetValue(targetName, out PackageInfo pi))
                throw new ArgumentException($"Unable to find {targetName} section in {fileName}");

            return pi;
        }

        string fileName;
        Dictionary<string, PackageInfo> packageMap;
    }
}
