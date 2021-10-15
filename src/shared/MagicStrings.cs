namespace Elastic.Installer
{
    public static class MagicStrings
    {
        public const string AppAlias = "./build";

        public static readonly string Elastic = "Elastic";

        public static class Beats
        {
            public static readonly string Name = "Beats";

            public static readonly string Description =
                "Beats is the platform for single-purpose data shippers. They send data " +
                "from hundreds or thousands of machines and systems to Logstash or Elasticsearch.";

            public static readonly string CompilerName = "BeatPackageCompiler";
        }

        public static class Ext
        {
            public static readonly string DotAll = ".*";
            public static readonly string DotExe = ".exe";
            public static readonly string DotYaml = ".yaml";
            public static readonly string DotYml = ".yml";
            public static readonly string DotIco = ".ico";
            public static readonly string DotPs1 = ".ps1";
            public static readonly string DotMsi = ".msi";
            public static readonly string DotCmd = ".cmd";
            public static readonly string DotZip = ".zip";
        }

        public static class Files
        {
            public static readonly string All = "*" + Ext.DotAll;
            public static readonly string AllDotYml = "*" + Ext.DotYml;
            public static readonly string AllDotZip = "*" + Ext.DotZip;
            public static readonly string ConfigYaml = "config.yaml";
            public static readonly string LicenseTxt = "LICENSE.txt";
            public static readonly string TopBannerBmp = "topbanner.bmp";
            public static readonly string LeftBannerBmp = "leftbanner.bmp";
            public static readonly string BuildRoot = ".buildroot";
            public static readonly string SignToolExe = "signtool.exe";
            public static readonly string DashOssSuffix = "-oss";

            public static readonly string LicenseRtf = "LICENSE.rtf";
            public static string PackageLicenseRtf(string packageName) =>
                packageName + "." + LicenseRtf;

            public static string ProductCliShim(string productName) =>
                productName + Ext.DotCmd;
        }

        public static class Dirs
        {
            public static readonly string Src = "src";
            public static readonly string Bin = "bin";
            public static readonly string In = "in";
            public static readonly string Out = "out";
            public static readonly string Tools = "tools";
            public static readonly string Installer = "installer";
            public static readonly string Resources = "resources";
            public static readonly string Config = "config";
            public static readonly string Compiler = "compiler";
            public static readonly string Cert = "cert";
        }

        public static class Services
        {
            public static readonly string Tcpip = "Tcpip";
            public static readonly string Dnscache = "Dnscache";
        }

        public static class ArtifactsApi
        {
            public static readonly string BaseAddress = "https://artifacts-api.elastic.co/v1/";
            public static readonly string Architecture = "architecture";
            public static readonly string Branches = "branches";
            public static readonly string Versions = "versions";
            public static readonly string Aliases = "aliases";
            public static readonly string Url = "url";
        }

        public static class Arch
        {
            public static readonly string x86_64 = "x86_64";
        }

        public static class Ver
        {
            public static readonly string Snapshot = "SNAPSHOT";
        }

        public static class Content
        {
            public static string WrapWithRtf(string content) =>
                @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033" +
                @"{\fonttbl{\f0\fnil\fcharset0 Tahoma;}}" +
                @"{\viewkind4\uc1\pard\sa200\sl276\slmult1\f0\fs18\lang9 " +
                content
                    .Replace("\r\n\r\n", "\n\n")
                    .Replace("\n\n", @"\par" + "\r\n") +
                @"\par}";
        }

        public static class Errors
        {
            public static readonly string NeedCidWhenTargetSpecified = @$"
Need --cid when PRODUCT specified. To discover container IDs run:
    {AppAlias} discover containers

Run with --help for more information.";

            public static readonly string NewerVersionInstalled =
                "A more recent version is already installed, " +
                "please uninstall before continuing.";
        }
    }
}
