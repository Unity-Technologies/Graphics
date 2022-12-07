#define NOTIFICATION_VALIDATION

using System.Linq;

namespace UnityEditor.VFX.UI
{
    internal class VFXSystemController : Controller<VFXUI>
    {
        VFXContextController[] m_Contexts;

        public VFXSystemController(VFXUI model) : base(model)
        {
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
        }

        public string title { get; private set; }

        internal VFXContextController[] contexts
        {
            get => m_Contexts;
            set
            {
                m_Contexts = value;
                title = contexts.Length > 0 ? contexts[0].model.GetGraph().systemNames.GetUniqueSystemName(contexts[0].model.GetData()) : string.Empty;
            }
        }

        internal void SetTitle(string newTitle)
        {
            if (newTitle != title && contexts.Length > 0)
            {
                    var data = contexts.First().model.GetData();
                    if (data != null)
                    {
                        int index = newTitle.IndexOfAny(new char[] { '\r', '\n' });
                        data.title = index == -1 ? newTitle : newTitle.Substring(0, index);
                    }

                    title = newTitle;
                    data.owners.First().Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
    }
}
