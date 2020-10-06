namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public abstract class VFXOutputEventAbstractHandler : MonoBehaviour
    {
        public abstract bool canExecuteInEditor { get; }
        public bool executeInEditor = true;
        public ExposedProperty outputEvent = "On Received Event";

        protected VisualEffect m_VisualEffect { private set; get; }

        protected virtual void OnEnable()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
            if (m_VisualEffect != null)
                m_VisualEffect.outputEventReceived += OnOutputEventRecieved;
        }

        protected virtual void OnDisable()
        {
            if(m_VisualEffect != null)
                m_VisualEffect.outputEventReceived -= OnOutputEventRecieved;
        }

        void OnOutputEventRecieved(VFXOutputEventArgs args)
        {
            if (Application.isPlaying || (executeInEditor && canExecuteInEditor))
            {
                if (args.nameId == outputEvent)
                    OnVFXOutputEvent(args.eventAttribute);
            }
        }

        public abstract void OnVFXOutputEvent(VFXEventAttribute eventAttribute);

    }
}
