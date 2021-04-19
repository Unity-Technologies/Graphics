namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Extensions to create new Lights in HDRP.
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        ///  Add a new HDRP Light to a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject on which the light is going to be added</param>
        /// <param name="lightTypeAndShape">The Type of the HDRP light to Add</param>
        /// <returns>The created HDRP Light component</returns>
        public static HDAdditionalLightData AddHDLight(this GameObject gameObject, HDLightTypeAndShape lightTypeAndShape)
        {
            var hdLight = gameObject.AddComponent< HDAdditionalLightData >();

            HDAdditionalLightData.InitDefaultHDAdditionalLightData(hdLight);

            // Reflector have been change to true by default in the UX, however to not break compatibility
            // with previous 2020.2 project that use light scripting we must keep reflector to false for scripted light
            hdLight.enableSpotReflector = false;
            hdLight.SetLightTypeAndShape(lightTypeAndShape);

            return hdLight;
        }

        /// <summary>
        /// Remove the HD Light components from a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject on which the light is going to be removed</param>
        public static void RemoveHDLight(this GameObject gameObject)
        {
            var light = gameObject.GetComponent< Light >();
            var hdLight = gameObject.GetComponent< HDAdditionalLightData >();

            // destroy light components in order
            CoreUtils.Destroy(hdLight);
            CoreUtils.Destroy(light);
        }

        // TODO: camera functions
    }
}
