using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Default instance of a ImportanceSamplers
    /// </summary>
    public static class ImportanceSamplers
    {
        static ImportanceSamplersSystem s_DefaultInstance = new ImportanceSamplersSystem();

        /// <summary>
        /// Check if an Importance Sampling exist (generated or schedule for generation).
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public static bool Exist(int identifier)
        {
            return s_DefaultInstance.Exist(identifier);
        }

        /// <summary>
        /// Check if an Importance Sampling exist & ready (generated or schedule for generation).
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public static bool ExistAndReady(int identifier)
        {
            return s_DefaultInstance.ExistAndReady(identifier);
        }

        /// <summary>
        /// Getter for marginal textures.
        /// </summary>
        /// <param name="identifier">Unique ID to identify the marginals.</param>
        public static ImportanceSamplersSystem.MarginalTextures GetMarginals(int identifier)
        {
            return s_DefaultInstance.GetMarginals(identifier);
        }

        /// <summary>
        /// Schedule generation of the marginal textures. return if the task was scheduled
        /// </summary>
        /// <param name="identifier">Unique ID to identify this scheduling.</param>
        /// <param name="pdfTexture">Texture2D or CubeMap which used for the the generation of the important sampling.</param>
        /// <param name="buildHemisphere">if the pdfTexture is a Cubemap or a CubemapArray, buildHemisphere allow to enforce to build the marginals only for the Upper Hemisphere.</param>
        public static bool ScheduleMarginalGeneration(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            return s_DefaultInstance.ScheduleMarginalGeneration(identifier, pdfTexture, buildHemisphere);
        }

        /// <summary>
        /// Schedule generation of the marginal textures. Even if the identifier already exist (always return true)
        /// </summary>
        /// <param name="identifier">Unique ID to identify this scheduling.</param>
        /// <param name="pdfTexture">Texture2D or CubeMap which used for the the generation of the important sampling.</param>
        /// <param name="buildHemisphere">if the pdfTexture is a Cubemap or a CubemapArray, buildHemisphere allow to enforce to build the marginals only for the Upper Hemisphere.</param>
        public static bool ScheduleMarginalGenerationForce(int identifier, Texture pdfTexture, bool buildHemisphere = false)
        {
            return s_DefaultInstance.ScheduleMarginalGenerationForce(identifier, pdfTexture, buildHemisphere);
        }

        /// <summary>
        /// Schedule a release of Marginal Textures
        /// </summary>
        /// <param name="identifier">Unique ID to identify this Release.</param>
        public static bool ScheduleRelease(int identifier)
        {
            return s_DefaultInstance.InternalScheduleRelease(identifier);
        }

        /// <summary>
        /// Update the logics, done once per frame
        /// </summary>
        /// <param name="cmd">Command buffer provided to setup shader constants.</param>
        public static void Update(CommandBuffer cmd)
        {
            s_DefaultInstance.Update(cmd);
        }

        /// <summary>
        /// Get convenient ID to identify a texture in the Importance Sampling
        /// </summary>
        /// <param name="texture">Texture which we want to have an ID from.</param>
        /// <param name="buildHemisphere">Used if texture is a Cubemap.</param>
        public static int GetIdentifier(Texture texture, bool buildHemisphere = false)
        {
            if (texture == null)
                return -1;

            int hash = 23*texture.GetHashCode();
            hash = 23*hash + texture.updateCount.GetHashCode();

            if (texture.dimension == TextureDimension.Cube ||
                texture.dimension == TextureDimension.CubeArray)
                hash = 23*hash + buildHemisphere.GetHashCode();

            return hash;
        }
    }
}
