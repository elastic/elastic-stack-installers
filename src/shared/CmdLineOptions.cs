using System;
using System.IO;
using CommandLine;
using Elastic.Installer;

namespace Elastic.PackageCompiler
{
    public class CmdLineOptions : CommonPathsProvider
    {
        [Option("package", Required = true,
            HelpText = "Full package name without extension, ex: winlogbeat-7.4.0-SNAPSHOT-windows-x86_64")]
        public string PackageName { get; private set; }

        [Option("wxs-only", HelpText = "Only generate .wxs file, skip building .msi")]
        public bool WxsOnly { get; private set; }

        [Option("cmd-only", HelpText = "Only generate .wxs and .cmd file (build script in SrcDir), skip building .msi")]
        public bool CmdOnly { get; private set; }

        [Option('v', "verbose", Default = false, HelpText = "Wix Toolset will print verbose output")]
        public bool Verbose { get; set; }

        [Option("keep-temp-files", Default = false, HelpText = "Preserve (do not delete) temporary files")]
        public bool KeepTempFiles { get; set; }

        public string ShortPackageName =>
            PackageName?.Substring(0, PackageName.IndexOf('-')) ?? string.Empty;

        public string PackageInDir => Path.Combine(InDir, PackageName);
        public string PackageOutDir => Path.Combine(OutDir, ShortPackageName);

        public static CmdLineOptions Parse(string[] args)
        {
            using var parser = new Parser(config =>
            {
                config.CaseSensitive = false;
                config.AutoHelp = false;
                config.AutoVersion = false;
                config.IgnoreUnknownArguments = false;
            });

            var res = parser.ParseArguments(() => new CmdLineOptions(), args);

            if (res is NotParsed<CmdLineOptions>)
                throw new Exception("bad command line args");

            var opts = (res as Parsed<CmdLineOptions>).Value;

            return opts;
        }
    }
}
