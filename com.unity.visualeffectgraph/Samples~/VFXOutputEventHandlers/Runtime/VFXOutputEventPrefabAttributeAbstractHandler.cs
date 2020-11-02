namespace UnityEngine.VFX.Utility
{
    public abstract class VFXOutputEventPrefabAttributeAbstractHandler : MonoBehaviour
    {
        public abstract void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect);
    }
}
