namespace UnityEngine.VFX.Utility
{
    public abstract class VFXOutputEventPrefabAttributeHandler : MonoBehaviour
    {
        public abstract void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect);
    }
}