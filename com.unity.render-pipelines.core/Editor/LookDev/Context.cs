using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Different working views in LookDev
    /// </summary>
    public enum ViewIndex
    {
        /// <summary>First view</summary>
        First,
        /// <summary>Second view</summary>
        Second
    };

    /// <summary>
    /// Same as <see cref="ViewIndex"/> plus a compound value
    /// </summary>
    public enum ViewCompositionIndex
    {
        /// <summary>First view</summary>
        First = ViewIndex.First,
        /// <summary>Second view</summary>
        Second = ViewIndex.Second,
        /// <summary>Composite view (Several view on screen)</summary>
        Composite
    };

    // /!\ WARNING: these value name are used as uss file too.
    // if your rename here, rename in the uss too.
    /// <summary>
    /// Different layout supported in LookDev
    /// </summary>
    public enum Layout
    {
        /// <summary>First view display fully</summary>
        FullFirstView,
        /// <summary>Second view display fully</summary>
        FullSecondView,
        /// <summary>First and second views displayed splitted horizontally</summary>
        HorizontalSplit,
        /// <summary>First and second views displayed splitted vertically</summary>
        VerticalSplit,
        /// <summary>First and second views displayed with stacking and orientation customizable  split</summary>
        CustomSplit
    }

    /// <summary>
    /// Status of the side panel of the LookDev window
    /// </summary>
    public enum SidePanel
    {
        /// <summary>No side panel</summary>
        None = -1,
        /// <summary>Environment side panel</summary>
        Environment,
        /// <summary>Debug side panel</summary>
        Debug,
    }

    /// <summary>
    /// The target views of the debug panel
    /// </summary>
    public enum TargetDebugView
    {
        /// <summary>First Debug view</summary>
        First,
        /// <summary>Both Debug view</summary>
        Both,
        /// <summary>Second Debug view</summary>
        Second
    };

    /// <summary>
    /// Class containing all data used by the LookDev Window to render
    /// </summary>
    [System.Serializable]
    public class Context : ScriptableObject, IDisposable
    {
        [SerializeField]
        string m_EnvironmentLibraryGUID = ""; //Empty GUID

        [SerializeField]
        bool m_CameraSynced = true;

        EnvironmentLibrary m_EnvironmentLibrary;

        /// <summary>The currently used Environment</summary>
        public EnvironmentLibrary environmentLibrary
        {
            get
            {
                //check if asset deleted by user
                if (m_EnvironmentLibrary != null && AssetDatabase.Contains(m_EnvironmentLibrary))
                    return m_EnvironmentLibrary;

                if (!String.IsNullOrEmpty(m_EnvironmentLibraryGUID))
                {
                    //user deleted the EnvironmentLibrary asset
                    m_EnvironmentLibraryGUID = ""; //Empty GUID
                    LookDev.currentEnvironmentDisplayer.Repaint();
                }
                return null;
            }
            private set => m_EnvironmentLibrary = value;
        }

        /// <summary>The currently used layout</summary>
        [field: SerializeField]
        public LayoutContext layout { get; private set; } = new LayoutContext();

        /// <summary>
        /// State if both views camera movement are synced or not
        /// </summary>
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

        /// <summary>
        /// Helper class to iterate on views
        /// </summary>
        public struct ViewIterator : IEnumerable<ViewContext>
        {
            ViewContext[] m_Views;
            internal ViewIterator(ViewContext[] views)
                => m_Views = views;

            /// <summary>
            /// Helper function to enumerates on ViewContexts
            /// </summary>
            /// <returns>Enumerator on ViewContext</returns>
            IEnumerator IEnumerable.GetEnumerator()
                => m_Views.GetEnumerator();

            /// <summary>
            /// Helper function to enumerates on ViewContexts
            /// </summary>
            /// <returns>Enumerator on ViewContext</returns>
            IEnumerator<ViewContext> IEnumerable<ViewContext>.GetEnumerator()
                => ((IEnumerable<ViewContext>)m_Views).GetEnumerator();
        }

        /// <summary>
        /// Helper function to get ViewIterator on ViewContexts
        /// </summary>
        public ViewIterator viewContexts
            => new ViewIterator(m_Views);

        /// <summary>
        /// Get datas relative to a view
        /// </summary>
        /// <param name="index">The view index to look at</param>
        /// <returns>Datas for the selected view</returns>
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

        /// <summary>Update the environment library used.</summary>
        /// <param name="library">The new EnvironmentLibrary</param>
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

        /// <summary>
        /// Change focused view.
        /// Focused view is the base view to copy data when syncing views' cameras
        /// </summary>
        /// <param name="index">The index of the view</param>
        public void SetFocusedCamera(ViewIndex index)
            => layout.lastFocusedView = index;


        private bool disposedValue = false; // To detect redundant calls
        /// <summary>Disposable behaviour</summary>
        void IDisposable.Dispose()
        {
            if (!disposedValue)
            {
                if (cameraSynced)
                    EditorApplication.update -= SynchronizeCameraStates;

                disposedValue = true;
            }
        }

        internal bool HasLibraryAssetChanged(EnvironmentLibrary environmentLibrary)
        {
            if (environmentLibrary == null)
                return !String.IsNullOrEmpty(m_EnvironmentLibraryGUID);

            return m_EnvironmentLibraryGUID != AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(environmentLibrary));
        }
    }

    /// <summary>
    /// Data regarding the layout currently used in LookDev
    /// </summary>
    [System.Serializable]
    public class LayoutContext
    {
        /// <summary>The layout used</summary>
        public Layout viewLayout;
        /// <summary>The last focused view</summary>
        public ViewIndex lastFocusedView = ViewIndex.First;
        /// <summary>The state of the side panel</summary>
        public SidePanel showedSidePanel;
        /// <summary>The view to change when manipulating the Debug side panel</summary>
        [NonSerialized]
        public TargetDebugView debugPanelSource = TargetDebugView.Both;

        [SerializeField]
        internal ComparisonGizmoState gizmoState = new ComparisonGizmoState();

        internal bool isSimpleView => viewLayout == Layout.FullFirstView || viewLayout == Layout.FullSecondView;
        internal bool isMultiView => viewLayout == Layout.HorizontalSplit || viewLayout == Layout.VerticalSplit;
        internal bool isCombinedView => viewLayout == Layout.CustomSplit;
    }

    /// <summary>
    /// Data container containing content of a view
    /// </summary>
    [System.Serializable]
    public class ViewContext
    {
        /// <summary>The position and rotation of the camera</summary>
        [field: SerializeField]
        public CameraState camera { get; private set; } = new CameraState();

        /// <summary>The currently viewed debugState</summary>
        public DebugContext debug { get; private set; } = new DebugContext();

        //Environment asset, sub-asset (under a library) or cubemap
        [SerializeField]
        string m_EnvironmentGUID = ""; //Empty GUID

        /// <summary>
        /// Check if an Environment is registered for this view.
        /// The result will be accurate even if the Environment have not been reloaded yet.
        /// </summary>
        public bool hasEnvironment => !String.IsNullOrEmpty(m_EnvironmentGUID);

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
            m_EnvironmentGUID = "";
            environment = null;
            if (environmentOrCubemapAsset == null || environmentOrCubemapAsset.Equals(null))
                return;

            if (!(environmentOrCubemapAsset is Environment)
                && !(environmentOrCubemapAsset is Cubemap))
                throw new System.ArgumentException("Only Environment or Cubemap accepted for environmentOrCubemapAsset parameter");

            string GUID;
            long localIDInFile;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(environmentOrCubemapAsset, out GUID, out localIDInFile);
            m_EnvironmentGUID = $"{GUID},{localIDInFile}";

            if (environmentOrCubemapAsset is Environment)
                environment = environmentOrCubemapAsset as Environment;
            else //Cubemap
                environment = Environment.GetTemporaryEnvironmentForCubemap(environmentOrCubemapAsset as Cubemap);
        }

        void LoadEnvironmentFromGUID()
        {
            environment = null;

            GUID storedGUID;
            string[] GUIDAndLocalIDInFile = m_EnvironmentGUID.Split(new[] { ',' });
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
                environment = Environment.GetTemporaryEnvironmentForCubemap(cubemap);
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

        /// <summary>Reset the camera state to default values</summary>
        public void ResetCameraState()
            => camera.Reset();
    }


    /// <summary>
    /// Class that will contain debug value used.
    /// </summary>
    public class DebugContext
    {
        /// <summary>Display shadows in view.</summary>
        public bool shadow = true;

        /// <summary>Debug mode displayed. -1 means none.</summary>
        public int viewMode = -1;

        ///// <summary>Display the debug grey balls</summary>
        //public bool greyBalls;

        //[SerializeField]
        //string colorChartGUID = ""; //Empty GUID

        ///// <summary>The currently used color chart</summary>
        //public Texture2D colorChart { get; private set; }
    }
}
