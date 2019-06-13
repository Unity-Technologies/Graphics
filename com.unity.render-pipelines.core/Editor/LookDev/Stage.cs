using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Experimental.LookDev;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    //TODO: add undo support
    public class Stage : IDisposable
    {
        const int k_PreviewCullingLayerIndex = 31; //Camera.PreviewCullingLayer; //TODO: expose or reflection

        private readonly Scene m_PreviewScene;

        // Everything except camera
        private readonly List<GameObject> m_GameObjects = new List<GameObject>();
        private readonly List<GameObject> m_PersistentGameObjects = new List<GameObject>();
        private readonly Camera m_Camera;

        /// <summary>Get access to the stage's camera</summary>
        public Camera camera => m_Camera;

        /// <summary>Get access to the stage's scene</summary>
        public Scene scene => m_PreviewScene;

        private StageRuntimeInterface SRI;
        public StageRuntimeInterface runtimeInterface
            => SRI ?? (SRI = new StageRuntimeInterface(CreateGameObjectIntoStage, () => camera));

        /// <summary>
        /// Construct a new stage to let your object live.
        /// A stage is a scene with visibility isolation.
        /// </summary>
        /// <param name="sceneName">Name of the scene used.</param>
        public Stage(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new System.ArgumentNullException("sceneName");

            m_PreviewScene = EditorSceneManager.NewPreviewScene();
            m_PreviewScene.name = sceneName;
            
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Look Dev Camera", HideFlags.HideAndDontSave, typeof(Camera));
            MoveIntoStage(camGO, true); //position will be updated right before rendering
            camGO.layer = k_PreviewCullingLayerIndex;

            m_Camera = camGO.GetComponent<Camera>();
            m_Camera.cameraType = CameraType.Game;  //cannot be preview in HDRP: too many things skiped
            m_Camera.enabled = false;
            m_Camera.clearFlags = CameraClearFlags.Depth;
            m_Camera.cullingMask = 1 << k_PreviewCullingLayerIndex;
            m_Camera.renderingPath = RenderingPath.DeferredShading;
            m_Camera.useOcclusionCulling = false;
            m_Camera.scene = m_PreviewScene;
        }

        /// <summary>
        /// Move a GameObject into the stage's scene at origin.
        /// </summary>
        /// <param name="gameObject">The gameObject to move.</param>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <seealso cref="InstantiateIntoStage"/>
        public void MoveIntoStage(GameObject gameObject, bool persistent = false)
            => MoveIntoStage(gameObject, Vector3.zero, Quaternion.identity, persistent);

        /// <summary>
        /// Move a GameObject into the stage's scene at specific position and
        /// rotation.
        /// </summary>
        /// <param name="gameObject">The gameObject to move.</param>
        /// <param name="position">The new world position</param>
        /// <param name="rotation">The new world rotation</param>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <seealso cref="InstantiateIntoStage"/>
        public void MoveIntoStage(GameObject gameObject, Vector3 position, Quaternion rotation, bool persistent = false)
        {
            if (m_GameObjects.Contains(gameObject))
                return;

            SceneManager.MoveGameObjectToScene(gameObject, m_PreviewScene);
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            if (persistent)
                m_PersistentGameObjects.Add(gameObject);
            else
                m_GameObjects.Add(gameObject);

            InitAddedObjectsRecursively(gameObject);
        }

        /// <summary>
        /// Instantiate a scene GameObject or a prefab into the stage's scene.
        /// It is instantiated at origin.
        /// </summary>
        /// <param name="prefabOrSceneObject">The element to instantiate</param>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <returns>The instance</returns>
        /// <seealso cref="MoveIntoStage"/>
        public GameObject InstantiateIntoStage(GameObject prefabOrSceneObject, bool persistent = false)
            => InstantiateIntoStage(prefabOrSceneObject, Vector3.zero, Quaternion.identity, persistent);

        /// <summary>
        /// Instantiate a scene GameObject or a prefab into the stage's scene
        /// at a specific position and rotation.
        /// </summary>
        /// <param name="prefabOrSceneObject">The element to instantiate</param>
        /// <param name="position">The new world position</param>
        /// <param name="rotation">The new world rotation</param>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <returns>The instance</returns>
        /// <seealso cref="MoveIntoStage"/>
        public GameObject InstantiateIntoStage(GameObject prefabOrSceneObject, Vector3 position, Quaternion rotation, bool persistent = false)
        {
            var handle = GameObject.Instantiate(prefabOrSceneObject);
            MoveIntoStage(handle, position, rotation, persistent);
            return handle;
        }

        /// <summary>Create a GameObject into the stage.</summary>
        /// <param name="persistent">
        /// [OPTIONAL] If true, the object is not recreated with the scene update.
        /// Default value: false.
        /// </param>
        /// <returns>The created GameObject</returns>
        public GameObject CreateGameObjectIntoStage(bool persistent = false)
        {
            var handle = new GameObject();
            MoveIntoStage(handle, persistent);
            return handle;
        }

        /// <summary>Clear all scene object except camera.</summary>
        /// <param name="persistent">
        /// [OPTIONAL] If true, clears also persistent objects.
        /// Default value: false.
        /// </param>
        public void Clear(bool persistent = false)
        {
            foreach (var go in m_GameObjects)
                UnityEngine.Object.DestroyImmediate(go);
            m_GameObjects.Clear();

            if (persistent)
            {
                foreach (var go in m_PersistentGameObjects)
                    UnityEngine.Object.DestroyImmediate(go);
                m_PersistentGameObjects.Clear();
            }
        }

        static void InitAddedObjectsRecursively(GameObject go)
        {
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = k_PreviewCullingLayerIndex;
            foreach (Transform child in go.transform)
                InitAddedObjectsRecursively(child.gameObject);
        }

        /// <summary>Changes stage scene's objects visibility.</summary>
        /// <param name="visible">
        /// True: make them visible.
        /// False: hide them.
        /// </param>
        public void SetGameObjectVisible(bool visible)
        {
            foreach (GameObject go in m_GameObjects)
            {
                if (go == null || go.Equals(null))
                    continue;
                foreach (UnityEngine.Renderer renderer in go.GetComponentsInChildren<UnityEngine.Renderer>())
                    renderer.enabled = visible;
                foreach (Light light in go.GetComponentsInChildren<Light>())
                    light.enabled = visible;
            }

            // in case we add camera frontal light and such
            foreach (UnityEngine.Renderer renderer in m_Camera.GetComponentsInChildren<UnityEngine.Renderer>())
                renderer.enabled = visible;
            foreach (Light light in m_Camera.GetComponentsInChildren<Light>())
                light.enabled = visible;
        }
        
        private bool disposedValue = false; // To detect redundant calls

        void CleanUp()
        {
            if (!disposedValue)
            {
                if (SRI != null)
                    SRI.SRPData = null;
                SRI = null;
                EditorSceneManager.ClosePreviewScene(m_PreviewScene);
                
                disposedValue = true;
            }
        }
        
        ~Stage() => CleanUp();

        /// <summary>Clear and close the stage's scene.</summary>
        public void Dispose()
        {
            CleanUp();
            GC.SuppressFinalize(this);
        }
    }
    
    class StageCache : IDisposable
    {
        const string firstStageName = "LookDevFirstView";
        const string secondStageName = "LookDevSecondView";

        Stage[] m_Stages;
        Context m_Contexts;

        public Stage this[ViewIndex index]
            => m_Stages[(int)index];

        public bool initialized { get; private set; }

        public StageCache(IDataProvider dataProvider, Context contexts)
        {
            m_Contexts = contexts;
            m_Stages = new Stage[2]
            {
                InitStage(ViewIndex.First, dataProvider),
                InitStage(ViewIndex.Second, dataProvider)
            };
            initialized = true;
        }
        
        Stage InitStage(ViewIndex index, IDataProvider dataProvider)
        {
            Stage stage;
            switch (index)
            {
                case ViewIndex.First:
                    stage = new Stage(firstStageName);
                    stage.camera.backgroundColor = Compositer.firstViewGizmoColor;
                    break;
                case ViewIndex.Second:
                    stage = new Stage(secondStageName);
                    stage.camera.backgroundColor = Compositer.secondViewGizmoColor;
                    break;
                default:
                    throw new ArgumentException("Unknown ViewIndex: " + index);
            }

            dataProvider.FirstInitScene(stage.runtimeInterface);
            return stage;
        }

        public void UpdateSceneObjects(ViewIndex index)
        {
            Stage stage = this[index];
            stage.Clear();

            var viewContent = m_Contexts.GetViewContent(index);
            if (viewContent == null)
            {
                viewContent.viewedInstanceInPreview = null;
                return;
            }

            if (viewContent.viewedObjectReference != null && !viewContent.viewedObjectReference.Equals(null))
                viewContent.viewedInstanceInPreview = stage.InstantiateIntoStage(viewContent.viewedObjectReference);
        }
        
        public void UpdateSceneLighting(ViewIndex index, IDataProvider provider)
        {
            Stage stage = this[index];
            Environment environment = m_Contexts.GetViewContent(index).environment;
            provider.UpdateSky(stage.camera,
                environment?.sky,
                stage.runtimeInterface);
        }
        
        private bool disposedValue = false; // To detect redundant calls

        void CleanUp()
        {
            if (!disposedValue)
            {
                foreach (Stage stage in m_Stages)
                    stage.Dispose();

                disposedValue = true;
            }
        }

        ~StageCache() => CleanUp();

        public void Dispose()
        {
            CleanUp();
            GC.SuppressFinalize(this);
        }
    }
}
