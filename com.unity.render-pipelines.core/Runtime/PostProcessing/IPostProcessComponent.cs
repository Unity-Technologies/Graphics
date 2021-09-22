namespace UnityEngine.Rendering
{
    public interface IPostProcessComponent
    {
        bool IsActive();
        bool IsTileCompatible()
        {
            return false;
        }
    }
}
