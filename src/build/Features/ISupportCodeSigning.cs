using CommandLine;

namespace ElastiBuild.Commands
{
    public interface ISupportCodeSigning
    {
        [Option("cert-file", Default = "", HelpText = "Path to a file containing certificate")]
        string CertFile { get; set; }

        [Option("cert-pass", Default = "", HelpText = "Certificate password: path to a file containing a password or name of environment variable")]
        string CertPass { get; set; }
    }
}
