using WixSharp;

namespace Elastic.PackageCompiler
{
    public class DummyEntity : WixEntity, IGenericEntity
    {
        public void Process(ProcessingContext context)
        {
            // Dummy
        }
    }
}
