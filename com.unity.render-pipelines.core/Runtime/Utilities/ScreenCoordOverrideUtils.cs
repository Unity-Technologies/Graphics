namespace UnityEngine.Rendering
{
    /// <summary>
    /// Screen Coord Override Utility class.
    /// </summary>
    public class ScreenCoordOverrideUtils
    {
        const string k_ShaderKeyword = "SCREEN_COORD_OVERRIDE";

        /// <summary>
        /// Set the Screen Coord Override keyword globally using a Command Buffer.
        /// </summary>
        /// <param name="cmd">The command buffer.</param>
        /// <param name="state">The value of the keyword.</param>
        public static void SetKeyword(CommandBuffer cmd, bool state)
        {
            CoreUtils.SetKeyword(cmd, k_ShaderKeyword, state);
        }

        /// <summary>
        /// Set the Screen Coord Override keyword locally for this Compute Shader.
        /// </summary>
        /// <param name="computeShader">The compute shader</param>
        public static void EnableKeyword(ComputeShader computeShader)
        {
            computeShader.EnableKeyword(k_ShaderKeyword);
        }
    }
}
