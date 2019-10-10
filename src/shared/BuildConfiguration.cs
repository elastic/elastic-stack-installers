using System;
using System.Collections.Generic;
using System.IO;
using SharpYaml.Serialization;

namespace Elastic.Installer
{
    public class BuildConfiguration
    {
        public static BuildConfiguration Read(string fileName_)
        {
            BuildConfiguration bc = null;
            var ser = new Serializer();

            using (var yamlConfig = File.OpenRead(fileName_))
            {
                bc = new BuildConfiguration
                {
                    fileName = fileName_,
                    packageMap = ser.Deserialize<Dictionary<string, PackageInfo>>(yamlConfig)
                };
            }

            var packageConfigFilePrefix = Path.Combine(
                Path.GetDirectoryName(fileName_),
                Path.GetFileNameWithoutExtension(fileName_));

            foreach (var itm in bc.packageMap.Keys)
            {
                using var yamlConfig = new StreamReader(
                    packageConfigFilePrefix + "-" + itm + MagicStrings.Ext.DotYaml);

                ser.Deserialize<PackageInfo>(yamlConfig, bc.packageMap[itm]);
            }

            return bc;
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
