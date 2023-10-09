#if UNITY_EDITOR
namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface to define an stripper for a <see cref="IRenderPipelineGraphicsSettings"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRenderPipelineGraphicsSettingsStripper<in T> : IStripper
        where T : IRenderPipelineGraphicsSettings
    {
        /// <summary>
        /// Specifies if a <see cref="IRenderPipelineGraphicsSettings"/> can be stripped from the build
        /// </summary>
        /// <param name="settings">The settings that will be stripped</param>
        /// <returns>true if the setting is not used and can be stripped</returns>
        public bool CanRemoveSettings(T settings);
    }
}
#endif
