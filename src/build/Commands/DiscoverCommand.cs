using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("discover", HelpText = "Discover Container Id's used for build and fetch commands")]
    public class DiscoverCommand
        : IElastiBuildCommand
        , ISupportRequiredTargets
        , ISupportContainerId
        , ISupportBitnessChoice
        , ISupportForceSwitch
    {
        public IEnumerable<string> Targets { get; set; } = new List<string>();
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }
        public bool ForceSwitch { get; set; }

        public async Task RunAsync()
        {
            if (Targets.Any(t => t.ToLower() == "containers"))
            {
                var items = await ArtifactsApi.ListNamedContainers();

                await Console.Out.WriteLineAsync(Environment.NewLine + "Branches:");
                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items
                        .Where(itm => itm.IsBranch)
                        .Select(itm => "  " + itm.Name)
                    ));

                await Console.Out.WriteLineAsync(Environment.NewLine + "Versions:");
                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items
                        .Where(itm => itm.IsVersion)
                        .Select(itm => "  " + itm.Name)
                    ));

                await Console.Out.WriteLineAsync(Environment.NewLine + "Aliases:");
                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items
                        .Where(itm => itm.IsAlias)
                        .Select(itm => "  " + itm.Name)
                    ));

                return;
            }

            if (string.IsNullOrWhiteSpace(ContainerId))
            {
                await Console.Out.WriteLineAsync(
                    $"ERROR(s):{Environment.NewLine}" +
                    MagicStrings.Errors.NeedCidWhenTargetSpecified);
                return;
            }

            if (Targets.Any(t => t.ToLower() == "all"))
                Targets = BuildContext.Default.Config.ProductNames;

            foreach (var target in Targets)
            {
                if (!ForceSwitch && !BuildContext.Default.Config.ProductNames.Any(t => t.ToLower() == target))
                    throw new InvalidDataException($"Invalid product '{target}'");

                await Console.Out.WriteLineAsync(Environment.NewLine +
                    $"Discovering '{target}' in '{ContainerId}' ...");

                var items = await ArtifactsApi.FindArtifact(target, filter =>
                {
                    filter.ContainerId = ContainerId;
                    filter.Bitness = Bitness;
                });

                await Console.Out.WriteLineAsync(string.Join(
                    Environment.NewLine,
                    items.Select(itm => "  " + itm.FileName + Environment.NewLine + "  " + itm.Url + Environment.NewLine)
                    ));
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
                        "Discover branches, versions and aliases",
                        new DiscoverCommand()
                        {
                            Targets = new List<string> { "containers" },
                        }),

                    new Example(Environment.NewLine +
                        "Discover available Winlogbeat packages for alias 6.8",
                        new DiscoverCommand
                        {
                            ContainerId = "6.8",
                            Targets = new List<string> { "winlogbeat" },
                        }),

                    new Example(Environment.NewLine +
                        "Discover package names for all supported products in 8.0-SNAPSHOT",
                        new DiscoverCommand()
                        {
                            Targets = new List<string> { "all" },
                            ContainerId = "8.0-SNAPSHOT"
                        }),
                };
            }
        }
    }
}
