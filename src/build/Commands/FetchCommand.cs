using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ElastiBuild.Options;
using static Bullseye.Targets;

namespace ElastiBuild.Commands
{
    [Verb("fetch", HelpText = "Download and optionally unpack input artifacts")]
    public class FetchCommand
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
                $"Fetching '{target}' in '{ContainerId}' ...");

                var items = await ArtifactsApi.FindArtifact(target, filter =>
                {
                    filter.ContainerId = ContainerId;
                    filter.ShowOss = ShowOss;
                    filter.Bitness = Bitness;
                });

                foreach (var ap in items)
                {
                    await Console.Out.WriteAsync("  " + ap.Name + " ... ");
                    await ArtifactsApi.FetchArtifact(ctx_, ap);
                    await Console.Out.WriteLineAsync("done");
                }
            }
        }

        [Usage(ApplicationAlias = GlobalOptions.AppAlias)]
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
                            Targets = new List<string> { "winlogbeat" },
                        }),

                    new Example(Environment.NewLine +
                        "Fetch Winlogbeat packages for alias 6.8",
                        new FetchCommand
                        {
                            ContainerId = "6.8",
                            Targets = new List<string> { "winlogbeat" },
                        })

                };
            }
        }
    }
}
