using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("fetch", HelpText = "Download and optionally unpack input artifacts")]
    public class FetchCommand
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

            foreach (var target in Targets)
            {
                await Console.Out.WriteLineAsync(Environment.NewLine +
                $"Fetching '{target}' in '{ContainerId}':");

                var items = await ArtifactsApi.FindArtifact(target, filter =>
                {
                    filter.ContainerId = ContainerId;
                    filter.ShowOss = ShowOss;
                    filter.Bitness = Bitness;
                });

                foreach (var ap in items)
                {
                    await Console.Out.WriteAsync("  " + ap.FileName + " ... ");
                    await ArtifactsApi.FetchArtifact(ctx_, ap);
                    await Console.Out.WriteLineAsync("done");
                }
            }
        }

        [Usage(ApplicationAlias = MagicStrings.AppAlias)]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>()
                {
                    new Example(Environment.NewLine +
                        "Fetch Winlogbeat packages for version 7.4.0",
                        new FetchCommand
                        {
                            ContainerId = "7.4.0",
                            Targets = "winlogbeat".Split(),
                        }),

                    new Example(Environment.NewLine +
                        "Fetch Winlogbeat OSS packages for alias 6.8",
                        new FetchCommand
                        {
                            ContainerId = "6.8",
                            ShowOss = true,
                            Targets = "winlogbeat".Split(),
                        })
                };
            }
        }
    }
}
