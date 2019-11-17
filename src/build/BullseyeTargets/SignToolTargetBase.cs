using ElastiBuild.Extensions;

namespace ElastiBuild.BullseyeTargets
{
    public abstract class SignToolTargetBase<T> : BullseyeTargetBase<T>
    {
        protected static (string certPass, string SignToolArgs) MakeSignToolArgs(
            BuildContext ctx, string target)
        {
            var pc = ctx.Config.GetProductConfig(target);
            var (certFile, certPass) = ctx.GetCertificate();

            return (
                certPass,
                string.Join(' ',
                    "sign",
                    "/v",
                    "/tr", ctx.Config.TimestampUrl.Quote(),
                    "/d", pc.PublishedName.Quote(),
                    "/du", pc.PublishedUrl,
                    "/f", certFile.Quote(),
                    "/p", certPass.Quote(),

                    // extra space before binary name
                    string.Empty)
                );
        }
    }
}
