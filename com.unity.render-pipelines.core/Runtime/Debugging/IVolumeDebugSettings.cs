using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public interface IVolumeDebugSettings
    {
        /// <summary>
        /// Specifies the render pipelie
        /// </summary>
        Type targetRenderPipeline { get; }

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

        /// <summary>List of Volume component types and their path</summary>
        List<(string, Type)> componentTypes { get; }

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
}
