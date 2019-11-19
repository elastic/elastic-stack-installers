using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatPackageCompiler.Properties;
using Elastic.Installer;
using WixSharp;
using WixSharp.CommonTasks;

namespace Elastic.PackageCompiler.Beats
{
    public class BeatPackageCompiler
    {
        static void Main(string[] args)
        {
            var opts = CmdLineOptions.Parse(args);

            var config = BuildConfiguration.Read(
                Path.Combine(opts.ConfigDir, MagicStrings.Files.ConfigYaml));

            Directory.CreateDirectory(opts.PackageOutDir);

            var ap = new ArtifactPackage(opts.PackageName);
            var pc = config.GetProductConfig(ap.TargetName);

            var companyName = MagicStrings.Elastic;
            var productSetName = MagicStrings.Beats.Name;
            var beatName = ap.TargetName;
            var displayName = MagicStrings.Beats.Name + " " + ap.TargetName;
            var serviceName = ap.TargetName;
            var exeName = ap.TargetName + MagicStrings.Ext.DotExe;

            // TODO: validate/process Product Id

            var project = new Project(displayName)
            {
                // This GUID *must* be stable and unique per-beat
                GUID = pc.UpgradeCode,

                Name = $"{displayName} {ap.SemVer} ({ap.Architecture})",

                Description = pc.Description,

                OutFileName = Path.Combine(opts.PackageOutDir, opts.PackageName),
                Version = new Version(ap.Version),

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(
                    opts.PackageOutDir,
                    MagicStrings.Files.PackageLicenseRtf(opts.PackageName)),

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
                    DowngradeErrorMessage = MagicStrings.Errors.NewerVersionInstalled,
                },
            };

            project.ControlPanelInfo = new ProductInfo
            {
                Contact = companyName,
                Manufacturer = companyName,
                UrlInfoAbout = "https://www.elastic.co/downloads/beats",

                Comments = pc.Description + ". " + MagicStrings.Beats.Description,

                ProductIcon = Path.Combine(
                    opts.ResDir,
                    Path.GetFileNameWithoutExtension(exeName) + MagicStrings.Ext.DotIco),

                NoRepair = true,
            };

            // Convert LICENSE.txt to something richedit control can render
            System.IO.File.WriteAllText(
                Path.Combine(opts.PackageOutDir, MagicStrings.Files.PackageLicenseRtf(opts.PackageName)),
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033" +
                @"{\fonttbl{\f0\fnil\fcharset0 Tahoma;}}" +
                @"{\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs18\lang9 " +
                System.IO.File
                    .ReadAllText(Path.Combine(opts.PackageInDir, MagicStrings.Files.LicenseTxt))
                    .Replace("\r\n\r\n", "\n\n")
                    .Replace("\n\n", @"\par" + "\r\n") +
                @"\par}");

            var beatDataPath = Path.Combine("[CommonAppDataFolder]", companyName, productSetName, beatName);

            WixSharp.File service = null;
            if (pc.IsWindowsService)
            {
                service = new WixSharp.File(Path.Combine(opts.PackageInDir, exeName));

                // TODO: CNDL1150 : ServiceConfig functionality is documented in the Windows Installer SDK to 
                //                  "not [work] as expected." Consider replacing ServiceConfig with the 
                //                  WixUtilExtension ServiceConfig element.

                service.ServiceInstaller = new ServiceInstaller
                {
                    Interactive = false,

                    Name = serviceName,
                    DisplayName = $"{displayName} {ap.SemVer}",
                    Description = pc.Description,
                    DependsOn = new[] { new ServiceDependency(MagicStrings.Services.Tcpip) },

                    Arguments =
                        $" --path.home \"{beatDataPath}\"" +
                        $" -E logging.files.redirect_stderr=true",

                    DelayedAutoStart = true,
                    Start = SvcStartType.auto,

                    // Don't start on install, config file is likely not ready yet
                    //StartOn = SvcEvent.Install,

                    StopOn = SvcEvent.InstallUninstall_Wait,
                    RemoveOn = SvcEvent.Uninstall_Wait,
                };
            }

            var packageContents = new List<WixEntity>
            {
                new DirFiles(Path.Combine(opts.PackageInDir, MagicStrings.Files.All), path =>
                {
                    var itm = path.ToLower();

                    bool exclude = 

                        // configuration will go into mutable location
                        itm.EndsWith(MagicStrings.Ext.DotYml) ||

                        // we install/remove service ourselves
                        itm.EndsWith(MagicStrings.Ext.DotPs1) ||

                        // .exe must be excluded for service configuration to work
                        (pc.IsWindowsService && itm.EndsWith(exeName))
                    ;

                    // this is an "include" filter
                    return ! exclude;
                })
            };

            packageContents.AddRange(
                new DirectoryInfo(opts.PackageInDir)
                    .GetDirectories()
                    .Select(dir => dir.Name)
                    .Except(pc.MutableDirs)
                    .Select(dirName =>
                        new Dir(dirName, new Files(Path.Combine(opts.PackageInDir, dirName, MagicStrings.Files.All)))));

            packageContents.Add(pc.IsWindowsService ? service : null);

            var dataContents = new DirectoryInfo(opts.PackageInDir)
                .GetFiles(MagicStrings.Files.AllDotYml, SearchOption.TopDirectoryOnly)
                .Select(fi =>
                {
                    var wf = new WixSharp.File(fi.FullName);

                    // rename main config file to hide it from MSI engine and keep customizations
                    if (string.Compare(fi.Name, beatName + MagicStrings.Ext.DotYml, true) == 0)
                        wf.Attributes.Add("Name", beatName + ".example" + MagicStrings.Ext.DotYml);

                    return wf;
                })
                .ToList<WixEntity>();

            dataContents.AddRange(
                pc.MutableDirs
                    .Select(dirName =>
                    {
                        var dirPath = Path.Combine(opts.PackageInDir, dirName);
                        return Directory.Exists(dirPath)
                            ? new Dir(dirName, new Files(Path.Combine(dirPath, MagicStrings.Files.All)))
                            : null;
                    })
                    .Where(dir => dir != null));

            // Drop CLI shim on disk
            var cliShimScriptPath = Path.Combine(opts.PackageOutDir, MagicStrings.Files.ProductCliShim(beatName));
            System.IO.File.WriteAllText(cliShimScriptPath, Resources.GenericCliShim);

            var beatsInstallPath = Path.Combine(
                $"[ProgramFiles{(ap.Is64Bit ? "64" : string.Empty)}Folder]",
                companyName,
                productSetName);

            project.Dirs = new[]
            {
                // Binaries
                new InstallDir(
                    beatsInstallPath,
                    new Dir(
                        ap.Version,
                        new Dir(beatName, packageContents.ToArray()),
                        new WixSharp.File(cliShimScriptPath))),

                // Configration and logs
                new Dir(beatDataPath, dataContents.ToArray())
            };

            // CLI Shim path
            project.Add(new EnvironmentVariable("PATH", Path.Combine(beatsInstallPath, ap.Version))
            {
                Part = EnvVarPart.last
            });

            // We hard-link Wix Toolset to a known location
            Compiler.WixLocation = Path.Combine(opts.BinDir, "WixToolset", "bin");
            Compiler.PreserveTempFiles = true;

            project.ResolveWildCards();

            if (opts.WxsOnly)
                project.BuildWxs();
            else
                Compiler.BuildMsi(project);
        }
    }
}
