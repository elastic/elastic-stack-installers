using System;
using System.IO;
using Elastic.Installer.Shared;
using WixSharp;

namespace Elastic.Installer.Beats
{
    public class Program
    {
        static void Main(string[] args_)
        {
            var opts = CmdLineOptions.Parse(args_);
            var srcdir = Path.Combine(opts.BuildRoot, "bin", "in", opts.PackageDir);
            var outdir = Path.Combine(opts.BuildRoot, "bin", "out", opts.PackageDir);
            var resourceDir = Path.Combine(opts.BuildRoot, "src", "installer", "shared", "resources");

            Directory.CreateDirectory(outdir);

            var project = new Project("Winlog Beat")
            {
                //Name = "Winlog Beat", //{bitness}
                Description = "...",
                OutFileName = Path.Combine(outdir, "winlogbeat-setup-v0.0.0.0"),
                Version = new Version("0.0.0.0"),
                ControlPanelInfo = new ProductInfo
                {
                    Manufacturer = "Elastic",
                },

                // TODO: x64/x86
                Platform = Platform.x64,

                // TODO: x64/x86
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

                Dirs = new[]
                {
                    new Dir("ProgramFiles64Folder")
                    {
                        Dirs = new []
                        {
                            new Dir("Elastic")
                            {
                                Dirs = new []
                                {
                                    new Dir("0.0.0")
                                    {
                                        Dirs = new[]
                                        {
                                            new Dir("Beats")
                                            {
                                                Dirs = new[]
                                                {
                                                    new Dir(new Id("INSTALLDIR"), "WinlogBeat", new Files(srcdir + @"\*.*"))
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            };

            Compiler.PreserveTempFiles = true;
            //Compiler.BuildMsiCmd(project, Path.Combine(outdir, "compile.cmd"));
            Compiler.BuildMsi(project);
        }
    }
}
