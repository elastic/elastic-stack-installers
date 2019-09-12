using System;
using System.IO;
using System.Text.RegularExpressions;
using Elastic.Installer.Shared;
using WixSharp;
using WixSharp.CommonTasks;

namespace Elastic.Installer.Beats
{
    public class Winlogbeat
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);
            var srcdir = Path.Combine(opts.BuildRoot, "bin", "in", opts.PackageDir);
            var outdir = Path.Combine(opts.BuildRoot, "bin", "out", opts.PackageDir);
            var resourceDir = Path.Combine(opts.BuildRoot, "src", "installer", "shared", "resources");

            var rx = new Regex(@"(?<version>\d+\.\d+\.\d+)-(?<snapshot>SNAPSHOT)?", RegexOptions.Compiled);
            var rxVersion = rx.Match(opts.PackageDir);

            Directory.CreateDirectory(outdir);

            var project = new Project("Winlogbeat")
            {
                Name = "Winlogbeat " + rxVersion.Value, //{bitness}
                Description = "...",
                OutFileName = Path.Combine(outdir, opts.PackageDir),
                Version = new Version(rxVersion.Groups["version"].Value),
                ControlPanelInfo = new ProductInfo
                {
                    Manufacturer = "Elastic",
                },

                // TODO: RichEdit control doesn't like plain-text license
                LicenceFile = Path.Combine(srcdir, "LICENSE.rtf"),

                // TODO: x64/x86
                Platform = Platform.x64,

                // TODO: x64/x86
                // TODO: GUID versioning
                GUID = Guid.Parse("{A6CFAAFA-623D-4CB4-95B2-3AB11DD52478}"),

                InstallScope = InstallScope.perMachine,

                UI = WUI.WixUI_Minimal,

                BannerImage = Path.Combine(resourceDir, "topbanner.bmp"),
                BackgroundImage = Path.Combine(resourceDir, "leftbanner.bmp"),

                MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    AllowSameVersionUpgrades = false,
                    //Disallow = true,
                    //DisallowUpgradeErrorMessage = "An existing version is already installed, please uninstall before continuing.",
                    DowngradeErrorMessage = "A more recent version is already installed, please uninstall before continuing.",
                },
            };

            // Hack in LICENSE.rtf file
            System.IO.File.WriteAllText(
                Path.Combine(srcdir, "LICENSE.rtf"), 
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Calibri;}}
{\*\generator Riched20 10.0.18362}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\f0\fs22\lang9 Proper license will come from LICENSE.txt\par
}
");

            //project.Include(WixExtension.Util);
            //WixExtension.Util.ToXName("");

            var service = new WixSharp.File(Path.Combine(srcdir, "winlogbeat.exe"));
            service.ServiceInstaller = new ServiceInstaller
            {
                Name = "winlogbeat",
                DisplayName = "Winlogbeat " + rxVersion.Value,
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
                new Files(srcdir + @"\*.*", filter =>
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
