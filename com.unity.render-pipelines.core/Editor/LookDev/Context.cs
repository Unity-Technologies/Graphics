using UnityEngine;

namespace UnityEditor.Rendering.LookDev
{
    public enum ViewIndex
    {
        First,
        Second
    };
    public enum ViewCompositionIndex
    {
        First = ViewIndex.First,
        Second = ViewIndex.Second,
        Composite
    };

    // /!\ WARNING: these value name are used as uss file too.
    // if your rename here, rename in the uss too.
    public enum Layout
    {
        FullFirstView,
        FullSecondView,
        HorizontalSplit,
        VerticalSplit,
        CustomSplit,
        CustomCircular
    }

    [System.Serializable]
    public class Context : ScriptableObject
    {
        [field: SerializeField]
        public LayoutContext layout { get; private set; } = new LayoutContext();

        [SerializeField]
        ViewContext[] m_Views = new ViewContext[2]
        {
            new ViewContext(),
            new ViewContext()
        };

        [SerializeField]
        CameraState[] m_Cameras = new CameraState[2]
        {
            new CameraState(),
            new CameraState()
        };

        public ViewContext GetViewContent(ViewIndex index)
            => m_Views[(int)index];

        public CameraState GetCameraState(ViewIndex index)
            => m_Cameras[(int)index];

        internal void Validate()
        {
            if (m_Views == null || m_Views.Length != 2)
            {
                m_Views = new ViewContext[2]
                {
                    new ViewContext(),
                    new ViewContext()
                };
            }
            if (m_Cameras == null || m_Cameras.Length != 2)
            {
                m_Cameras = new CameraState[2]
                {
                    new CameraState(),
                    new CameraState()
                };
            }
        }
    }
    
    [System.Serializable]
    public class LayoutContext
    {
        public Layout viewLayout;
        public bool showEnvironmentPanel;

        [SerializeField]
        internal ComparisonGizmoState gizmoState = new ComparisonGizmoState();

        public bool isSimpleView => viewLayout == Layout.FullFirstView || viewLayout == Layout.FullSecondView;
        public bool isMultiView => viewLayout == Layout.HorizontalSplit || viewLayout == Layout.VerticalSplit;
        public bool isCombinedView => viewLayout == Layout.CustomSplit || viewLayout == Layout.CustomCircular;
    }

    [System.Serializable]
    public class ViewContext
    {
        //Environment asset, sub-asset (under a library) or cubemap
        [SerializeField]
        string environmentGUID = ""; //Empty GUID

        /// <summary>The currently used Environment</summary>
        public Environment environment { get; private set; }
        
        [SerializeField]
        string viewedObjectAssetGUID = ""; //Empty GUID

        // Careful here: we want to keep it while reloading script.
        // But from one unity editor to an other, ID are not kept.
        // So, only use it when reloading from script update.
        [SerializeField]
        int viewedObjecHierarchytInstanceID;

        /// <summary>Reference to the object given for instantiation.</summary>
        public GameObject viewedObjectReference { get; private set; }

        /// <summary>
        /// The currently displayed instance of <see cref="viewedObjectReference"/>.
        /// It will be instantiated when pushing changes to renderer.
        /// See <see cref="LookDev.SaveContextChangeAndApply(ViewIndex)"/>
        /// </summary>
        public GameObject viewedInstanceInPreview { get; internal set; }

        /// <summary>Update the environment used.</summary>
        /// <param name="environmentOrCubemapAsset">
        /// The new <see cref="Environment"/> to use.
        /// Or the <see cref="Cubemap"/> to use to build a new one.
        /// Other types will raise an ArgumentException.
        /// </param>
        public void UpdateEnvironment(UnityEngine.Object environmentOrCubemapAsset)
        {
            environmentGUID = "";
            environment = null;
            if (environmentOrCubemapAsset == null || environmentOrCubemapAsset.Equals(null))
                return;

            if (!(environmentOrCubemapAsset is Environment)
                && !(environmentOrCubemapAsset is Cubemap))
                throw new System.ArgumentException("Only Environment or Cubemap accepted for environmentOrCubemapAsset parameter");
            
            environmentGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(environmentOrCubemapAsset));
            if (environmentOrCubemapAsset is Environment)
                environment = environmentOrCubemapAsset as Environment;
            else //Cubemap
            {
                environment = new Environment();
                environment.sky.cubemap = environmentOrCubemapAsset as Cubemap;
            }
        }

        void LoadEnvironmentFromGUID()
        {
            environment = null;

            GUID storedGUID;
            GUID.TryParse(environmentGUID, out storedGUID);
            if (storedGUID.Empty())
                return;

            string path = AssetDatabase.GUIDToAssetPath(environmentGUID);
            environment = AssetDatabase.LoadAssetAtPath<Environment>(path);

            if (environment == null)
            {
                Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                environment = new Environment();
                environment.sky.cubemap = cubemap;
            }
        }

        /// <summary>Update the object reference used for instantiation.</summary>
        /// <param name="viewedObject">The new reference.</param>
        public void UpdateViewedObject(GameObject viewedObject)
        {
            viewedObjectAssetGUID = "";
            viewedObjecHierarchytInstanceID = 0;
            viewedObjectReference = null;
            if (viewedObject == null || viewedObject.Equals(null))
                return;
            
            bool fromHierarchy = viewedObject.scene.IsValid();
            if (fromHierarchy)
                viewedObjecHierarchytInstanceID = viewedObject.GetInstanceID();
            else
                viewedObjectAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(viewedObject));
            viewedObjectReference = viewedObject;
        }

        //WARNING: only for script reloading
        void LoadViewedObject()
        {
            viewedObjectReference = null;

            GUID storedGUID;
            GUID.TryParse(viewedObjectAssetGUID, out storedGUID);
            if (!storedGUID.Empty())
            {
                string path = AssetDatabase.GUIDToAssetPath(viewedObjectAssetGUID);
                viewedObjectReference = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            else if (viewedObjecHierarchytInstanceID != 0)
            {
                viewedObjectReference = EditorUtility.InstanceIDToObject(viewedObjecHierarchytInstanceID) as GameObject;
            }
        }

        internal void LoadAll(bool reloadWithTemporaryID)
        {
            if (!reloadWithTemporaryID)
                CleanTemporaryObjectIndexes();
            LoadEnvironmentFromGUID();
            LoadViewedObject();
        }

        internal void CleanTemporaryObjectIndexes()
            => viewedObjecHierarchytInstanceID = 0;

        //[TODO: add object position]
        //[TODO: add camera frustum]
        //[TODO: manage shadow and lights]
    }
}
