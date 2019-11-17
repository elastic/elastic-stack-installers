using System;
using System.IO;

namespace Elastic.Installer
{
    public class CommonPathsProvider
    {
        public string BuildRoot => lazyBuildRoot.Value;
        protected static readonly Lazy<string> lazyBuildRoot = new Lazy<string>(
            () => LookupBuildRoot());

        public string SrcDir => lazySrcDir.Value;
        protected static readonly Lazy<string> lazySrcDir = new Lazy<string>(
            () => Path.Combine(lazyBuildRoot.Value, MagicStrings.Dirs.Src));

        public string BinDir => lazyBinDir.Value;
        protected static readonly Lazy<string> lazyBinDir = new Lazy<string>(
            () => Path.Combine(lazyBuildRoot.Value, MagicStrings.Dirs.Bin));

        public string ToolsDir => lazyToolsDir.Value;
        protected static readonly Lazy<string> lazyToolsDir = new Lazy<string>(
            () => Path.Combine(lazyBuildRoot.Value, MagicStrings.Dirs.Tools));

        public string ResDir => lazyResDir.Value;
        protected static readonly Lazy<string> lazyResDir = new Lazy<string>(
            () => Path.Combine(lazySrcDir.Value, MagicStrings.Dirs.Installer, MagicStrings.Dirs.Resources));

        public string ConfigDir => lazyConfigDir.Value;
        protected static readonly Lazy<string> lazyConfigDir = new Lazy<string>(
            () => Path.Combine(lazySrcDir.Value, MagicStrings.Dirs.Config));

        public string InDir => lazyInDir.Value;
        protected static readonly Lazy<string> lazyInDir = new Lazy<string>(
            () => Path.Combine(lazyBinDir.Value, MagicStrings.Dirs.In));

        public string OutDir => lazyOutDir.Value;
        protected static readonly Lazy<string> lazyOutDir = new Lazy<string>(
            () => Path.Combine(lazyBinDir.Value, MagicStrings.Dirs.Out));

        public string CompilerDir => lazyCompilerDir.Value;
        protected static readonly Lazy<string> lazyCompilerDir = new Lazy<string>(
            () => Path.Combine(lazyBinDir.Value, MagicStrings.Dirs.Compiler));

        protected static string LookupBuildRoot()
        {
            var dir = new DirectoryInfo(
                Path.GetDirectoryName(
                    typeof(CommonPathsProvider).Assembly.Location));

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
