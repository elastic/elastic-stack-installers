using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using ElastiBuild.BullseyeTargets;
using ElastiBuild.Extensions;
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
        , ISupportWxsOnlySwitch
    {
        public IEnumerable<string> Targets { get; set; }
        public string ContainerId { get; set; }
        public eBitness Bitness { get; set; }
        public string CertFile { get; set; }
        public string CertPass { get; set; }
        public bool WxsOnly { get; set; }

        public async Task RunAsync()
        {
            if (Targets.Any(t => t.ToLower() == "all"))
                Targets = BuildContext.Default.Config.ProductNames;

            var bt = new Bullseye.Targets();
            var cmd = this;

            bt.Add(
                BuildBeatPackageCompilerTarget.Name,
                async () => await BuildBeatPackageCompilerTarget.RunAsync(BuildContext.Default));

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
                var ctx = new BuildContext();
                ctx.SetCommand(this);

                bt.Add(
                    FindPackageTarget.NameWith(target),
                    async () => await FindPackageTarget.RunAsync(ctx, target));

                bt.Add(
                    FetchPackageTarget.NameWith(target),
                    Bullseye.Targets.DependsOn(FindPackageTarget.NameWith(target)),
                    async () => await FetchPackageTarget.RunAsync(ctx));

                bt.Add(
                    UnpackPackageTarget.NameWith(target),
                    Bullseye.Targets.DependsOn(FetchPackageTarget.NameWith(target)),
                    async () => await UnpackPackageTarget.RunAsync(ctx));

                // sign individual binaries
                if (addSignTarget)
                    ctx.SetCertificate(CertFile, CertPass);

                if (addSignTarget)
                {
                    bt.Add(
                        SignProductBinariesTarget.NameWith(target),
                        Bullseye.Targets.DependsOn(UnpackPackageTarget.NameWith(target)),
                        async () => await SignProductBinariesTarget.RunAsync(ctx));
                }
                else
                {
                    bt.Add(
                        SignProductBinariesTarget.NameWith(target),
                        Bullseye.Targets.DependsOn(UnpackPackageTarget.NameWith(target)),
                        async () => await Console.Out.WriteLineAsync("Skipping digital signature for product binaries"));
                }

                bt.Add(
                    CompileMsiTarget.NameWith(target),
                    Bullseye.Targets.DependsOn(
                        BuildBeatPackageCompilerTarget.Name,
                        SignProductBinariesTarget.NameWith(target)),
                    async () => await CompileMsiTarget.RunAsync(ctx));

                // sign final .msi
                if (addSignTarget)
                {
                    bt.Add(
                        SignMsiPackageTarget.NameWith(target),
                        Bullseye.Targets.DependsOn(CompileMsiTarget.NameWith(target)),
                        async () => await SignMsiPackageTarget.RunAsync(ctx));
                }
                else
                {
                    bt.Add(
                        SignMsiPackageTarget.NameWith(target),
                        Bullseye.Targets.DependsOn(CompileMsiTarget.NameWith(target)),
                        async () => await Console.Out.WriteLineAsync("Skipping digital signature for MSI package"));
                }

                bt.Add(
                    BuildInstallerTarget.NameWith(target),
                    Bullseye.Targets.DependsOn(SignMsiPackageTarget.NameWith(target)),
                    async () => await BuildInstallerTarget.RunAsync(ctx));

                productBuildTargets.Add(BuildInstallerTarget.NameWith(target));
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
                        "Build Winlogbeat x86 version 7.4.0",
                        new BuildCommand
                        {
                            ContainerId = "7.4.0",
                            Bitness = eBitness.x86,
                            Targets = "winlogbeat".Split(),
                        }),

                    new Example(Environment.NewLine +
                        "Build Winlogbeat x64 for alias 6.8",
                        new BuildCommand
                        {
                            ContainerId = "6.8",
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
