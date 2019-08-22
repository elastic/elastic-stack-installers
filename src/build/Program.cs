using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Bullseye.Internal;

namespace ElastiBuild
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.CaseSensitive = false;
                config.AutoHelp = false;
                config.AutoVersion = false;
                config.IgnoreUnknownArguments = true;
            });

            var opts = CmdLineOptions.Default;

            parser
                .ParseArguments<CmdLineOptions>(args)
                .WithParsed(o =>
                {
                    opts = o;

                    if (string.IsNullOrWhiteSpace(opts.BuildRoot))
                    {
                        var dir = new DirectoryInfo(
                            Path.GetDirectoryName(
                                Assembly.GetExecutingAssembly().Location));

                        const string buildrootMarkerName = ".buildroot";

                        while (dir != null && !File.Exists(Path.Combine(dir.FullName, buildrootMarkerName)))
                            dir = dir.Parent;

                        opts.BuildRoot = dir?.FullName ??
                            throw new Exception(buildrootMarkerName + " marker is missing, should be present in " +
                                                "the root of the repository, next to src, readme.md and license files");
                    }
                })
                .WithNotParsed((errors) =>
                {
                    var log = new Logger(
                        Console.Out, true, false, false,
                        new Palette(false, Host.Unknown, Bullseye.Internal.OperatingSystem.Unknown), false);

                    log.Failed(
                        "Command Line",
                        new Exception(
                            Environment.NewLine +
                            string.Join(
                                Environment.NewLine,
                                errors.Select(err => err.Tag + " " + ((err as TokenError)?.Token) ?? "")) +
                            Environment.NewLine),
                        0);
                });

            await new Program().BuildTaskSetup(parser, new BuildContext(opts));
        }
    }
}
