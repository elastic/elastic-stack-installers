using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using SimpleExec;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("build", HelpText = "Build Targets")]
    public class BuildCommand
        : IElastiBuildCommand
        , ISupportTargets
        , ISupportContainerId
        , ISupportOssChoice
        , ISupportBitnessChoice
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public bool ShowOss { get; set; }
        public eBitness Bitness { get; set; }

        public async Task RunAsync(BuildContext ctx_)
        {
            if (string.IsNullOrWhiteSpace(ContainerId))
            {
                await Console.Out.WriteLineAsync(
                    $"ERROR(s):{Environment.NewLine}" +
                    MagicStrings.Errors.NeedCidWhenTargetSpecified);
                return;
            }

            if (Targets.Any(t => t.ToLower() == "all"))
                Targets = ctx_.Config.TargetNames;

            var compilerSrcDir = Path.Combine(ctx_.SrcDir, "installer", "BeatPackageCompiler");
            var compilerExe = Path.Combine(ctx_.CompilerDir, "BeatPackageCompiler.exe").Quote();

            await Command.RunAsync(
                "dotnet", $"build {compilerSrcDir} --configuration Release --output {ctx_.CompilerDir}");

            foreach (var target in Targets)
            {
                await Console.Out.WriteLineAsync(Environment.NewLine +
                $"Building '{target}' in '{ContainerId}':");

                var items = await ArtifactsApi.FindArtifact(target, filter =>
                {
                    filter.ContainerId = ContainerId;
                    filter.ShowOss = ShowOss;
                    filter.Bitness = Bitness;
                });

                if (items.Count() > 1)
                {
                    await Console.Out.WriteLineAsync(string.Join(
                        Environment.NewLine,
                        items
                            .Select(itm => "  " + itm.FileName)
                        ));

                    throw new Exception("More than one possibility for package. Try specifying --bitness.");
                }

                var ap = items.Single();

                await Console.Out.WriteAsync("Downloading " + ap.FileName + " ... ");
                await ArtifactsApi.FetchArtifact(ctx_, ap);
                await Console.Out.WriteLineAsync("done");

                await Console.Out.WriteAsync("Unpacking " + ap.FileName + " ... ");
                await ArtifactsApi.UnpackArtifact(ctx_, ap);
                await Console.Out.WriteLineAsync("done");

                var args = string.Join(' ', new string[]
                {
                    "--package=\"" + Path.GetFileNameWithoutExtension(ap.FileName) + "\"",
                    (WxsOnly ? "--wxs-only" : string.Empty),
                });

                await Command.RunAsync(compilerExe, args, ctx_.InDir);
            }
        }

        [Option("wxs-only", HelpText = "Only generate .wxs file, skip building .msi")]
        public bool WxsOnly { get; set; }

        // TODO: add env support
        [Option("cert-file", Hidden = true, HelpText = "Path to signing certificate")]
        public string CertFile { get; set; }

        [Option("cert-pass", Hidden = true, HelpText = "Certificate password")]
        public string CertPass { get; set; }

        [Usage(ApplicationAlias = MagicStrings.AppAlias)]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example(Environment.NewLine +
                        "Build Winlogbeat x86 version 7.4.0",
                        new BuildCommand
                        {
                            ContainerId = "7.4.0",
                            Bitness = eBitness.x86,
                            Targets = "winlogbeat".Split(),
                        }),

                    new Example(Environment.NewLine +
                        "Build Winlogbeat x64 OSS for alias 6.8",
                        new BuildCommand
                        {
                            ContainerId = "6.8",
                            ShowOss = true,
                            Targets = "winlogbeat".Split(),
                        }),

                    new Example(Environment.NewLine +
                        "Build all supported installers from 8.0-SNAPSHOT",
                        new BuildCommand
                        {
                            ContainerId = "8.0-SNAPSHOT",
                            Targets = "all".Split(),
                        }),

                };
            }
        }
    }
}
