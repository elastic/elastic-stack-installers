using System.IO;
using Elastic.Installer;
using NUnit.Framework;

namespace QA.Core
{
    [TestFixture]
    public class TestBase
    {
        protected static PathsProvider pathsProvider = new PathsProvider();
        protected static ElastiBuildConfig BuildConfig = BuildConfiguration.Read(
            Path.Combine(pathsProvider.ConfigDir, MagicStrings.Files.ConfigYaml));

        [OneTimeSetUp]
        public void InitialSetup()
        {
            // Remove test results directory if present
            if (Directory.Exists(pathsProvider.TestResultsDir))
                Directory.Delete(pathsProvider.TestResultsDir, true);

            // Initialize test result dir
            Directory.CreateDirectory(pathsProvider.TestResultsDir);
            Directory.CreateDirectory(pathsProvider.TestLogsDir);
        }
    }
}
