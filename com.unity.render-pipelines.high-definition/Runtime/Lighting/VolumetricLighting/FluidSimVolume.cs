using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct FluidSimVolumeArtistParameters
    {
        [SerializeField]
        public int workflow;
        [SerializeField]
        public float loopTime;
        [SerializeField]
        public bool volumetricShadowing;

        [SerializeField]
        public Texture3D initialStateTexture;
        [SerializeField]
        public Texture3D initialVectorField;
        [NonSerialized]
        public Texture3D vectorField;
        [NonSerialized]
        public Texture3D vectorFieldNext;
        [SerializeField]
        public float vectorFieldSpeed;
        [SerializeField]
        public int numVectorFields;
        [SerializeField]
        public float vfFrameTime;

        [SerializeField]
        public Texture3D initialDensityTexture;
        [NonSerialized]
        public Texture3D DensityTexture;
        [NonSerialized]
        public Texture3D DensityTextureNext;
        [SerializeField]
        public int numDensityTextures;
        [SerializeField]
        public float adFrameTime;

        [SerializeField]
        public Vector3 positiveFade;
        [SerializeField]
        public Vector3 negativeFade;

        [SerializeField]
        internal float m_EditorUniformFade;
        [SerializeField]
        internal Vector3 m_EditorPositiveFade;
        [SerializeField]
        internal Vector3 m_EditorNegativeFade;
        [SerializeField]
        internal bool m_EditorAdvancedFade;

        public Vector3 size;

        public float distanceFadeStart;
        public float distanceFadeEnd;

        public int textureIndex;

        public FluidSimVolumeArtistParameters(int anything)
        {
            workflow = (int)FluidSimVolume.Workflow.VectorField;
            loopTime = 0.0f;
            volumetricShadowing = true;

            initialStateTexture = null;
            initialVectorField = null;
            vectorField = null;
            vectorFieldNext = null;
            vectorFieldSpeed = 1.0f;
            numVectorFields = 1;
            vfFrameTime = 1.0f;

            initialDensityTexture = null;
            DensityTexture = null;
            DensityTextureNext = null;
            numDensityTextures = 1;
            adFrameTime = 1.0f;

            size = Vector3.one;

            positiveFade = Vector3.zero;
            negativeFade = Vector3.zero;

            distanceFadeStart = 10000;
            distanceFadeEnd = 10000;

            m_EditorPositiveFade = Vector3.zero;
            m_EditorNegativeFade = Vector3.zero;
            m_EditorUniformFade = 0;
            m_EditorAdvancedFade = false;

            textureIndex = -1;
        }

        public FluidSimVolumeEngineData ConvertToEngineData()
        {
            FluidSimVolumeEngineData data = new FluidSimVolumeEngineData();

            // todo : implement it!

            data.volumeRes = new Vector3(1.0f, 1.0f, 1.0f);
            data.volumetricShadowing = volumetricShadowing ? 1 : 0;
            data.textureIndex = -1;

            if (workflow == (int)FluidSimVolume.Workflow.VectorField)
            {
                if (initialStateTexture != null)
                {
                    data.volumeRes = new Vector3(
                        initialStateTexture.width,
                        initialStateTexture.height,
                        initialStateTexture.depth);

                    data.textureIndex = 0;
                }
            }
            else
            {
                if (initialDensityTexture != null)
                {
                    data.volumeRes = new Vector3(
                        initialDensityTexture.width,
                        initialDensityTexture.height,
                        initialDensityTexture.depth);

                    data.textureIndex = 0;
                }
            }

            return data;
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Fluid Simulation Volume", 1200)]
    public class FluidSimVolume : MonoBehaviour
    {
        public enum Workflow
        {
            VectorField,
            AnimatedDensity,
        }

        public FluidSimVolumeArtistParameters parameters = new FluidSimVolumeArtistParameters(-1);

        public RTHandleSystem.RTHandle simulationBuffer0 = null;
        public RTHandleSystem.RTHandle simulationBuffer1 = null;

        public bool needToInitialize { get; private set; }

        public float frameBlend { get; private set; }

        private List<Texture3D> _volumeTexList = new List<Texture3D>();

        private float _playingTime = 0.0f;
        private float _frameElapsedTime = 0.0f;
        private int   _frameIndex = 0;

        private AssetBundle _vectorFieldBundles = null;
        private AssetBundle _animatedDensityBundles = null;

        private bool _needToUpdateVolumeTexList = true;

        private void Start()
        {
            needToInitialize = true;
            _needToUpdateVolumeTexList = true;
        }

        public FluidSimVolume()
        {
        }

        private void OnEnable()
        {
            needToInitialize = true;
            _needToUpdateVolumeTexList = true;
            FluidSimVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            needToInitialize = false;
            _needToUpdateVolumeTexList = false;
            FluidSimVolumeManager.manager.DeRegisterVolume(this);

            UnloadVolumeTexBundles();
        }

        private void OnDestroy()
        {
            UnloadVolumeTexBundles();
        }

        private void LoadVolumeTexturesBundles(bool vfWorkflow)
        {
            if (_vectorFieldBundles == null)
                _vectorFieldBundles = AssetBundle.LoadFromFile("Assets/VolumeTextures/vectorfields");
            if (_animatedDensityBundles == null)
                _animatedDensityBundles = AssetBundle.LoadFromFile("Assets/VolumeTextures/animdensities");
        }
        private void UnloadVolumeTexBundles()
        {
            if (_vectorFieldBundles != null || _animatedDensityBundles != null)
            {
                AssetBundle.UnloadAllAssetBundles(true);
                _vectorFieldBundles = null;
                _animatedDensityBundles = null;
            }
        }
        private void UpdateVolumeTexList()
        {
            bool vfWorkflow = parameters.workflow == (int)Workflow.VectorField;

            LoadVolumeTexturesBundles(vfWorkflow);

            var initialTexture = vfWorkflow ? parameters.initialVectorField : parameters.initialDensityTexture;

            var numVolumeTexs = vfWorkflow ? parameters.numVectorFields : parameters.numDensityTextures;
            var volumeTexName = initialTexture != null ? initialTexture.name : "";

            if (vfWorkflow)
                volumeTexName = volumeTexName.Substring(0, volumeTexName.Length - 1);
            else
                volumeTexName = volumeTexName.Substring(0, volumeTexName.Length - 11);

            //Debug.Log(volumeTexName);

            _volumeTexList.Clear();

            var res    = vfWorkflow ? parameters.initialVectorField.width  : parameters.initialDensityTexture.width;
            var format = vfWorkflow ? parameters.initialVectorField.format : parameters.initialDensityTexture.format;
            var ext    = vfWorkflow ? ".vf" : "_Texture3D.asset";

            for (int i = 1; i < numVolumeTexs; i++)
            {
                var textureName = volumeTexName + i + ext;
                Texture3D volumeTex = LoadTexture3DFromBundles(vfWorkflow, textureName);

                if (volumeTex == null)
                {
                    Debug.Log("Check number of volume textures.");
                    break;
                }

                //Debug.Log("Loaded: " + volumeTex.name);

                _volumeTexList.Add(volumeTex);
            }

            //foreach (var tex in _volumeTexList)
            //    Debug.Log("volume tex list: " + tex.name);
        }
        private Texture3D LoadTexture3DFromBundles(bool vfWorkflow, string textureName)
        {
            Texture3D tex = vfWorkflow ?
                _vectorFieldBundles.LoadAsset<Texture3D>(textureName) :
                _animatedDensityBundles.LoadAsset<Texture3D>(textureName);

            //Debug.Log(tex.dimension);

            return tex;
        }

        public void NotifyToUpdateVolumeTexList()
        {
            _needToUpdateVolumeTexList = true;
        }

        private void Update()
        {
            _playingTime += Time.deltaTime;

            RecreateSimulationBuffersIfNeeded();

            UpdateLoopTime();

            if (_needToUpdateVolumeTexList)
            {
                UpdateVolumeTexList();
                _needToUpdateVolumeTexList = false;
            }

            if (parameters.workflow == (int)Workflow.VectorField)
                UpdateMultipleVectorFields();
            else
                UpdateMultipleAnimatedDensities();
        }

        private void RecreateSimulationBuffersIfNeeded()
        {
            bool needToRecreate =
                parameters.initialStateTexture != null && (
                simulationBuffer0 == null ||
                parameters.initialStateTexture.width  != simulationBuffer0.rt.width ||
                parameters.initialStateTexture.height != simulationBuffer0.rt.height ||
                parameters.initialStateTexture.depth  != simulationBuffer0.rt.volumeDepth);

            if (needToRecreate)
            {
                needToInitialize = true;

                if (simulationBuffer0 != null)
                    RTHandles.Release(simulationBuffer0);
                if (simulationBuffer1 != null)
                    RTHandles.Release(simulationBuffer1);

                simulationBuffer0 = RTHandles.Alloc(
                    parameters.initialStateTexture.width,
                    parameters.initialStateTexture.height,
                    parameters.initialStateTexture.depth,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    filterMode: FilterMode.Bilinear,
                    dimension: TextureDimension.Tex3D,
                    enableRandomWrite: true,
                    name: "SimulationBuffer0");

                simulationBuffer1 = RTHandles.Alloc(
                    parameters.initialStateTexture.width,
                    parameters.initialStateTexture.height,
                    parameters.initialStateTexture.depth,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    filterMode: FilterMode.Bilinear,
                    dimension: TextureDimension.Tex3D,
                    enableRandomWrite: true,
                    name: "SimulationBuffer1");
            }
            else
            {
                needToInitialize = false;
            }
        }
        private void UpdateLoopTime()
        {
            if (parameters.loopTime > 0.0 && _playingTime >= parameters.loopTime)
            {
                _playingTime = 0.0f;
                needToInitialize = true;
            }
        }
        private void UpdateMultipleVectorFields()
        {
            float frameTime = parameters.vfFrameTime;

            if (parameters.numVectorFields >= 2)
            {
                _frameElapsedTime += Time.deltaTime;
                if (_frameElapsedTime >= frameTime)
                {
                    _frameElapsedTime = 0.0f;
                    _frameIndex++;

                    if (_frameIndex >= parameters.numVectorFields)
                        _frameIndex = 0;
                    if (_frameIndex > _volumeTexList.Count)
                        _frameIndex = 0;
                }

                frameBlend = _frameElapsedTime / frameTime;

                int frameIndexNext = Mathf.Min(_frameIndex + 1, _volumeTexList.Count);

                parameters.vectorField     = _frameIndex == 0 ? parameters.initialVectorField : _volumeTexList[_frameIndex - 1];
                parameters.vectorFieldNext = _volumeTexList[frameIndexNext - 1];

                //Debug.Log("FrameIndex: " + _frameIndex + ", " + "VectorFieldName: " + parameters.vectorField.name);
            }
            else
            {
                frameBlend = 0.0f;

                parameters.vectorField     = parameters.initialVectorField;
                parameters.vectorFieldNext = parameters.initialVectorField;
            }
        }
        private void UpdateMultipleAnimatedDensities()
        {
            float frameTime = parameters.adFrameTime;

            if (parameters.numDensityTextures >= 2)
            {
                _frameElapsedTime += Time.deltaTime;
                if (_frameElapsedTime >= frameTime)
                {
                    _frameElapsedTime = 0.0f;
                    _frameIndex++;

                    if (_frameIndex >= parameters.numDensityTextures)
                        _frameIndex = 0;
                    if (_frameIndex > _volumeTexList.Count)
                        _frameIndex = 0;
                }

                frameBlend = _frameElapsedTime / frameTime;

                int frameIndexNext = Mathf.Min(_frameIndex + 1, _volumeTexList.Count);

                parameters.DensityTexture     = _frameIndex == 0 ? parameters.initialDensityTexture : _volumeTexList[_frameIndex - 1];
                parameters.DensityTextureNext = _volumeTexList[frameIndexNext - 1];

                //Debug.Log("FrameIndex: " + _frameIndex + ", " + "Anim Densities: " + parameters.DensityTexture.name);
            }
            else
            {
                frameBlend = 0.0f;

                parameters.DensityTexture     = parameters.initialDensityTexture;
                parameters.DensityTextureNext = parameters.initialDensityTexture;
            }
        }
    }
}
