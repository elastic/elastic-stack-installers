using System;
using System.IO;
using Elastic.Installer;

namespace QA.Core
{
    public class PathsProvider : CommonPathsProvider
    {
        public string TestResultsDir => lazyTestResultsDir.Value;
        protected static readonly Lazy<string> lazyTestResultsDir = new Lazy<string>(
            () => Path.Combine(lazyBinDir.Value, Constants.Dirs.TestResults));

        public string TestLogsDir => lazyTestLogsDir.Value;
        protected static readonly Lazy<string> lazyTestLogsDir = new Lazy<string>(
            () => Path.Combine(lazyTestResultsDir.Value, Constants.Dirs.TestLogs));

        public string ElasticImmutableRoot => lazyElasticImmutableRoot.Value;
        protected static readonly Lazy<string> lazyElasticImmutableRoot = new Lazy<string>(
            () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Elastic"));

        public string ElasticMutableRoot => lazyElasticMutableRoot.Value;
        protected static readonly Lazy<string> lazyElasticMutableRoot = new Lazy<string>(
            () => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Elastic"));
    }
}
