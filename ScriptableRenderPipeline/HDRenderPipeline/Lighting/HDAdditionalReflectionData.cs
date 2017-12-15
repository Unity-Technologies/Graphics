using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public enum ReflectionInfluenceShape { Box, Sphere };

    [ExecuteInEditMode]
    [RequireComponent(typeof(ReflectionProbe), typeof(MeshFilter), typeof(MeshRenderer))]
    public class HDAdditionalReflectionData : MonoBehaviour
    {
        #region Registration
        // We need to notify when a data is enabled
        // So it can be properly initialized in the editor
        public static event Action<HDAdditionalReflectionData> OnNewItem;
        static List<HDAdditionalReflectionData> s_AllDatas = new List<HDAdditionalReflectionData>();
        public static IEnumerable<HDAdditionalReflectionData> AllDatas { get { return s_AllDatas; } }

        static void AddData(HDAdditionalReflectionData value)
        {
            s_AllDatas.Add(value);
            if (OnNewItem != null)
                OnNewItem(value);
        }

        static void RemoveData(HDAdditionalReflectionData value)
        {
            s_AllDatas.Remove(value);
        }
        #endregion

        public ReflectionInfluenceShape influenceShape;
        [Range(0.0f,1.0f)]
        public float dimmer = 1.0f;
        public float influenceSphereRadius = 3.0f;
        public float sphereReprojectionVolumeRadius = 1.0f;
        public bool useSeparateProjectionVolume = false;
        public Vector3 boxReprojectionVolumeSize = Vector3.one;
        public Vector3 boxReprojectionVolumeCenter = Vector3.zero;
        public float maxSearchDistance = 8.0f;
        public Texture previewCubemap;

        void OnEnable()
        {
            AddData(this);
        }

        void OnDisable()
        {
            RemoveData(this);
        }
    }
}
