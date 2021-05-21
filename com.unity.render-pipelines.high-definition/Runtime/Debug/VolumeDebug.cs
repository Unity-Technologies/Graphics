using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public class VolumeDebugSettings
    {
        /// <summary>Current volume component to debug.</summary>
        public int      selectedComponent = 0;

        int m_SelectedCamera = 0;

        /// <summary>Current camera index to debug.</summary>
        public int selectedCameraIndex
        {
            get
            {
#if UNITY_EDITOR
                if (m_SelectedCamera < 0 || m_SelectedCamera > cameras.Count + 1)
                    return 0;
#else
                if (m_SelectedCamera < 0 || m_SelectedCamera > cameras.Count)
                    return 0;
#endif
                return m_SelectedCamera;
            }
            set { m_SelectedCamera = value; }
        }

        /// <summary>Current camera to debug.</summary>
        public Camera selectedCamera
        {
            get
            {
#if UNITY_EDITOR
                if (m_SelectedCamera <= 0 || m_SelectedCamera > cameras.Count + 1)
                    return null;
                if (m_SelectedCamera == 1)
                    return SceneView.lastActiveSceneView.camera;
                else
                    return cameras[m_SelectedCamera - 2].GetComponent<Camera>();
#else
                if (m_SelectedCamera <= 0 || m_SelectedCamera > cameras.Count)
                    return null;
                return cameras[m_SelectedCamera - 1].GetComponent<Camera>();
#endif
            }
        }

        /// <summary>Selected camera volume stack.</summary>
        public VolumeStack selectedCameraVolumeStack
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return null;
                var stack = HDCamera.GetOrCreate(cam).volumeStack;
                if (stack != null)
                    return stack;
                return VolumeManager.instance.stack;
            }
        }

        /// <summary>Selected camera volume layer mask.</summary>
        public LayerMask selectedCameraLayerMask
        {
            get
            {
#if UNITY_EDITOR
                if (m_SelectedCamera <= 0 || m_SelectedCamera > cameras.Count + 1)
                    return (LayerMask)0;
                if (m_SelectedCamera == 1)
                    return -1;
                return cameras[m_SelectedCamera - 2].volumeLayerMask;
#else
                if (m_SelectedCamera <= 0 || m_SelectedCamera > cameras.Count)
                    return (LayerMask)0;
                return cameras[m_SelectedCamera - 1].volumeLayerMask;
#endif
            }
        }

        /// <summary>Selected camera volume position.</summary>
        public Vector3 selectedCameraPosition
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return Vector3.zero;

                var anchor = HDCamera.GetOrCreate(cam).volumeAnchor;
                if (anchor == null) // means the hdcamera has not been initialized
                {
                    // So we have to update the stack manually
                    if (cam.TryGetComponent<HDAdditionalCameraData>(out var data))
                        anchor = data.volumeAnchorOverride;
                    if (anchor == null) anchor = cam.transform;
                    var stack = selectedCameraVolumeStack;
                    if (stack != null)
                        VolumeManager.instance.Update(stack, anchor, selectedCameraLayerMask);
                }
                return anchor.position;
            }
        }

        /// <summary>Type of the current component to debug.</summary>
        public Type     selectedComponentType
        {
            get { return componentTypes[selectedComponent - 1]; }
            set
            {
                var index = componentTypes.FindIndex(t => t == value);
                if (index != -1)
                    selectedComponent = index + 1;
            }
        }

        static List<Type> s_ComponentTypes;

        /// <summary>List of Volume component types.</summary>
        static public List<Type> componentTypes
        {
            get
            {
                if (s_ComponentTypes == null)
                {
                    s_ComponentTypes = VolumeManager.instance.baseComponentTypeArray
                    .Where(t => !t.IsDefined(typeof(VolumeComponentDeprecated), false))
                    .OrderBy(t => ComponentDisplayName(t))
                    .ToList();
                }
                return s_ComponentTypes;
            }
        }

        /// <summary>Returns the name of a component from its VolumeComponentMenu.</summary>
        /// <param name="component">A volume component.</param>
        /// <returns>The component display name.</returns>
        static public string ComponentDisplayName(Type component)
        {
            Attribute attrib = component.GetCustomAttribute(typeof(VolumeComponentMenu), false);
            if (attrib != null)
                return (attrib as VolumeComponentMenu).menu;
            return component.Name;
        }

        /// <summary>List of HD Additional Camera data.</summary>
        static public List<HDAdditionalCameraData> cameras {get; private set; } = new List<HDAdditionalCameraData>();

        /// <summary>Register HDAdditionalCameraData for DebugMenu</summary>
        /// <param name="camera">The camera to register.</param>
        public static void RegisterCamera(HDAdditionalCameraData camera)
        {
            if (!cameras.Contains(camera))
                cameras.Add(camera);
        }

        /// <summary>Unregister HDAdditionalCameraData for DebugMenu</summary>
        /// <param name="camera">The camera to unregister.</param>
        public static void UnRegisterCamera(HDAdditionalCameraData camera)
        {
            if (cameras.Contains(camera))
                cameras.Remove(camera);
        }


        /// <summary>Get a VolumeParameter from a VolumeComponent</summary>
        /// <param name="component">The component to get the parameter from.</param>
        /// <param name="field">The field info of the parameter.</param>
        /// <returns>The volume parameter.</returns>
        public VolumeParameter GetParameter(VolumeComponent component, FieldInfo field)
        {
            return (VolumeParameter)field.GetValue(component);
        }

        /// <summary>Get a VolumeParameter from a VolumeComponent on the <see cref="selectedCameraVolumeStack"/></summary>
        /// <param name="field">The field info of the parameter.</param>
        /// <returns>The volume parameter.</returns>
        public VolumeParameter GetParameter(FieldInfo field)
        {
            VolumeStack stack = selectedCameraVolumeStack;
            return stack == null ? null : GetParameter(stack.GetComponent(selectedComponentType), field);
        }

        /// <summary>Get a VolumeParameter from a component of a volume</summary>
        /// <param name="volume">The volume to get the component from.</param>
        /// <param name="field">The field info of the parameter.</param>
        /// <returns>The volume parameter.</returns>
        public VolumeParameter GetParameter(Volume volume, FieldInfo field)
        {
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
            if (!profile.TryGet(selectedComponentType, out VolumeComponent component))
                return null;
            var param = GetParameter(component, field);
            if (!param.overrideState)
                return null;
            return param;
        }

        float[] weights = null;
        float ComputeWeight(Volume volume, Vector3 triggerPos)
        {
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;

            if (!volume.gameObject.activeInHierarchy) return 0;
            if (!volume.enabled || profile == null || volume.weight <= 0f) return 0;
            if (!profile.TryGet(selectedComponentType, out VolumeComponent component)) return 0;
            if (!component.active) return 0;

            float weight = Mathf.Clamp01(volume.weight);
            if (!volume.isGlobal)
            {
                var colliders = volume.GetComponents<Collider>();

                // Find closest distance to volume, 0 means it's inside it
                float closestDistanceSqr = float.PositiveInfinity;
                foreach (var collider in colliders)
                {
                    if (!collider.enabled)
                        continue;

                    var closestPoint = collider.ClosestPoint(triggerPos);
                    var d = (closestPoint - triggerPos).sqrMagnitude;

                    if (d < closestDistanceSqr)
                        closestDistanceSqr = d;
                }
                float blendDistSqr = volume.blendDistance * volume.blendDistance;
                if (closestDistanceSqr > blendDistSqr)
                    weight = 0f;
                else if (blendDistSqr > 0f)
                    weight *= 1f - (closestDistanceSqr / blendDistSqr);
            }
            return weight;
        }

        Volume[] volumes = null;

        /// <summary>Get an array of volumes on the <see cref="selectedCameraLayerMask"/></summary>
        /// <returns>An array of volumes sorted by influence.</returns>
        public Volume[] GetVolumes()
        {
            return VolumeManager.instance.GetVolumes(selectedCameraLayerMask)
                .Where(v => v.sharedProfile != null)
                .Reverse().ToArray();
        }

        VolumeParameter[,] savedStates = null;
        VolumeParameter[,] GetStates()
        {
            var fields = selectedComponentType
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(t => t.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                .ToArray();

            VolumeParameter[,] states = new VolumeParameter[volumes.Length, fields.Length];
            for (int i = 0; i < volumes.Length; i++)
            {
                var profile = volumes[i].HasInstantiatedProfile() ? volumes[i].profile : volumes[i].sharedProfile;
                if (!profile.TryGet(selectedComponentType, out VolumeComponent component))
                    continue;

                for (int j = 0; j < fields.Length; j++)
                {
                    var param = GetParameter(component, fields[j]);;
                    states[i, j] = param.overrideState ? param : null;
                }
            }
            return states;
        }

        bool ChangedStates(VolumeParameter[,] newStates)
        {
            if (savedStates.GetLength(1) != newStates.GetLength(1))
                return true;
            for (int i = 0; i < savedStates.GetLength(0); i++)
            {
                for (int j = 0; j < savedStates.GetLength(1); j++)
                {
                    if ((savedStates[i, j] == null) != (newStates[i, j] == null))
                        return true;
                }
            }
            return false;
        }

        /// <summary>Updates the list of volumes and recomputes volume weights</summary>
        /// <param name="newVolumes">The new list of volumes.</param>
        /// <returns>True if the volume list have been updated.</returns>
        public bool RefreshVolumes(Volume[] newVolumes)
        {
            bool ret = false;
            if (volumes == null || !newVolumes.SequenceEqual(volumes))
            {
                volumes = (Volume[])newVolumes.Clone();
                savedStates = GetStates();
                ret = true;
            }
            else
            {
                var newStates = GetStates();
                if (savedStates == null || ChangedStates(newStates))
                {
                    savedStates = newStates;
                    ret = true;
                }
            }

            var triggerPos = selectedCameraPosition;
            weights = new float[volumes.Length];
            for (int i = 0; i < volumes.Length; i++)
                weights[i] = ComputeWeight(volumes[i], triggerPos);

            return ret;
        }

        /// <summary>Get the weight of a volume computed from the <see cref="selectedCameraPosition"/></summary>
        /// <param name="volume">The volume to compute weight for.</param>
        /// <returns>The weight of the volume.</returns>
        public float GetVolumeWeight(Volume volume)
        {
            if (weights == null)
                return 0;

            float total = 0f, weight = 0f;
            for (int i = 0; i < volumes.Length; i++)
            {
                weight = weights[i];
                weight *= 1f - total;
                total += weight;

                if (volumes[i] == volume)
                    return weight;
            }

            return 0f;
        }

        /// <summary>Determines if a volume as an influence on the interpolated value</summary>
        /// <param name="volume">The volume.</param>
        /// <returns>True if the given volume as an influence.</returns>
        public bool VolumeHasInfluence(Volume volume)
        {
            if (weights == null)
                return false;

            int index = Array.IndexOf(volumes, volume);
            if (index == -1)
                return false;

            return weights[index] != 0f;
        }
    }
}
