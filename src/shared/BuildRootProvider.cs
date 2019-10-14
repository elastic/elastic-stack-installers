using System;
using System.IO;

namespace Elastic.Installer
{
    public class BuildRootPrivider
    {
        protected static string LookupBuildRoot()
        {
            var dir = new DirectoryInfo(
                Path.GetDirectoryName(
                    typeof(BuildRootPrivider).Assembly.Location));

            while (dir != null && !File.Exists(Path.Combine(dir.FullName, MagicStrings.Files.BuildRoot)))
                dir = dir.Parent;

            return dir?.FullName ??
                throw new Exception(
                    MagicStrings.Files.BuildRoot +
                    " marker is missing, should be present in the root of" +
                    " the repository, next to src, readme.md and license files");
        }
    }
}