using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

using SimpleExec;
using ElastiBuild.Options;

namespace ElastiBuild.Commands
{
    [Verb("build", HelpText = "Build Targets")]
    public class BuildCommand
        : IElastiBuildCommand
        , ISupportTargets
        , ISupportContainerId
        , ISupportOssChoice
        , ISupportPlatformChoice
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public bool ShowOss { get; set; }
        public eBitness Bitness { get; set; }

        public async Task RunAsync(BuildContext ctx_)
        {
            foreach (var target in Targets)
            {
                // TODO: remove hack
                if (target.ToLower() != "winlogbeat")
                    continue;

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

                var quotedInstallerDir = ctx_.MakeInstallerDir(ap.TargetName, quote_: true);
                var quotedPackagesDir = ctx_.MakePackagesDir(quote_: true);

                // TODO: check exit code
                await Command.RunAsync(
                    Path.Combine(ctx_.BinDir, "nuget.exe"),
                    "restore " + quotedInstallerDir + " -PackagesDirectory " + quotedPackagesDir);
                
                await Command.RunAsync(
                    "dotnet", 
                    "msbuild -r:true -t:Build -nr:false -p:Configuration=Release " + quotedInstallerDir);

                await Command.RunAsync(
                    ctx_.MakeCompilerFilename(ap.TargetName),
                    "--package=\"" + Path.GetFileNameWithoutExtension(ap.FileName) + "\"",
                    ctx_.InDir);
            }
        }

        // TODO: add env support
        [Option("cert-file", Hidden = true, HelpText = "Path to signing certificate")]
        public string CertFile { get; set; }

        [Option("cert-pass", Hidden = true, HelpText = "Certificate password")]
        public string CertPass { get; set; }

        [Usage(ApplicationAlias = GlobalOptions.AppAlias)]
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
                            Bitness = eBitness.x64,
                            Targets = "winlogbeat".Split(),
                        })
                };
            }
        }
    }
}
