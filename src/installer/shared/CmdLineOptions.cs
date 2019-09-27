using System;
using System.IO;
using CommandLine;

namespace Elastic.Installer.Shared
{
    public class CmdLineOptions
    {
        [Option("package", Required = true, 
            HelpText = "Full package name without extension, ex: winlogbeat-7.4.0-SNAPSHOT-windows-x86_64")]
        public string PackageName { get; private set; }

        // Initialized to the directory of .buildroot file
        public string BuildRoot { get; private set; }

        // TODO: cache these
        public string SrcDir => Path.Combine(BuildRoot, "src");
        public string BinDir => Path.Combine(BuildRoot, "bin");
        public string InDir => Path.Combine(BinDir, "in", PackageName);
        public string OutDir => Path.Combine(BinDir, "out", PackageName);
        public string SharedDir => Path.Combine(SrcDir, "installer", "shared");
        public string ResDir => Path.Combine(SharedDir, "resources");

        public static CmdLineOptions Parse(string[] args_)
        {
            var parser = new Parser(config =>
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
            {
                var dir = new DirectoryInfo(
                    Path.GetDirectoryName(
                        typeof(CmdLineOptions).Assembly.Location));

                const string buildrootMarker = ".buildroot";

                while (dir != null && !File.Exists(Path.Combine(dir.FullName, buildrootMarker)))
                    dir = dir.Parent;

                opts.BuildRoot = dir?.FullName ??
                    throw new Exception(buildrootMarker + " marker is missing, should be present in the " +
                                        "root of the repository, next to src, readme.md and license files");
            }

            return opts;
        }
    }
}
