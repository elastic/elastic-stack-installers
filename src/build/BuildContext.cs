using System;
using System.Collections.Generic;
using System.IO;
using ElastiBuild.Options;
using Elastic.Installer;

namespace ElastiBuild
{
    public class BuildContext : CommonPathsProvider
    {
        public static BuildContext Default => lazyContext.Value;
        static readonly Lazy<BuildContext> lazyContext = new Lazy<BuildContext>();

        public ElastiBuildConfig Config => lazyBuildConfig.Value;
        static readonly Lazy<ElastiBuildConfig> lazyBuildConfig = new Lazy<ElastiBuildConfig>(
            () => BuildConfiguration.Read(Path.Combine(lazyConfigDir.Value, MagicStrings.Files.ConfigYaml)));

        public GlobalOptions Options { get; private set; }

        public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();
    }
}
