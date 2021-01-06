namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// A VFXOutputEventAbstractHandler is an API helper that hooks into an Output Event to allow you to execute scripts based on the event.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public abstract class VFXOutputEventAbstractHandler : MonoBehaviour
    {
        /// <summary>
        /// Returns false if this output event handler can only be executed in play mode or runtime.
        /// </summary>
        public abstract bool canExecuteInEditor { get; }

        /// <summary>
        /// Property to enable or disable the execution in editor.
        /// </summary>
        public bool executeInEditor = true;

        /// <summary>
        /// The name of the output event to catch.
        /// </summary>
        public ExposedProperty outputEvent = "On Received Event";

        /// <summary>
        /// The VisualEffect emitter of the output event.
        /// </summary>
        protected VisualEffect m_VisualEffect { private set; get; }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_VisualEffect = GetComponent<VisualEffect>();
            if (m_VisualEffect != null)
                m_VisualEffect.outputEventReceived += OnOutputEventRecieved;
        }

        /// <summary>
        /// This function is called when the behavior becomes disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (m_VisualEffect != null)
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

        /// <summary>
        /// This function is called when the specified event in outputEvent on the attached VisualEffect is triggered.
        /// The VFXEventAttribute passed as parameter is temporary and can be modified in a later process.
        /// </summary>
        /// <param name="eventAttribute">The VFXEventAttribute handling properties from the spawn event.</param>
        public abstract void OnVFXOutputEvent(VFXEventAttribute eventAttribute);
    }
}
