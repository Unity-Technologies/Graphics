using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water deformer component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public partial class TextureWaterDeformer : MonoBehaviour
    {
        /// <summary>
        /// Specifies the size of the deformer in meters.
        /// </summary>
        public Vector2 regionSize = new Vector2(20.0f, 20.0f);

        /// <summary>
        /// Specifies the amplitude of the deformer. This parameter is used differently based on the deformer type.
        /// </summary>
        public float amplitude = 2.0f;

        /// <summary>
        /// Specifies the range of the texture deformer
        /// </summary>
        public Vector2 range = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// Specifies the texture used for the deformer.
        /// </summary>
        public Texture texture = null;

        #region Instance Management
        // Management to avoid memory allocations at fetch time
        internal static HashSet<TextureWaterDeformer> instances = new HashSet<TextureWaterDeformer>();
        internal static TextureWaterDeformer[] instancesAsArray = null;
        internal static int instanceCount = 0;

        internal static void RegisterInstance(TextureWaterDeformer deformer)
        {
            instances.Add(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new TextureWaterDeformer[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }

        internal static void UnregisterInstance(TextureWaterDeformer deformer)
        {
            instances.Remove(deformer);
            instanceCount = instances.Count;
            if (instanceCount > 0)
            {
                instancesAsArray = new TextureWaterDeformer[instanceCount];
                instances.CopyTo(instancesAsArray);
            }
            else
            {
                instancesAsArray = null;
            }
        }
        #endregion

        #region MonoBehavior Methods
        private void Start()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void Awake()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnEnable()
        {
            // Add this water surface to the internal surface management
            RegisterInstance(this);
        }

        private void OnDisable()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);
        }

        void OnDestroy()
        {
            // Remove this water surface from the internal surface management
            UnregisterInstance(this);
        }
        #endregion

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireMesh(Resources.GetBuiltinResource<Mesh>("Cube.fbx"), transform.position, Quaternion.Euler(0, transform.eulerAngles.y, 0), new Vector3(regionSize.x, amplitude * 2, regionSize.y));
        }
    }
}
