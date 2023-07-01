#if UNITY_EDITOR
namespace UnityEngine.Rendering
{
    public interface IStripper
    {
        /// <summary>
        /// Returns if the stripper is active
        /// </summary>
        bool active { get; }
    }
}
#endif
