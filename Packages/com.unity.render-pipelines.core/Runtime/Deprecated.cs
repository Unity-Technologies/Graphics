using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;
using static UnityEngine.Rendering.DebugUI;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Exposes settings for shader variants
    /// </summary>
    [Obsolete("Use GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>(). #from(23.3)")]

    public interface IShaderVariantSettings
    {
        /// <summary>
        /// Specifies the level of the logging for shader variants
        /// </summary>
        ShaderVariantLogLevel shaderVariantLogLevel { get; set; }

        /// <summary>
        /// Specifies if the stripping of the shaders variants needs to be exported
        /// </summary>
        bool exportShaderVariants { get; set; }

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        bool stripDebugVariants { get => false; set { } }
    }

    public abstract partial class VolumeDebugSettings<T>
    {
        static List<Type> s_ComponentTypes;
        /// <summary>List of Volume component types.</summary>
        [Obsolete("Please use volumeComponentsPathAndType instead, and get the second element of the tuple", false)]
        public static List<Type> componentTypes
        {
            get
            {
                if (s_ComponentTypes == null)
                {
                    s_ComponentTypes = VolumeManager.instance.baseComponentTypeArray
                        .Where(t => !t.IsDefined(typeof(HideInInspector), false))
                        .Where(t => !t.IsDefined(typeof(ObsoleteAttribute), false))
                        .OrderBy(t => ComponentDisplayName(t))
                        .ToList();
                }
                return s_ComponentTypes;
            }
        }

        /// <summary>Returns the name of a component from its VolumeComponentMenuForRenderPipeline.</summary>
        /// <param name="component">A volume component.</param>
        /// <returns>The component display name.</returns>
        [Obsolete("Please use componentPathAndType instead, and get the first element of the tuple", false)]
        public static string ComponentDisplayName(Type component)
        {
            if (component.GetCustomAttribute(typeof(VolumeComponentMenuForRenderPipeline), false) is VolumeComponentMenuForRenderPipeline volumeComponentMenuForRenderPipeline)
                return volumeComponentMenuForRenderPipeline.menu;

            if (component.GetCustomAttribute(typeof(VolumeComponentMenu), false) is VolumeComponentMenuForRenderPipeline volumeComponentMenu)
                return volumeComponentMenu.menu;

            return component.Name;
        }

        /// <summary>
        /// The list of the additional camera datas
        /// </summary>
        [Obsolete("Cameras are auto registered/unregistered, use property cameras", false)]
        protected static List<T> additionalCameraDatas { get; private set; } = new List<T>();

        /// <summary>
        /// Register the camera for the Volume Debug.
        /// </summary>
        /// <param name="additionalCamera">The AdditionalCameraData of the camera to be registered.</param>
        [Obsolete("Cameras are auto registered/unregistered", false)]
        public static void RegisterCamera(T additionalCamera)
        {
            if (!additionalCameraDatas.Contains(additionalCamera))
                additionalCameraDatas.Add(additionalCamera);
        }

        /// <summary>
        /// Unregister the camera for the Volume Debug.
        /// </summary>
        /// <param name="additionalCamera">The AdditionalCameraData of the camera to be registered.</param>
        [Obsolete("Cameras are auto registered/unregistered", false)]
        public static void UnRegisterCamera(T additionalCamera)
        {
            if (additionalCameraDatas.Contains(additionalCamera))
                additionalCameraDatas.Remove(additionalCamera);
        }
    }

    public sealed partial class DebugManager
    {
        /// <summary>
        /// Toggle the debug window.
        /// </summary>
        /// <param name="open">State of the debug window.</param>
        [Obsolete("Use DebugManager.instance.displayEditorUI property instead. #from(23.1)")]
        public void ToggleEditorUI(bool open) => editorUIState.open = open;
    }

    /// <summary>
    /// A marker to adjust probes in an area of the scene.
    /// </summary>
    [Obsolete("ProbeTouchupVolume has been deprecated (UnityUpgradable) -> ProbeAdjustmentVolume", false)]
    public class ProbeTouchupVolume : ProbeAdjustmentVolume
    {
    }

    public sealed partial class VolumeManager
    {
        /// <summary>
        /// Registers a new Volume in the manager. Unity does this automatically when a new Volume is
        /// enabled, or its layer changes, but you can use this function to force-register a Volume
        /// that is currently disabled.
        /// </summary>
        /// <param name="volume">The volume to register.</param>
        /// <param name="layer">The LayerMask that this volume is in.</param>
        /// <seealso cref="Unregister"/>
        [Obsolete("Please use the Register without a given layer index #from(6000.0)", false)]
        public void Register(Volume volume, int layer)
        {
            if (volume.gameObject.layer != layer)
            {
                Debug.LogWarning($"Trying to register Volume {volume.name} on layer index {layer}, when the GameObject {volume.gameObject.name} is on layer index {volume.gameObject.layer}." +
                                 $"{Environment.NewLine}The Volume Manager will respect the GameObject's layer.");
            }

            Register(volume);
        }

        /// <summary>
        /// Unregisters a Volume from the manager. Unity does this automatically when a Volume is
        /// disabled or goes out of scope, but you can use this function to force-unregister a Volume
        /// that you added manually while it was disabled.
        /// </summary>
        /// <param name="volume">The Volume to unregister.</param>
        /// <param name="layer">The LayerMask that this volume is in.</param>
        /// <seealso cref="Register"/>
        [Obsolete("Please use the Register without a given layer index #from(6000.0)", false)]
        public void Unregister(Volume volume, int layer)
        {
            if (volume.gameObject.layer != layer)
            {
                Debug.LogWarning($"Trying to unregister Volume {volume.name} on layer index {layer}, when the GameObject {volume.gameObject.name} is on layer index {volume.gameObject.layer}." +
                                 $"{Environment.NewLine}The Volume Manager will respect the GameObject's layer.");
            }

            Unregister(volume);
        }
    }


    public partial class DebugUI
    {
        /// <summary>
        /// Maskfield enumeration field.
        /// </summary>
        [Obsolete("Mask field is not longer supported. Please use a BitField or implement your own Widget. #from(6000.2)", false)]
        public class MaskField : EnumField<uint>
        {
            /// <summary>
            /// Fills the enum using the provided names
            /// </summary>
            /// <param name="names">names to fill the enum</param>
            public void Fill(string[] names)
            {
                using (ListPool<GUIContent>.Get(out var tmpNames))
                using (ListPool<int>.Get(out var tmpValues))
                {
                    for (int i = 0; i < (names.Length); ++i)
                    {
                        tmpNames.Add(new GUIContent(names[i]));
                        tmpValues.Add(i);
                    }
                    enumNames = tmpNames.ToArray();
                    enumValues = tmpValues.ToArray();
                }
            }

            /// <summary>
            /// Assigns a value to the maskfield.
            /// </summary>
            /// <param name="value">value for the maskfield</param>
            public override void SetValue(uint value)
            {
                Assert.IsNotNull(setter);
                var validValue = ValidateValue(value);

                if (!validValue.Equals(getter()))
                {
                    setter(validValue);
                    onValueChanged?.Invoke(this, validValue);
                }
            }
        }
    }
}
