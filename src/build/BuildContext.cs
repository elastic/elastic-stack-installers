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
        public string SrcDir => Path.Combine(BuildRoot, MagicStrings.Dirs.Src);
        public string BinDir => Path.Combine(BuildRoot, MagicStrings.Dirs.Bin);
        public string InDir => Path.Combine(BinDir, MagicStrings.Dirs.In);
        public string OutDir => Path.Combine(BinDir, MagicStrings.Dirs.Out);
        public string ResDir => Path.Combine(SrcDir, MagicStrings.Dirs.Installer, MagicStrings.Dirs.Resources);
        public string ConfigDir => Path.Combine(SrcDir, MagicStrings.Dirs.Config);
        public string CompilerDir => Path.Combine(BinDir, MagicStrings.Dirs.Compiler);

        public static BuildContext Create()
        {
            var dir = new DirectoryInfo(
                Path.GetDirectoryName(
                    typeof(Program).Assembly.Location));

            while (dir != null && !File.Exists(Path.Combine(dir.FullName, MagicStrings.Files.BuildRoot)))
                dir = dir.Parent;

            var ctx = new BuildContext
            {
                BuildRoot = dir?.FullName 
                    ?? throw new Exception(MagicStrings.Files.BuildRoot + " marker is missing, should be present in " +
                                    "the root of the repository, next to src, readme.md and license files")
            };

            return ctx;
        }
    }
}
