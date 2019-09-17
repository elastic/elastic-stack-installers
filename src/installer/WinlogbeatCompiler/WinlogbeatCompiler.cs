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

            var companyName = "Elastic";
            var productSetName = "Beats";
            var displayName = "Winlogbeat";
            var serviceName = "winlogbeat";
            var fileName = "winlogbeat.exe";

            var project = new Project(displayName)
            {
                Name = $"{displayName} {package.SemVer} ({package.Architecture})",

                // TODO: Grab this text from README.md
                Description = "Winlogbeat ships Windows event logs to Elasticsearch or Logstash",

                OutFileName = Path.Combine(opts.OutDir, opts.PackageName),
                Version = new Version(package.Version),

                ControlPanelInfo = new ProductInfo
                {
                    Manufacturer = companyName,
                },

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(opts.InDir, "LICENSE.rtf"),

                // TODO: x64/x86
                Platform = Platform.x64,

                // TODO: GUID versioning
                GUID = Guid.Parse("{A6CFAAFA-623D-4CB4-95B2-3AB11DD52478}"),

                InstallScope = InstallScope.perMachine,

                UI = WUI.WixUI_Minimal,

                // TODO: Custom images?
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

            var service = new WixSharp.File(Path.Combine(opts.InDir, fileName));

            var installSubPath = $@"{companyName}\{package.Version}\{productSetName}\{displayName}";

            var mainInstallDir = new InstallDir(
                $@"ProgramFiles64Folder\{installSubPath}",
                new DirFiles(opts.InDir + @"\*.*", filter =>
                {
                    var itm = filter.ToLower();

                    return !(
                        itm.EndsWith("ps1") ||  // we install/remove service ourselves
                        itm.EndsWith("yml") ||  // configuration will go into mutable location
                        itm.EndsWith(fileName)  // .exe must be excluded for service configuration to work
                    );
                }),
                service);

            // TODO: CNDL1150 : ServiceConfig functionality is documented in the Windows Installer SDK to 
            //                  "not [work] as expected." Consider replacing ServiceConfig with the 
            //                  WixUtilExtension ServiceConfig element.

            service.ServiceInstaller = new ServiceInstaller
            {
                Interactive = false,

                Name = serviceName,
                DisplayName = $"{displayName} {package.SemVer}",
                Description = $"{companyName} {displayName} service",
                DependsOn = new[] { new ServiceDependency("Tcpip") },

                Arguments =
                    $" -path.home \"[CommonAppDataFolder]{installSubPath}\"" +
                    //$" -path.data \"[CommonAppDataFolder]{installSubPath}\\data\"" +
                    //$" -path.logs \"[CommonAppDataFolder]{installSubPath}\\logs\"" +
                    $" -E logging.files.redirect_stderr=true",

                DelayedAutoStart = true,
                Start = SvcStartType.auto,

                //StartOn = SvcEvent.Install,
                StopOn = SvcEvent.InstallUninstall_Wait,
                RemoveOn = SvcEvent.Uninstall_Wait,
            };

            // TODO: Get directory names from FS
            var mutableInstallDir = new Dir(
                $@"CommonAppDataFolder\{installSubPath}",
                new DirFiles(opts.InDir + @"\*.yml"), 
                new Dir("kibana", new Files(Path.Combine(opts.InDir, "kibana") + @"\*.*")),
                new Dir("module", new Files(Path.Combine(opts.InDir, "module") + @"\*.*")));

            project.Dirs = new[]
            {
                mainInstallDir,
                mutableInstallDir
            };

            //Compiler.WixSourceGenerated += (/*XDocument*/ document) => { };

            //Compiler.AllowNonRtfLicense = true;
            Compiler.PreserveTempFiles = true;

            //Compiler.BuildWxs(project, Compiler.OutputType.MSI);
            //Compiler.BuildMsiCmd(project, Path.Combine(outdir, "compile.cmd"));

            Compiler.BuildMsi(project);
        }
    }
}
