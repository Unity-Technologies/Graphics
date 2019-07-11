using System;
using UnityEngine;

namespace UnityEditor.Rendering.Experimental.LookDev
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

    public enum SidePanel
    {
        None = -1,
        Environment,
        Debug
    }

    [System.Serializable]
    public class Context : ScriptableObject, IDisposable
    {
        [SerializeField]
        string m_EnvironmentLibraryGUID = ""; //Empty GUID

        [SerializeField]
        bool m_CameraSynced = true;

        /// <summary>The currently used Environment</summary>
        public EnvironmentLibrary environmentLibrary { get; private set; }

        /// <summary>The currently used layout</summary>
        [field: SerializeField]
        public LayoutContext layout { get; private set; } = new LayoutContext();

        public bool cameraSynced
        {
            get => m_CameraSynced;
            set
            {
                if (m_CameraSynced ^ value)
                {
                    if (value)
                        EditorApplication.update += SynchronizeCameraStates;
                    else
                        EditorApplication.update -= SynchronizeCameraStates;
                    m_CameraSynced = value;
                }
            }
        }

        [SerializeField]
        ViewContext[] m_Views = new ViewContext[2]
        {
            new ViewContext(),
            new ViewContext()
        };

        public ViewContext GetViewContent(ViewIndex index)
            => m_Views[(int)index];

        internal void Init()
        {
            LoadEnvironmentLibraryFromGUID();

            //recompute non serialized computes states
            layout.gizmoState.Init();

            if (cameraSynced)
                EditorApplication.update += SynchronizeCameraStates;
        }

        /// <summary>Update the environment used.</summary>
        /// <param name="environmentOrCubemapAsset">
        /// The new <see cref="Environment"/> to use.
        /// Or the <see cref="Cubemap"/> to use to build a new one.
        /// Other types will raise an ArgumentException.
        /// </param>
        public void UpdateEnvironmentLibrary(EnvironmentLibrary library)
        {
            m_EnvironmentLibraryGUID = "";
            environmentLibrary = null;
            if (library == null || library.Equals(null))
                return;

            m_EnvironmentLibraryGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(library));
            environmentLibrary = library;
        }

        void LoadEnvironmentLibraryFromGUID()
        {
            environmentLibrary = null;

            GUID storedGUID;
            GUID.TryParse(m_EnvironmentLibraryGUID, out storedGUID);
            if (storedGUID.Empty())
                return;

            string path = AssetDatabase.GUIDToAssetPath(m_EnvironmentLibraryGUID);
            environmentLibrary = AssetDatabase.LoadAssetAtPath<EnvironmentLibrary>(path);
        }

        /// <summary>
        /// Synchronize cameras from both view using data from the baseCameraState
        /// </summary>
        /// <param name="baseCameraState">The <see cref="ViewIndex"/> to be used as reference</param>
        public void SynchronizeCameraStates(ViewIndex baseCameraState)
        {
            switch (baseCameraState)
            {
                case ViewIndex.First:
                    m_Views[1].camera.SynchronizeFrom(m_Views[0].camera);
                    break;
                case ViewIndex.Second:
                    m_Views[0].camera.SynchronizeFrom(m_Views[1].camera);
                    break;
                default:
                    throw new System.ArgumentException("Unknow ViewIndex given in parameter.");
            }
        }

        void SynchronizeCameraStates()
            => SynchronizeCameraStates(layout.lastFocusedView);

        public void SetFocusedCamera(ViewIndex index)
            => layout.lastFocusedView = index;


        private bool disposedValue = false; // To detect redundant calls
        void IDisposable.Dispose()
        {
            if (!disposedValue)
            {
                if (cameraSynced)
                    EditorApplication.update -= SynchronizeCameraStates;

                disposedValue = true;
            }
        }
    }

    [System.Serializable]
    public class LayoutContext
    {
        public Layout viewLayout;
        public ViewIndex lastFocusedView = ViewIndex.First;
        public SidePanel showedSidePanel;

        [SerializeField]
        internal ComparisonGizmoState gizmoState = new ComparisonGizmoState();

        public bool isSimpleView => viewLayout == Layout.FullFirstView || viewLayout == Layout.FullSecondView;
        public bool isMultiView => viewLayout == Layout.HorizontalSplit || viewLayout == Layout.VerticalSplit;
        public bool isCombinedView => viewLayout == Layout.CustomSplit || viewLayout == Layout.CustomCircular;
    }

    [System.Serializable]
    public class ViewContext
    {
        [field: SerializeField]
        public CameraState camera { get; private set; } = new CameraState();


        /// <summary>The currently viewed debugState</summary>
        [field: SerializeField]
        public DebugContext debug { get; private set; } = new DebugContext();

        //Environment asset, sub-asset (under a library) or cubemap
        [SerializeField]
        string environmentGUID = ""; //Empty GUID

        /// <summary>
        /// Check if an Environment is registered for this view.
        /// The result will be accurate even if the Environment have not been reloaded yet.
        /// </summary>
        public bool hasEnvironment => !String.IsNullOrEmpty(environmentGUID);

        /// <summary>The currently used Environment</summary>
        public Environment environment { get; private set; }

        [SerializeField]
        string viewedObjectAssetGUID = ""; //Empty GUID

        // Careful here: we want to keep it while reloading script.
        // But from one unity editor to an other, ID are not kept.
        // So, only use it when reloading from script update.
        [SerializeField]
        int viewedObjecHierarchytInstanceID;

        /// <summary>
        /// Check if an Environment is registered for this view.
        /// The result will be accurate even if the object have not been reloaded yet.
        /// </summary>
        public bool hasViewedObject =>
            !String.IsNullOrEmpty(viewedObjectAssetGUID)
            || viewedObjecHierarchytInstanceID != 0;

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

            string GUID;
            long localIDInFile;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(environmentOrCubemapAsset, out GUID, out localIDInFile);
            environmentGUID = $"{GUID},{localIDInFile}";

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
            string[] GUIDAndLocalIDInFile = environmentGUID.Split(new[] { ',' });
            GUID.TryParse(GUIDAndLocalIDInFile[0], out storedGUID);
            if (storedGUID.Empty())
                return;
            long localIDInFile = GUIDAndLocalIDInFile.Length < 2 ? 0L : long.Parse(GUIDAndLocalIDInFile[1]);

            string path = AssetDatabase.GUIDToAssetPath(GUIDAndLocalIDInFile[0]);

            Type savedType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (savedType == typeof(EnvironmentLibrary))
            {
                object[] loaded = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < loaded.Length; ++i)
                {
                    string garbage;
                    long testedLocalIndex;
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier((UnityEngine.Object)loaded[i], out garbage, out testedLocalIndex)
                        && testedLocalIndex == localIDInFile)
                    {
                        environment = loaded[i] as Environment;
                        break;
                    }
                }
            }
            else if (savedType == typeof(Environment))
                environment = AssetDatabase.LoadAssetAtPath<Environment>(path);
            else if (savedType == typeof(Cubemap))
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

        public void ResetCameraState()
            => camera.Reset();
    }


    [System.Serializable]
    public class DebugContext
    {
        ///// <summary>Display the debug grey balls</summary>
        //public bool greyBalls;

        //[SerializeField]
        //string colorChartGUID = ""; //Empty GUID

        ///// <summary>The currently used color chart</summary>
        //public Texture2D colorChart { get; private set; }


    }
}
