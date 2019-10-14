using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixSharp;
using Elastic.Installer;

namespace Elastic.PackageCompiler.Beats
{
    public class BeatPackageCompiler
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);

            Directory.CreateDirectory(opts.OutDir);

            var ap = new ArtifactPackage(opts.PackageName);

            var config = BuildConfiguration.Read(
                Path.Combine(opts.ConfigDir, MagicStrings.Files.ConfigYaml));

            var pi = config.GetPackageInfo(ap.TargetName);

            var companyName = MagicStrings.Elastic;
            var productSetName = MagicStrings.Beats;
            var displayName = MagicStrings.Beats + " " + ap.TargetName;
            var serviceName = ap.TargetName;
            var exeName = ap.TargetName + MagicStrings.Ext.DotExe;

            // TODO: validate/process Product Id
            //       bi.KnownVersions

            var project = new Project(displayName)
            {
                // This GUID *must* be stable and unique per-beat
                GUID = pi.UpgradeCode,

                Name = $"{displayName} {ap.SemVer} ({ap.Architecture})",

                Description = pi.Description,

                OutFileName = Path.Combine(opts.OutDir, opts.PackageName),
                Version = new Version(ap.Version),

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(opts.OutDir, MagicStrings.Files.LicenseRtf),

                Platform = ap.Is32bit ? Platform.x86 : Platform.x64,

                InstallScope = InstallScope.perMachine,

                UI = WUI.WixUI_Minimal,

                // TODO: Custom images?
                BannerImage = Path.Combine(opts.ResDir, MagicStrings.Files.TopBannerBmp),
                BackgroundImage = Path.Combine(opts.ResDir, MagicStrings.Files.LeftBannerBmp),

                MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    AllowSameVersionUpgrades = false,
                    //Disallow = true,
                    //DisallowUpgradeErrorMessage = "An existing version is already installed, please uninstall before continuing.",
                    DowngradeErrorMessage = "A more recent version is already installed, please uninstall before continuing.",
                },
            };

            project.ControlPanelInfo = new ProductInfo
            {
                Contact = companyName,
                Manufacturer = companyName,
                UrlInfoAbout = "https://www.elastic.co/downloads/beats",

                Comments = pi.Description + 
                           ". Beats is the platform for single-purpose data shippers. They send data " +
                           "from hundreds or thousands of machines and systems to Logstash or Elasticsearch.",

                ProductIcon = Path.Combine(
                    opts.ResDir,
                    Path.GetFileNameWithoutExtension(exeName) + MagicStrings.Ext.DotIco),
            };

            // Convert LICENSE.txt to something richedit control can render
            System.IO.File.WriteAllText(
                Path.Combine(opts.OutDir, MagicStrings.Files.LicenseRtf),
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033" +
                @"{\fonttbl{\f0\fnil\fcharset0 Tahoma;}}" +
                @"{\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs18\lang9 " +
                System.IO.File
                    .ReadAllText(Path.Combine(opts.InDir, MagicStrings.Files.LicenseTxt))
                    .Replace("\r\n\r\n", "\n\n")
                    .Replace("\n\n", @"\par" + "\r\n") +
                @"\par}");

            var installSubPath = $@"{companyName}\{ap.Version}\{productSetName}\{serviceName}";

            WixSharp.File service = null;
            if (pi.IsWindowsService)
            {
                service = new WixSharp.File(Path.Combine(opts.InDir, exeName));

                // TODO: CNDL1150 : ServiceConfig functionality is documented in the Windows Installer SDK to 
                //                  "not [work] as expected." Consider replacing ServiceConfig with the 
                //                  WixUtilExtension ServiceConfig element.

                service.ServiceInstaller = new ServiceInstaller
                {
                    Interactive = false,

                    Name = serviceName,
                    DisplayName = $"{displayName} {ap.SemVer}",
                    Description = pi.Description,
                    DependsOn = new[] { new ServiceDependency(MagicStrings.Services.Tcpip) },

                    Arguments =
                        $" -path.home \"[CommonAppDataFolder]{installSubPath}\"" +
                        $" -E logging.files.redirect_stderr=true",

                    DelayedAutoStart = true,
                    Start = SvcStartType.auto,

                    // Don't start on install, config file is likely not ready yet
                    //StartOn = SvcEvent.Install,

                    StopOn = SvcEvent.InstallUninstall_Wait,
                    RemoveOn = SvcEvent.Uninstall_Wait,
                };
            }

            var elements = new List<WixEntity>
            {
                new DirFiles(Path.Combine(opts.InDir, MagicStrings.Ext.All), path =>
                {
                    var itm = path.ToLower();

                    bool include = !(

                        // configuration will go into mutable location
                        itm.EndsWith(MagicStrings.Ext.DotYml) ||

                        // we install/remove service ourselves
                        itm.EndsWith(MagicStrings.Ext.DotPs1) ||

                        // .exe must be excluded for service configuration to work
                        (pi.IsWindowsService ? itm.EndsWith(exeName) : false)
                    );

                    return include;
                })
            };

            elements.AddRange(
                new DirectoryInfo(opts.InDir)
                    .GetDirectories()
                    .Select(dirName => dirName.Name)
                    .Except(pi.MutableDirs)
                    .Select(dirName => new Dir(dirName, new Files(Path.Combine(opts.InDir, dirName, MagicStrings.Ext.All)))));

            elements.Add(pi.IsWindowsService ? service : null);

            var mainInstallDir = new InstallDir(
                $@"ProgramFiles{(ap.Is64Bit ? "64" : string.Empty)}Folder\{installSubPath}",
                elements.ToArray());

            // TODO: evaluate adding metadata file into beats repo that lists these per-beat
            var mutablePaths = new List<WixEntity>
            {
                new DirFiles(Path.Combine(opts.InDir, MagicStrings.Ext.DotYml))
            };

            // These are the directories that we know of
            mutablePaths.AddRange(
                pi.MutableDirs
                    .Select(dirName =>
                    {
                        var dirPath = Path.Combine(opts.InDir, dirName);
                        return Directory.Exists(dirPath)
                            ? new Dir(dirName, new Files(Path.Combine(dirPath, MagicStrings.Ext.All)))
                            : null;
                    })
                    .Where(dir => dir != null));

            project.Dirs = new[]
            {
                mainInstallDir,

                // Mutable path
                new Dir(
                    $@"CommonAppDataFolder\{installSubPath}",
                    mutablePaths.ToArray())
            };

            project.ResolveWildCards();

            //Compiler.WixSourceGenerated += (/*XDocument*/ document) => { };

            //Compiler.AllowNonRtfLicense = true;
            Compiler.PreserveTempFiles = true;

            //Compiler.BuildWxs(project, Compiler.OutputType.MSI);
            //Compiler.BuildMsiCmd(project, Path.Combine(outdir, "compile.cmd"));

            Compiler.BuildMsi(project);
        }
    }
}
