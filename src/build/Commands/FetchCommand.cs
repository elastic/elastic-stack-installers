using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.BullseyeTargets;
using ElastiBuild.Extensions;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("fetch", HelpText = "Download and optionally unpack packages")]
    public class FetchCommand
        : IElastiBuildCommand
        , ISupportRequiredTargets
        , ISupportRequiredContainerId
        , ISupportBitnessChoice
        , ISupportForceSwitch
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }
        public bool ForceSwitch { get; set; }

        public async Task RunAsync()
        {
            if (Targets.Any(t => t.ToLower() == "all"))
                Targets = BuildContext.Default.Config.ProductNames;

            var bt = new Bullseye.Targets();
            var cmd = this;

            var productBuildTargets = new List<string>();

            foreach (var target in Targets)
            {
                var product = target;
                var ctx = new BuildContext();
                ctx.SetCommand(this);

                bt.Add(
                    FindPackageTarget.NameWith(product),
                    async () => await FindPackageTarget.RunAsync(ctx, product));

                bt.Add(
                    FetchPackageTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(FindPackageTarget.NameWith(product)),
                    async () => await FetchPackageTarget.RunAsync(ctx));

                productBuildTargets.Add(FetchPackageTarget.NameWith(product));
            }

            try
            {
                await bt.RunWithoutExitingAsync(productBuildTargets);
            }
            catch
            {
                // We swallow exceptions here, BullsEye prints them
                // TODO: use overload "messageOnly"
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
                            ForceSwitch = true,
                            Targets = "winlogbeat-oss".Split(),
                        })
                };
            }
        }
    }
}
