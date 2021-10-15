using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using Elastic.Installer;
using NUnit.Framework;
using QA.Core;

namespace QA
{
    public class BeatsMSI : TestBase
    {
        static IEnumerable Installers()
        {
            foreach (string productName in BuildConfig.ProductNames)
            {
                ProductConfig productConfig = BuildConfig.Products[productName];
                IEnumerable<string> installerPaths = Directory
                    .EnumerateFiles(pathsProvider.OutDir, $"{productName}-*.msi", SearchOption.AllDirectories)
                    .Where(filename => new Regex(@$"{productName}-\d.*\.msi").IsMatch(filename));

                foreach (string installerPath in installerPaths)
                {
                    MSIInstaller installer = new MSIInstaller(productName, productConfig, installerPath, pathsProvider.TestLogsDir);

                    TestCaseData td = new TestCaseData(installer);
                    td.SetName($"{productName} ({installerPath})");

                    yield return td;
                }
            }
        }

        [Test, TestCaseSource(nameof(Installers))]
        public void SilentInstallerTest(MSIInstaller installer)
        {
            string actualProductName = installer.ProductName.Replace("-oss", "");

            // Install
            TestContext.Out.WriteLine($"Installing {actualProductName}...");
            int installExitCode = installer.Install().ExitCode;

            TestContext.Out.WriteLine($" -> installation finished with exit code {installExitCode}");
            Assert.That(installExitCode, Is.EqualTo(0), "Product installed successfully (exit code 0)");

            // TODO Enable immutable dir check once stack version value is determined
            //string immutablePath = Path.Combine(pathsProvider.ElasticImmutableRoot, "Beats", stackVersion, actualProductName);
            //TestContext.Out.WriteLine($" -> checking immutable path exists: {immutablePath}");
            //DirectoryAssert.Exists(immutablePath, $"Immutable path {immutablePath} exists after installation");

            try
            {
                foreach (string mutableDir in installer.ProductConfig.MutableDirs)
                {
                    string mutablePath = Path.Combine(pathsProvider.ElasticMutableRoot, "Beats", actualProductName, mutableDir);
                    TestContext.Out.WriteLine($" -> checking mutable path exists: {mutablePath}");
                    DirectoryAssert.Exists(mutablePath, $"Mutable path {mutablePath} exists after installation");
                }

                ServiceController service = ServiceController.GetServices().FirstOrDefault(service => service.ServiceName == actualProductName);
                TestContext.Out.WriteLine($" -> checking service '{actualProductName}' exists");
                Assert.That(service, Is.Not.Null, $"Service named '{actualProductName}' exists");
            }
            finally
            {
                // Uninstall
                TestContext.Out.WriteLine($"Uninstalling {actualProductName}...");
                int uninstallExitCode = installer.Uninstall().ExitCode;

                TestContext.Out.WriteLine($" -> uninstall finished with exit code {uninstallExitCode}.");
                Assert.That(uninstallExitCode, Is.EqualTo(0), "Product uninstalled successfully (exit code 0)");

                // TODO Enable immutable dir check once stack version value is determined
                //TestContext.Out.WriteLine($" -> checking immutable path no longer exists: {immutablePath}");
                //DirectoryAssert.Exists(immutablePath, $"Immutable path {immutablePath} no longer exists after uninstall");

                foreach (string mutableDir in installer.ProductConfig.MutableDirs)
                {
                    string mutablePath = Path.Combine(pathsProvider.ElasticMutableRoot, "Beats", actualProductName, mutableDir);
                    TestContext.Out.WriteLine($" -> checking mutable path no longer exists: {mutablePath}");
                    DirectoryAssert.DoesNotExist(mutablePath, $"Mutable path {mutablePath} no longer exists after uninstall");
                }

                ServiceController service = ServiceController.GetServices().FirstOrDefault(service => service.ServiceName == actualProductName);
                TestContext.Out.WriteLine($" -> checking service '{actualProductName}' no longer exists");
                Assert.That(service, Is.Null, $"Service named '{actualProductName}' no longer exists");
            }

            TestContext.Out.WriteLine($"[PASSED] {installer.ProductName} MSI silent install/uninstall");
        }
    }
}
