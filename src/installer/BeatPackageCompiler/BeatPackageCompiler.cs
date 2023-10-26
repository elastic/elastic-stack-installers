using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BeatPackageCompiler.Properties;
using ElastiBuild.Extensions;
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

            if (!ArtifactPackage.FromFilename(opts.PackageName, out var ap))
                throw new Exception("Unable to parse file name: " + opts.PackageName);

            var pc = config.GetProductConfig(ap.TargetName);

            var companyName = MagicStrings.Elastic;
            var productSetName = MagicStrings.Beats.Name;
            var displayName = !string.IsNullOrEmpty(pc.DisplayName) ? pc.DisplayName : MagicStrings.Beats.Name + " " + ap.TargetName;
            var exeName = ap.CanonicalTargetName + MagicStrings.Ext.DotExe;

            // Generate UUID v5 from product properties.
            // This UUID *must* be stable and unique between Beats.
            var upgradeCode = Uuid5.FromString(ap.CanonicalTargetName);

            var project = new Project(displayName)
            {
                InstallerVersion = 500,

                GUID = upgradeCode,

                Name = $"{displayName} {ap.SemVer} ({ap.Architecture})",

                Description = pc.Description,

                OutFileName = Path.Combine(opts.PackageOutDir, opts.PackageName),
                Version = new Version(ap.Version),

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(
                    opts.PackageOutDir,
                    MagicStrings.Files.PackageLicenseRtf(opts.PackageName)),

                Platform = Platform.x64,

                InstallScope = InstallScope.perMachine,

                UI = WUI.WixUI_Minimal,

                // TODO: Custom images?
                BannerImage = Path.Combine(opts.ResDir, MagicStrings.Files.TopBannerBmp),
                BackgroundImage = Path.Combine(opts.ResDir, MagicStrings.Files.LeftBannerBmp),

                MajorUpgrade = new MajorUpgrade
                {
                    AllowDowngrades = false,
                    AllowSameVersionUpgrades = false,
                    DowngradeErrorMessage = MagicStrings.Errors.NewerVersionInstalled,
                },
            };

            project.Include(WixExtension.UI);
            project.Include(WixExtension.Util);

            project.ControlPanelInfo = new ProductInfo
            {
                Contact = companyName,
                Manufacturer = companyName,
                UrlInfoAbout = "https://www.elastic.co",

                Comments = pc.Description + ". " + MagicStrings.Beats.Description,

                ProductIcon = Path.Combine(
                    opts.ResDir,
                    Path.GetFileNameWithoutExtension(exeName) + MagicStrings.Ext.DotIco),

                NoRepair = true,
            };

            // Convert LICENSE.txt to something richedit control can render
            System.IO.File.WriteAllText(
                Path.Combine(
                    opts.PackageOutDir,
                    MagicStrings.Files.PackageLicenseRtf(opts.PackageName)),
                MagicStrings.Content.WrapWithRtf(
                    System.IO.File.ReadAllText(
                        Path.Combine(opts.PackageInDir, MagicStrings.Files.LicenseTxt))));

            var beatConfigPath = "[CommonAppDataFolder]" + Path.Combine(companyName, productSetName, ap.CanonicalTargetName);
            var beatDataPath = Path.Combine(beatConfigPath, "data");
            var beatLogsPath = Path.Combine(beatConfigPath, "logs");

            var textInfo = new CultureInfo("en-US", false).TextInfo;

            string serviceDisplayName;
            if (ap.TargetName.ToLower().StartsWith(companyName.ToLower()))
            {
                serviceDisplayName = $"{textInfo.ToTitleCase(ap.TargetName)} {ap.SemVer}";
            }
            else
            {
                serviceDisplayName = $"{companyName} {textInfo.ToTitleCase(ap.TargetName)} {ap.SemVer}";
            }
            

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

                    Name = ap.CanonicalTargetName,
                    DisplayName = serviceDisplayName,
                    Description = pc.Description,

                    DependsOn = new[]
                    {
                        new ServiceDependency(MagicStrings.Services.Tcpip),
                        new ServiceDependency(MagicStrings.Services.Dnscache),
                    },

                    Arguments =
                        " --path.home " + ("[INSTALLDIR]" + Path.Combine(ap.Version, ap.CanonicalTargetName)).Quote() +
                        " --path.config " + beatConfigPath.Quote() +
                        " --path.data " + beatDataPath.Quote() +
                        " --path.logs " + beatLogsPath.Quote() +
                        " -E logging.files.redirect_stderr=true",

                    DelayedAutoStart = false,
                    Start = SvcStartType.auto,

                    // Don't start on install, config file is likely not ready yet
                    //StartOn = SvcEvent.Install,

                    StopOn = SvcEvent.InstallUninstall_Wait,
                    RemoveOn = SvcEvent.InstallUninstall_Wait,
                };
            }

            var packageContents = new List<WixEntity>
            {
                new DirFiles(Path.Combine(opts.PackageInDir, MagicStrings.Files.All), path =>
                {
                    var itm = path.ToLower();

                    bool exclude = 

                        // configuration will go into mutable location
                        itm.EndsWith(MagicStrings.Ext.DotYml, StringComparison.OrdinalIgnoreCase) ||

                        // we install/remove service ourselves
                        itm.EndsWith(MagicStrings.Ext.DotPs1, StringComparison.OrdinalIgnoreCase) ||

                        // .exe must be excluded for service configuration to work
                        (pc.IsWindowsService && itm.EndsWith(exeName, StringComparison.OrdinalIgnoreCase))
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
                        new Dir(
                            dirName,
                            new Files(Path.Combine(
                                opts.PackageInDir,
                                dirName,
                                MagicStrings.Files.All)))));

            packageContents.Add(pc.IsWindowsService ? service : null);

            // Add a note to the final screen and a checkbox to open the directory of .example.yml file
            var beatConfigExampleFileName = ap.CanonicalTargetName.Replace("-", "_") + ".example" + MagicStrings.Ext.DotYml;
            var beatConfigExampleFileId = beatConfigExampleFileName + "_" + (uint) beatConfigExampleFileName.GetHashCode32();

            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALTEXT",
                $"NOTE: Only Administrators can modify configuration files! We put an example configuration file " +
                $"in the data directory named {ap.CanonicalTargetName}.example.yml. Please copy this example file to " +
                $"{ap.CanonicalTargetName}.yml and make changes according to your environment. Once {ap.CanonicalTargetName}.yml " +
                $"is created, you can configure {ap.CanonicalTargetName} from your favorite shell (in an elevated prompt) " +
                $"and then start {serviceDisplayName} Windows service.\r\n"));

            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOX", "1"));
            project.AddProperty(new Property("WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT",
                $"Open {ap.CanonicalTargetName} data directory in Windows Explorer"));

            // We'll open the folder for now
            // TODO: select file in explorer window
            project.AddProperty(new Property(
                "WixShellExecTarget",
                $"[$Component.{beatConfigExampleFileId}]"));

            project.AddWixFragment("Wix/Product",
                XElement.Parse(@"
<CustomAction
    Id=""CA_SelectExampleYamlInExplorer""
    BinaryKey = ""WixCA""
    DllEntry = ""WixShellExec""
    Impersonate = ""yes""
/>"),
                XElement.Parse(@"
<UI>
    <Publish
        Dialog=""ExitDialog""
        Control=""Finish""
        Event=""DoAction"" 
        Value=""CA_SelectExampleYamlInExplorer"">WIXUI_EXITDIALOGOPTIONALCHECKBOX=1 and NOT Installed
    </Publish>
</UI>"));

            var dataContents = new DirectoryInfo(opts.PackageInDir)
                .GetFiles(MagicStrings.Files.AllDotYml, SearchOption.TopDirectoryOnly)
                .Select(fi =>
                {
                    var wf = new WixSharp.File(fi.FullName);

                    // rename main config file to hide it from MSI engine and keep customizations
                    if (string.Compare(
                        fi.Name,
                        ap.CanonicalTargetName + MagicStrings.Ext.DotYml,
                        StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        wf.Attributes.Add("Name", beatConfigExampleFileName);
                        wf.Id = new Id(beatConfigExampleFileId);
                    }

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
            var cliShimScriptPath = Path.Combine(
                opts.PackageOutDir,
                MagicStrings.Files.ProductCliShim(ap.CanonicalTargetName));

            System.IO.File.WriteAllText(cliShimScriptPath, Resources.GenericCliShim);

            var beatsInstallPath =
                $"[ProgramFiles{(ap.Is64Bit ? "64" : string.Empty)}Folder]" +
                Path.Combine(companyName, productSetName);

            project.Dirs = new[]
            {
                // Binaries
                new InstallDir(
                     // Wix# directory parsing needs forward slash
                    beatsInstallPath.Replace("Folder]", "Folder]\\"),
                    new Dir(
                        ap.Version,
                        new Dir(ap.CanonicalTargetName, packageContents.ToArray()),
                        new WixSharp.File(cliShimScriptPath))),

                // Configration and logs
                new Dir("[CommonAppDataFolder]",
                    new Dir(companyName,
                        new Dir(productSetName,
                            new Dir(ap.CanonicalTargetName, dataContents.ToArray())
                            {
                                GenericItems = new []
                                {
                                    /*
                                    This will *replace* ACL on the {beatname} directory:

                                    Directory tree:
                                        NT AUTHORITY\SYSTEM:(OI)(CI)F
                                        BUILTIN\Administrators:(OI)(CI)F
                                        BUILTIN\Users:(CI)R

                                    Files:
                                        NT AUTHORITY\SYSTEM:(ID)F
                                        BUILTIN\Administrators:(ID)F
                                    */

                                    new MsiLockPermissionEx(
                                        "D:PAI(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;CI;0x1200a9;;;BU)",
                                        ap.Is64Bit)
                                }
                            })))
            };

            // CLI Shim path
            project.Add(new EnvironmentVariable("PATH", Path.Combine(beatsInstallPath, ap.Version))
            {
                Part = EnvVarPart.last
            });

            // We hard-link Wix Toolset to a known location
            Compiler.WixLocation = Path.Combine(opts.BinDir, "WixToolset", "bin");

#if !DEBUG
            if (opts.KeepTempFiles)
#endif
            {
                Compiler.PreserveTempFiles = true;
            }

            if (opts.Verbose)
            {
                Compiler.CandleOptions += " -v";
                Compiler.LightOptions += " -v";
            }

            project.ResolveWildCards();

            if (opts.WxsOnly)
                project.BuildWxs();
            else if (opts.CmdOnly)
                Compiler.BuildMsiCmd(project, Path.Combine(opts.SrcDir, opts.PackageName) + ".cmd");
            else
                Compiler.BuildMsi(project);
        }
    }
}
