using System;
using ElastiBuild.Commands;
using Elastic.Installer;

namespace ElastiBuild.Extensions
{
    public static class BuildContextExtensions
    {
        public static void UseArtifactPackage(this BuildContext ctx, ArtifactPackage ap) =>
            ctx.Items.Add(nameof(ArtifactPackage), ap);

        public static ArtifactPackage GetArtifactPackage(this BuildContext ctx)
        {
            if (ctx.Items.TryGetValue(nameof(ArtifactPackage), out var obj) && obj is ArtifactPackage ap)
                return ap;

            throw new Exception(
                $"Unable to get {nameof(ArtifactPackage)} instance from " +
                $"{nameof(BuildContext)}.{nameof(BuildContext.Items)}");
        }

        public static void UseCertificate(this BuildContext ctx, string certFile, string certPass)
        {
            ctx.Items.Add(nameof(ISupportCodeSigning.CertFile), certFile);
            ctx.Items.Add(nameof(ISupportCodeSigning.CertPass), certPass);
        }

        public static (string certFile, string certPass) GetCertificate(this BuildContext ctx)
        {
            if (ctx.Items.TryGetValue(nameof(ISupportCodeSigning.CertFile), out var obj1)
                && obj1 is string certFile
                && ctx.Items.TryGetValue(nameof(ISupportCodeSigning.CertPass), out var obj2)
                && obj2 is string certPass)
            {
                return (certFile, certPass);
            }

            throw new Exception(
                $"Unable to get certificate information from " +
                $"{nameof(BuildContext)}.{nameof(BuildContext.Items)}");
        }
    }
}
