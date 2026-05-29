#if URP_SCREEN_SPACE_REFLECTION
namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Color history used by Screen Space Reflection temporal accumulation.
    /// </summary>
    internal sealed class ScreenSpaceReflectionColorHistory : ColorHistory
    {
        /// <inheritdoc />
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            m_Names[0] = "ScreenSpaceReflectionColorHistory0";
            m_Names[1] = "ScreenSpaceReflectionColorHistory1";
            base.OnCreate(owner, typeId);
        }
    }
}
#endif
