using System;
using System.IO;
using CommandLine;
using Elastic.Installer;

namespace Elastic.PackageCompiler
{
    public class CmdLineOptions : BuildRootPrivider
    {
        [Option("package", Required = true,
            HelpText = "Full package name without extension, ex: winlogbeat-7.4.0-SNAPSHOT-windows-x86_64")]
        public string PackageName { get; private set; }

        [Option("wxs-only", HelpText = "Only generate .wxs file, skip building .msi")]
        public bool WxsOnly { get; private set; }

        // Initialized to the directory of .buildroot file
        public string BuildRoot { get; private set; }

        // TODO: cache these
        public string SrcDir => Path.Combine(BuildRoot, MagicStrings.Dirs.Src);
        public string BinDir => Path.Combine(BuildRoot, MagicStrings.Dirs.Bin);
        public string InDir => Path.Combine(BinDir, MagicStrings.Dirs.In, PackageName);
        public string OutDir => Path.Combine(BinDir, MagicStrings.Dirs.Out, PackageName);
        public string ResDir => Path.Combine(SrcDir, MagicStrings.Dirs.Installer, MagicStrings.Dirs.Resources);
        public string ConfigDir => Path.Combine(SrcDir, MagicStrings.Dirs.Config);

        public static CmdLineOptions Parse(string[] args_)
        {
            using var parser = new Parser(config =>
            {
                config.CaseSensitive = false;
                config.AutoHelp = false;
                config.AutoVersion = false;
                config.IgnoreUnknownArguments = false;
            });

            var res = parser.ParseArguments(() => new CmdLineOptions(), args_);

            if (res is NotParsed<CmdLineOptions>)
                throw new Exception("bad command line args");

            var opts = (res as Parsed<CmdLineOptions>).Value;

            if (string.IsNullOrWhiteSpace(opts.BuildRoot))
                opts.BuildRoot = LookupBuildRoot();

            return opts;
        }
    }
}
