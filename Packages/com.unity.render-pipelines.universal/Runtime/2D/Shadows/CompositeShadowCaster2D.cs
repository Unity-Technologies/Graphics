using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class for 2D composite shadow casters.
    /// </summary>
    [CoreRPHelpURL("2DShadows", "com.unity.render-pipelines.universal")]
    [AddComponentMenu("Rendering/2D/Composite Shadow Caster 2D")]
    [MovedFrom(false, "UnityEngine.Experimental.Rendering.Universal", "com.unity.render-pipelines.universal")]
    [ExecuteInEditMode]
    public class CompositeShadowCaster2D : ShadowCasterGroup2D
    {
        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected void OnEnable()
        {
            ShadowCasterGroup2DManager.AddGroup(this);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        protected void OnDisable()
        {
            ShadowCasterGroup2DManager.RemoveGroup(this);
        }
    }
}
