namespace UnityEngine.Rendering.HighDefinition
{
    // Caution: keep in sync enum in HDProfileId in HDProfileId.cs
    /// <summary> Buffers available in HDRP </summary>
    public enum AOVBuffers
    {
        /// <summary>Color buffer that will be used at the end, include post processes.</summary>
        Output,
        /// <summary>Color buffer that will be used before post processes.</summary>
        Color,
        /// <summary>DepthStencil buffer at the end of the frame.</summary>
        DepthStencil,
        /// <summary>Normals (world space) buffer at the end of the frame.</summary>
        Normals,
        /// <summary>Motion vectors buffer at the end of the frame.</summary>
        MotionVectors
    }

    /// <summary>
    /// Describes the type of custom pass buffer that will be exported with the AOV API.
    /// </summary>
    public class CustomPassAOVBuffers
    {
        /// <summary> Specifies which output type to export.</summary>
        public enum OutputType
        {
            /// <summary> The custom pass buffer will be exported.</summary>
            CustomPassBuffer,
            /// <summary> The color buffer of the camera will be exported.</summary>
            Camera
        }

        /// <summary> The injection point of the custom passes that will be exported. </summary>
        public CustomPassInjectionPoint injectionPoint = CustomPassInjectionPoint.BeforeRendering;
        /// <summary> Specifies which output type to export.</summary>
        public OutputType outputType = OutputType.CustomPassBuffer;

        /// <summary>
        /// Constructor for CustomPassAOVBuffers
        /// </summary>
        /// <param name="injectionPoint"> The injection point of the custom passes that will be exported. </param>
        /// <param name="outputType"> The buffer type to export at the scpecified injection point. </param>
        public CustomPassAOVBuffers(CustomPassInjectionPoint injectionPoint, OutputType outputType)
        {
            this.injectionPoint = injectionPoint;
            this.outputType = outputType;
        }
    }
}
