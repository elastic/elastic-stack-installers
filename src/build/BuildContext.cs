using System;
using System.IO;
using ElastiBuild;
using ElastiBuild.Options;

namespace Elastic.Installer
{
    public class BuildContext : BuildRootPrivider
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
            var ctx = new BuildContext
            {
                BuildRoot = LookupBuildRoot()
            };

            return ctx;
        }
    }
}
