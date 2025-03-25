using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volume debug settings.
    /// This variant is obsolete and kept only for not breaking user code. Use <see cref="IVolumeDebugSettings2"/> for all new usage.
    /// </summary>
    [Obsolete("This is not longer supported Please use DebugDisplaySettingsVolume. #from(6000.2)", false)]
    public interface IVolumeDebugSettings
    {
        /// <summary>Selected component.</summary>
        int selectedComponent { get; set; }

        /// <summary>Current camera to debug.</summary>
        Camera selectedCamera { get; }

        /// <summary>Returns the collection of registered cameras.</summary>
        IEnumerable<Camera> cameras { get; }

        /// <summary>Selected camera index.</summary>
        int selectedCameraIndex { get; set; }

        /// <summary>Selected camera volume stack.</summary>
        VolumeStack selectedCameraVolumeStack { get; }

        /// <summary>Selected camera volume layer mask.</summary>
        LayerMask selectedCameraLayerMask { get; }

        /// <summary>Selected camera volume position.</summary>
        Vector3 selectedCameraPosition { get; }

        /// <summary>Type of the current component to debug.</summary>
        Type selectedComponentType { get; set; }

        /// <summary>
        /// Obtains the Volumes
        /// </summary>
        /// <returns>The list of <see cref="Volume"/></returns>
        Volume[] GetVolumes();

        /// <summary>
        /// Return if the <see cref="Volume"/> has influence
        /// </summary>
        /// <param name="volume"><see cref="Volume"/> to check the influence</param>
        /// <returns>If the volume has influence</returns>
        bool VolumeHasInfluence(Volume volume);

        /// <summary>
        /// Refreshes the volumes, fetches the stored volumes on the panel
        /// </summary>
        /// <param name="newVolumes">The list of <see cref="Volume"/> to refresh</param>
        /// <returns>If the volumes have been refreshed</returns>
        bool RefreshVolumes(Volume[] newVolumes);

        /// <summary>
        /// Obtains the volume weight
        /// </summary>
        /// <param name="volume"><see cref="Volume"/></param>
        /// <returns>The weight of the volume</returns>
        float GetVolumeWeight(Volume volume);
    }

    /// <summary>
    /// Volume debug settings.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete("This is not longer supported Please use DebugDisplaySettingsVolume. #from(6000.2)", false)]

    public interface IVolumeDebugSettings2 : IVolumeDebugSettings
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// Specifies the render pipeline
        /// </summary>
        [Obsolete("This property is obsolete and kept only for not breaking user code. VolumeDebugSettings will use current pipeline when it needs to gather volume component types and paths. #from(23.2)", false)]
        Type targetRenderPipeline { get; }

        /// <summary>List of Volume component types and their path</summary>
        [Obsolete("This property is obsolete and kept only for not breaking user code. VolumeDebugSettings will use current pipeline when it needs to gather volume component types and paths. #from(23.2)", false)]
        List<(string, Type)> volumeComponentsPathAndType { get; }
    }
}
