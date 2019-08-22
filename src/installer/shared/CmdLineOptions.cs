using System;
using System.IO;
using CommandLine;

namespace Elastic.Installer.Shared
{
    public class CmdLineOptions
    {
        // Initialized to the directory of .buildroot file
        public string BuildRoot { get; private set; }

        [Option("package-dir")]
        public string PackageDir { get; set; }

        public static CmdLineOptions Parse(string[] args_)
        {
            var parser = new Parser(config =>
            {
                config.CaseSensitive = false;
                config.AutoHelp = false;
                config.AutoVersion = false;
                config.IgnoreUnknownArguments = true;
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
                    throw new Exception(buildrootMarker + " marker is missing, should be present in " +
                                        "the root of the repository, next to src, readme.md and license files");
            }

            return opts;
        }
    }
}
