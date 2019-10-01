using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixSharp;
using SharpYaml.Serialization;
using Elastic.Installer.Shared;

namespace Elastic.Installer.Beats
{
    public class BeatPackageCompiler
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);

            Directory.CreateDirectory(opts.OutDir);

            var package = new ElastiBuild.ArtifactPackage(opts.PackageName);

            var companyName = "Elastic";
            var productSetName = "Beats";
            var displayName = "Beats " + package.TargetName;
            var serviceName = package.TargetName;
            var fileName = package.TargetName + ".exe";

            BeatInfo bi = null;

            var fname = Path.Combine(opts.SharedDir, "config.yaml");
            using (var yamlConfig = System.IO.File.OpenRead(fname))
            {
                var ser = new Serializer();
                var yaml = ser.Deserialize<Dictionary<string, BeatInfo>>(yamlConfig);

                if (!yaml.TryGetValue(package.TargetName, out bi))
                    throw new ArgumentException($"Unable to find {package.TargetName} section in {fname}");
            }

            // TODO: validate/process Product Id
            //       bi.KnownVersions

            var project = new Project(displayName)
            {
                // This GUID *must* be stable and unique per-beat
                GUID = bi.UpgradeCode,

                Name = $"{displayName} {package.SemVer} ({package.Architecture})",

                Description = bi.Description,

                OutFileName = Path.Combine(opts.OutDir, opts.PackageName),
                Version = new Version(package.Version),

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(opts.OutDir, "LICENSE.rtf"),

                Platform = package.Is32bit ? Platform.x86 : Platform.x64,

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

            project.ControlPanelInfo = new ProductInfo
            {
                Contact = companyName,
                Manufacturer = companyName,
                UrlInfoAbout = "https://www.elastic.co/downloads/beats",

                Comments = bi.Description + 
                           ". Beats is the platform for single-purpose data shippers. They send data " +
                           "from hundreds or thousands of machines and systems to Logstash or Elasticsearch.",

                ProductIcon = Path.Combine(
                    opts.ResDir,
                    Path.GetFileNameWithoutExtension(fileName) + ".ico"),
            };

            // Convert LICENSE.txt to something richedit control can render
            System.IO.File.WriteAllText(
                Path.Combine(opts.OutDir, "LICENSE.rtf"),
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033" +
                @"{\fonttbl{\f0\fnil\fcharset0 Tahoma;}}" +
                @"{\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs18\lang9 " +
                System.IO.File
                    .ReadAllText(Path.Combine(opts.InDir, "LICENSE.txt"))
                    .Replace("\r\n\r\n", "\n\n")
                    .Replace("\n\n", @"\par" + "\r\n") +
                @"\par}");

            var installSubPath = $@"{companyName}\{package.Version}\{productSetName}\{displayName}";

            WixSharp.File service = null;
            if (bi.IsWindowsService)
            {
                service = new WixSharp.File(Path.Combine(opts.InDir, fileName));

                // TODO: CNDL1150 : ServiceConfig functionality is documented in the Windows Installer SDK to 
                //                  "not [work] as expected." Consider replacing ServiceConfig with the 
                //                  WixUtilExtension ServiceConfig element.

                service.ServiceInstaller = new ServiceInstaller
                {
                    Interactive = false,

                    Name = serviceName,
                    DisplayName = $"{displayName} {package.SemVer}",
                    Description = bi.Description,
                    DependsOn = new[] { new ServiceDependency("Tcpip") },

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
                new DirFiles(opts.InDir + @"\*.*", path =>
                {
                    var itm = path.ToLower();

                    bool include = !(
                        // configuration will go into mutable location
                        itm.EndsWith("yml") ||

                        // we install/remove service ourselves
                        itm.EndsWith("ps1") ||

                        // .exe must be excluded for service configuration to work
                        (bi.IsWindowsService ? itm.EndsWith(fileName) : false)
                    );

                    return include;
                })
            };

            elements.AddRange(
                new DirectoryInfo(opts.InDir)
                    .GetDirectories()
                    .Select(dirName => dirName.Name)
                    .Except(bi.MutableDirs)
                    .Select(dirName => new Dir(dirName, new Files(Path.Combine(opts.InDir, dirName) + @"\*.*"))));

            elements.Add(bi.IsWindowsService ? (WixEntity)service : new DummyEntity());

            var mainInstallDir = new InstallDir(
                $@"ProgramFiles{(package.Is64Bit ? "64" : string.Empty)}Folder\{installSubPath}",
                elements.ToArray());

            // TODO: evaluate adding metadata file into beats repo that lists these per-beat
            var mutablePaths = new List<WixEntity>
            {
                new DirFiles(opts.InDir + @"\*.yml")
            };

            // These are the directories that we know of
            mutablePaths.AddRange(
                bi.MutableDirs
                    .Select(dirName =>
                    {
                        var dirPath = Path.Combine(opts.InDir, dirName);
                        return Directory.Exists(dirPath)
                            ? new Dir(dirName, new Files(dirPath + @"\*.*"))
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
