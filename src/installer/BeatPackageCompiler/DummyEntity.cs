using WixSharp;

namespace Elastic.Installer.Beats
{
    public class DummyEntity : WixEntity, IGenericEntity
    {
        public void Process(ProcessingContext context)
        {
            // Dummy
        }
    }
}
