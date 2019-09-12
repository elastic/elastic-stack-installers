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
                await Console.Out.WriteLineAsync(Environment.NewLine +
                $"Building '{target}' in '{ContainerId}' ...");

                var items = await ArtifactsApi.FindArtifact(target, filter =>
                {
                    filter.ContainerId = ContainerId;
                    filter.ShowOss = ShowOss;
                    filter.Bitness = Bitness;
                });

                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items
                        .Select(itm => "  " + itm.Name)
                    ));

                if (items.Count() > 1)
                    throw new Exception("More than one possibility for package. Try specifying --bitness.");

                var ap = items.Single();

                await ArtifactsApi.FetchArtifact(ctx_, ap);
                await ArtifactsApi.UnpackArtifact(ctx_, ap);

                await Command.RunAsync(
                    "dotnet", "msbuild \"" + Path.Combine(ctx_.SrcDir, "installer", "Winlogbeat") +
                    "\" -nr:false -t:Build -p:Configuration=Release");

                await Command.RunAsync(
                    Path.Combine(ctx_.SrcDir, "installer", "Winlogbeat", "bin", "Release", "Winlogbeat-compiler.exe"),
                    "--package-dir=\"" + Path.GetFileNameWithoutExtension(ap.Name) + "\"",
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
                        "Build Winlogbeat version 7.4.0",
                        new BuildCommand
                        {
                            ContainerId = "7.4.0",
                            Targets = new List<string> { "winlogbeat" },
                        }),

                    new Example(Environment.NewLine +
                        "Build Winlogbeat for alias 6.8",
                        new BuildCommand
                        {
                            ContainerId = "6.8",
                            Targets = new List<string> { "winlogbeat" },
                        })

                };
            }
        }
    }
}
