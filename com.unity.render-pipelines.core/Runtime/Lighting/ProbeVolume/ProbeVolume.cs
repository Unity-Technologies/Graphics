using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// A marker to determine what area of the scene is considered by the Probe Volumes system
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Probe Volume (Experimental)")]
    public class ProbeVolume : MonoBehaviour
    {
        public bool globalVolume = false;
        public Vector3 size = new Vector3(10, 10, 10);
        [HideInInspector]
        public float maxSubdivisionMultiplier = 1;
        [HideInInspector]
        public float minSubdivisionMultiplier = 0;
        [HideInInspector, Range(0f, 2f)]
        public float geometryDistanceOffset = 0.2f;

        public LayerMask objectLayerMask = -1;

        [SerializeField] internal bool mightNeedRebaking = false;

        [SerializeField] internal Matrix4x4 cachedTransform;
        [SerializeField] internal int cachedHashCode;

        /// <summary>
        /// Returns the extents of the volume.
        /// </summary>
        /// <returns>The extents of the ProbeVolume.</returns>
        public Vector3 GetExtents()
        {
            return size;
        }

#if UNITY_EDITOR
        internal void UpdateGlobalVolume(Scene scene)
        {
            if (gameObject.scene != scene) return;

            Bounds bounds = new Bounds();
            bool foundABound = false;
            bool ContributesToGI(Renderer renderer)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI;
                return (flags & StaticEditorFlags.ContributeGI) != 0;
            }

            void ExpandBounds(Bounds currBound)
            {
                if (!foundABound)
                {
                    bounds = currBound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(currBound);
                }
            }

            var renderers = UnityEngine.GameObject.FindObjectsOfType<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                bool contributeGI = ContributesToGI(renderer) && renderer.gameObject.activeInHierarchy && renderer.enabled;

                if (contributeGI && renderer.gameObject.scene == scene)
                {
                    ExpandBounds(renderer.bounds);
                }
            }

            transform.position = bounds.center;

            float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
            Vector3 tmpClamp = (bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize));
            tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
            tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
            tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
            size = tmpClamp;
        }

        internal void OnLightingDataAssetCleared()
        {
            mightNeedRebaking = true;
        }

        internal void OnBakeCompleted()
        {
            // We cache the data of last bake completed.
            cachedTransform = gameObject.transform.worldToLocalMatrix;
            cachedHashCode = GetHashCode();
            mightNeedRebaking = false;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            unchecked
            {
                hash = hash * 23 + size.GetHashCode();
                hash = hash * 23 + maxSubdivisionMultiplier.GetHashCode();
                hash = hash * 23 + minSubdivisionMultiplier.GetHashCode();
                hash = hash * 23 + geometryDistanceOffset.GetHashCode();
                hash = hash * 23 + objectLayerMask.GetHashCode();
            }

            return hash;
        }

#endif
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
