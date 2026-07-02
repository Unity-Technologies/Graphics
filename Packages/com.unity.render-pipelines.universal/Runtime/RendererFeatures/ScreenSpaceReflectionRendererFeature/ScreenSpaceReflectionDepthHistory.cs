namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Depth history used by Screen Space Reflection feature. Contains transparents if transparency support is enabled.
    /// </summary>
    internal sealed class ScreenSpaceReflectionDepthHistory : DepthHistory
    {
        /// <inheritdoc />
        public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
        {
            m_Names[0] = "ScreenSpaceReflectionDepthHistory0";
            m_Names[1] = "ScreenSpaceReflectionDepthHistory1";
            base.OnCreate(owner, typeId);
        }
    }
}
