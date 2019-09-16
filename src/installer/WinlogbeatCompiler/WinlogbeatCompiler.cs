using System;
using System.IO;
using System.Text.RegularExpressions;
using Elastic.Installer.Shared;
using WixSharp;

namespace Elastic.Installer.Beats
{
    public class WinlogbeatCompiler
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);

            Directory.CreateDirectory(opts.OutDir);

            var package = new ElastiBuild.ArtifactPackage(opts.PackageName, string.Empty);

            var project = new Project("Winlogbeat")
            {
                Name = $"Winlogbeat {package.SemVer} ({package.Architecture})",
                Description = "...",
                OutFileName = Path.Combine(opts.OutDir, opts.PackageName),
                Version = new Version(package.Version),
                ControlPanelInfo = new ProductInfo
                {
                    Manufacturer = "Elastic",
                },

                // TODO: RichEdit control doesn't like plain-text license
                LicenceFile = Path.Combine(opts.InDir, "LICENSE.rtf"),

                // TODO: x64/x86
                Platform = Platform.x64,

                // TODO: x64/x86
                // TODO: GUID versioning
                GUID = Guid.Parse("{A6CFAAFA-623D-4CB4-95B2-3AB11DD52478}"),

                InstallScope = InstallScope.perMachine,

                UI = WUI.WixUI_Minimal,

                BannerImage = Path.Combine(opts.ResDir, "topbanner.bmp"),
                BackgroundImage = Path.Combine(opts.ResDir, "leftbanner.bmp"),

                MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    AllowSameVersionUpgrades = false,
                    //Disallow = true,
                    //DisallowUpgradeErrorMessage = "An existing version is already installed, please uninstall before continuing.",
                    DowngradeErrorMessage = "A more recent version is already installed, please uninstall before continuing.",
                },
            };

            // TODO: Localization?
            // Convert LICENSE.txt to something richedit control can render
            System.IO.File.WriteAllText(
                Path.Combine(opts.InDir, "LICENSE.rtf"), 
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033" +
                @"{\fonttbl{\f0\fnil\fcharset0 Tahoma;}}" +
                @"{\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs18\lang9 " + 
                System.IO.File
                    .ReadAllText(Path.Combine(opts.InDir, "LICENSE.txt"))
                    .Replace("\r\n\r\n", "\n\n")
                    .Replace("\n\n", @"\par" + "\r\n") +
                @"\par}");

            //project.Include(WixExtension.Util);
            //WixExtension.Util.ToXName("");

            var service = new WixSharp.File(Path.Combine(opts.InDir, "winlogbeat.exe"));
            service.ServiceInstaller = new ServiceInstaller
            {
                Name = "winlogbeat",
                DisplayName = $"Winlogbeat {package.SemVer}",
                Description = "Elastic Winlogbeat service",
                Arguments = 
                    "-path.home \"c:/staging/wlb\" " +
                    "-path.data \"c:/staging/wlb/data\" " +
                    "-path.logs \"c:/staging/wlb/logs\" " +
                    "-E logging.files.redirect_stderr=true",

                Interactive = false,
                DependsOn = new[] { new ServiceDependency("Tcpip") },
                Start = SvcStartType.auto,
                //StartOn = SvcEvent.Install,
                StopOn = SvcEvent.InstallUninstall_Wait,
                RemoveOn = SvcEvent.Uninstall_Wait,
                DelayedAutoStart = true,
            };

            var installDir = new InstallDir("Winlogbeat",
                new Files(opts.InDir + @"\*.*", filter =>
                {
                    var itm = filter.ToLower();

                    return !(
                        itm.EndsWith("ps1") ||
                        itm.EndsWith("winlogbeat.exe")
                    );
                }),
                service);

            project.Dirs = new[]
            {
                new Dir("ProgramFiles64Folder")
                {
                    Dirs = new []
                    {
                        new Dir("Elastic")
                        {
                            Dirs = new []
                            {
                                new Dir("7.4.0")
                                {
                                    Dirs = new[]
                                    {
                                        new Dir("Beats")
                                        {
                                            Dirs = new[]
                                            {
                                                installDir
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            Compiler.WixSourceGenerated += (/*XDocument*/ document) =>
            {

            };

            Compiler.AllowNonRtfLicense = true;
            Compiler.PreserveTempFiles = true;

            //Compiler.BuildWxs(project, Compiler.OutputType.MSI);
            //Compiler.BuildMsiCmd(project, Path.Combine(outdir, "compile.cmd"));

            Compiler.BuildMsi(project);
        }
    }
}
