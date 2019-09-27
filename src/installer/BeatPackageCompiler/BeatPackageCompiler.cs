using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixSharp;
using SharpYaml.Serialization;
using Elastic.Installer.Shared;

namespace Elastic.Installer.Beats
{
    public class BeatInfo
    {
        [YamlMember("upgrade_code")]
        public Guid UpgradeCode { get; set; }

        [YamlMember("known_versions")]
        public Dictionary<string, Guid> KnownVersions { get; set; }
    }

    public class BeatPackageCompiler
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);

            Directory.CreateDirectory(opts.OutDir);

            var package = new ElastiBuild.ArtifactPackage(opts.PackageName, string.Empty);

            var companyName = "Elastic";
            var productSetName = "Beats";
            var displayName = package.TargetName;
            var serviceName = package.TargetName;
            var fileName = package.TargetName + ".exe";

            // TODO: ping beats team to tell them we depend on this
            var beatDescription = System.IO.File
                .ReadAllLines(Path.Combine(opts.InDir, "README.md"))
                .Skip(2)
                .Take(1)
                .First();

            var project = new Project(displayName)
            {
                Name = $"{displayName} {package.SemVer} ({package.Architecture})",

                Description = beatDescription,

                OutFileName = Path.Combine(opts.OutDir, opts.PackageName),
                Version = new Version(package.Version),

                // We massage LICENSE.txt into .rtf below
                LicenceFile = Path.Combine(opts.OutDir, "LICENSE.rtf"),

                // TODO: More robust test
                Platform = package.Architecture == "x86" ? Platform.x86 : Platform.x64,

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

            var fname = Path.Combine(opts.SharedDir, "beats-guids.yaml");
            using (var guidsYaml = System.IO.File.OpenRead(fname))
            {
                var ser = new Serializer();
                var yaml = ser.Deserialize<Dictionary<string, BeatInfo>>(guidsYaml);

                if (!yaml.TryGetValue(package.TargetName, out BeatInfo bi))
                    throw new ArgumentException($"Unable to find {package.TargetName} section in {fname}");

                // This GUID must be unique per-beat
                project.GUID = bi.UpgradeCode;

                // TODO: validate/proces Product Id
            }

            project.ControlPanelInfo = new ProductInfo
            {
                Contact = companyName,
                Manufacturer = companyName,
                UrlInfoAbout = "https://www.elastic.co/downloads/beats",

                Comments = "Beats is the platform for single-purpose data shippers. They send data " +
                           "from hundreds or thousands of machines and systems to Logstash or Elasticsearch.",

                // TODO: Beat specific icon
                ProductIcon = Path.Combine(opts.ResDir, Path.GetFileNameWithoutExtension(fileName) + ".ico"),
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
                    $" -E logging.files.redirect_stderr=true",

                DelayedAutoStart = true,
                Start = SvcStartType.auto,

                // Don't start on install, config file is likely not ready yet
                //StartOn = SvcEvent.Install,

                StopOn = SvcEvent.InstallUninstall_Wait,
                RemoveOn = SvcEvent.Uninstall_Wait,
            };

            var mutableDirs = new List<WixEntity>
            {
                new DirFiles(opts.InDir + @"\*.yml")
            };

            // TODO: evaluate adding metadata file into beats repo that lists these per-beat

            // These are the directories that we know of
            mutableDirs.AddRange(
                "kibana|module|modules.d|monitors.d"
                    .Split('|')
                    .Select(dirName =>
                    {
                        var dirPath = Path.Combine(opts.InDir, dirName);
                        return Directory.Exists(dirPath)
                            ? new Dir(dirName, new Files(dirPath + @"\*.*"))
                            : null;
                    })
                    .Where(dir => dir != null));

            var mutableInstallDir = new Dir(
                $@"CommonAppDataFolder\{installSubPath}",
                mutableDirs.ToArray());

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
