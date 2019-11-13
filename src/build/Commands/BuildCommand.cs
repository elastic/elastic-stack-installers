using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.BullseyeTargets;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("build", HelpText = "Build one or more installers")]
    public class BuildCommand
        : IElastiBuildCommand
        , ISupportRequiredTargets
        , ISupportRequiredContainerId
        , ISupportOssSwitch
        , ISupportBitnessChoice
        , ISupportWxsOnlySwitch
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public bool ShowOss { get; set; }
        public eBitness Bitness { get; set; }
        public bool WxsOnly { get; set; }

        public async Task RunAsync()
        {
            if (Targets.Any(t => t.ToLower() == "all"))
                Targets = BuildContext.Default.Config.ProductNames;

            var bt = new Bullseye.Targets();
            var cmd = this;

            bt.Add(
                BuildBeatPackageCompilerTarget.Name,
                async () => await BuildBeatPackageCompilerTarget.RunAsync(cmd, BuildContext.Default));

            var productBuildTargets = new List<string>();

            foreach (var target in Targets)
            {
                var product = target;
                var ctx = new BuildContext();

                bt.Add(
                    FindPackageTarget.NameWith(product),
                    async () => await FindPackageTarget.RunAsync(cmd, ctx, product));

                bt.Add(
                    FetchPackageTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(FindPackageTarget.NameWith(product)),
                    async () => await FetchPackageTarget.RunAsync(cmd, ctx, product));

                bt.Add(
                    UnpackPackageTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(FetchPackageTarget.NameWith(product)),
                    async () => await UnpackPackageTarget.RunAsync(cmd, ctx, product));

                bt.Add(
                    BuildInstallerTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(
                        BuildBeatPackageCompilerTarget.Name,
                        UnpackPackageTarget.NameWith(product)),
                    async () => await BuildInstallerTarget.RunAsync(cmd, ctx, product));

                productBuildTargets.Add(BuildInstallerTarget.NameWith(product));
            }

            try
            {
                await bt.RunWithoutExitingAsync(
                    productBuildTargets,
                    logPrefix: "ElastiBuild");
            }
            catch
            {
                // We swallow exceptions here, BullsEye prints them
                // TODO: use overload "messageOnly"
            }
        }

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
                        "Build all supported products from 8.0-SNAPSHOT",
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
