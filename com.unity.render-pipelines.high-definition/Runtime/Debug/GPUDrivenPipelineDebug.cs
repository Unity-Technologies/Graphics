using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Decal debug settings.
    /// </summary>
    [Serializable]
    public class GPUDrivenPipelineDebugSettings
    {
        public float maxRange = 100.0f;
        static public Vector4[] color = new Vector4[7]
                    {
                        Color.red,
                        Color.green,
                        Color.blue,
                        Color.cyan,
                        Color.gray,
                        Color.magenta,
                        Color.yellow
                    };
    }
}
