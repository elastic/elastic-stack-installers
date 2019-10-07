using WixSharp;

namespace Elastic.PackageCompiler
{
    // TODO: remove, once https://github.com/oleg-shilo/wixsharp/pull/729 merged and released
    // WixSharp doesn't accept null in WixEntity[] params.
    public class DummyEntity : WixEntity, IGenericEntity
    {
        public void Process(ProcessingContext context_)
        {
            // Dummy
        }
    }
}
