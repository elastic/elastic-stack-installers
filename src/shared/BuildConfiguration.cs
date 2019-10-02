using System;
using System.Collections.Generic;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public class BuildConfiguration
    {
        public static BuildConfiguration Read(string fileName)
        {
            using (var yamlConfig = System.IO.File.OpenRead(fileName))
            {
                var ser = new Serializer();
                return new BuildConfiguration
                {
                    fileName = fileName,
                    packageMap = ser.Deserialize<Dictionary<string, PackageInfo>>(yamlConfig)
                };
            }
        }

        public PackageInfo GetPackageInfo(string targetName_)
        {
            if (!packageMap.TryGetValue(targetName_, out PackageInfo pi))
                throw new ArgumentException($"Unable to find {targetName_} section in {fileName}");

            return pi;
        }

        string fileName;
        Dictionary<string, PackageInfo> packageMap;
    }
}
