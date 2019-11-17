using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.BullseyeTargets;
using ElastiBuild.Extensions;
using ElastiBuild.Infra;
using Elastic.Installer;

namespace ElastiBuild.Commands
{
    [Verb("build", HelpText = "Build one or more installers")]
    public class BuildCommand
        : IElastiBuildCommand
        , ISupportRequiredTargets
        , ISupportRequiredContainerId
        , ISupportBitnessChoice
        , ISupportCodeSigning
        , ISupportOssSwitch
        , ISupportWxsOnlySwitch
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }
        public string CertFile { get; set; }
        public string CertPass { get; set; }
        public bool ShowOss { get; set; }
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

            bool addSignTarget = false;
            if (!CertPass.IsEmpty() && File.Exists(CertFile))
            {
                // Let's try value as file name first, then as env var
                var password = CertPass;

                try
                { password = await File.ReadAllTextAsync(CertPass); }
                catch
                { password = Environment.GetEnvironmentVariable(password); }

                if (!password.IsEmpty())
                {
                    CertPass = password;
                    addSignTarget = true;
                }
            }

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

                // sign individual binaries
                if (addSignTarget)
                    ctx.UseCertificate(CertFile, CertPass);

                if (addSignTarget)
                {
                    bt.Add(
                        SignProductBinariesTarget.NameWith(product),
                        Bullseye.Targets.DependsOn(UnpackPackageTarget.NameWith(product)),
                        async () => await SignProductBinariesTarget.RunAsync(cmd, ctx, product));
                }
                else
                {
                    bt.Add(
                        SignProductBinariesTarget.NameWith(product),
                        Bullseye.Targets.DependsOn(UnpackPackageTarget.NameWith(product)),
                        async () => await Console.Out.WriteLineAsync("Skipping digital signature for product binaries"));
                }

                bt.Add(
                    CompileMsiTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(
                        BuildBeatPackageCompilerTarget.Name,
                        SignProductBinariesTarget.NameWith(product)),
                    async () => await CompileMsiTarget.RunAsync(cmd, ctx, product));

                // sign final .msi
                if (addSignTarget)
                {
                    bt.Add(
                        SignMsiPackageTarget.NameWith(product),
                        Bullseye.Targets.DependsOn(CompileMsiTarget.NameWith(product)),
                        async () => await SignMsiPackageTarget.RunAsync(cmd, ctx, product));
                }
                else
                {
                    bt.Add(
                        SignMsiPackageTarget.NameWith(product),
                        Bullseye.Targets.DependsOn(CompileMsiTarget.NameWith(product)),
                        async () => await Console.Out.WriteLineAsync("Skipping digital signature for MSI package"));
                }

                bt.Add(
                    BuildInstallerTarget.NameWith(product),
                    Bullseye.Targets.DependsOn(SignMsiPackageTarget.NameWith(product)),
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
