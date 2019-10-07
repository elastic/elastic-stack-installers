using System;
using System.IO;
using ElastiBuild;
using ElastiBuild.Options;

namespace Elastic.Installer
{
    public class BuildContext
    {
        public string BuildRoot { get; set; }

        public GlobalOptions Options { get; private set; }

        // TODO: cache these
        public string SrcDir => Path.Combine(BuildRoot, "src");
        public string InDir => Path.Combine(BinDir, "in");
        public string OutDir => Path.Combine(BinDir, "out");
        public string BinDir => Path.Combine(BuildRoot, "bin");
        public string CompilerDir => Path.Combine(BinDir, "compiler");
        public string ConfigDir => Path.Combine(SrcDir, "config");

        public static BuildContext Create()
        {
            var dir = new DirectoryInfo(
                Path.GetDirectoryName(
                    typeof(Program).Assembly.Location));

            const string buildrootMarkerName = ".buildroot";

            while (dir != null && !File.Exists(Path.Combine(dir.FullName, buildrootMarkerName)))
                dir = dir.Parent;

            var ctx = new BuildContext
            {
                BuildRoot = dir?.FullName 
                    ?? throw new Exception(buildrootMarkerName + " marker is missing, should be present in " +
                                    "the root of the repository, next to src, readme.md and license files")
            };

            return ctx;
        }
    }
}
