using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct FluidSimVolumeArtistParameters
    {
        [SerializeField]
        public Texture3D initialStateTexture;
        [SerializeField]
        public Texture3D initialVectorField;
        [NonSerialized]
        public Texture3D vectorField;
        [SerializeField]
        public float vectorFieldSpeed;
        [SerializeField]
        public int numVectorFields;
        [SerializeField]
        public float loopTime;

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
            initialStateTexture = null;
            initialVectorField = null;
            vectorField = null;
            vectorFieldSpeed = 1.0f;
            numVectorFields = 1;
            loopTime = 0.0f;

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

            if (initialStateTexture != null)
            {
                data.volumeRes = new Vector3(
                    initialStateTexture.width,
                    initialStateTexture.height,
                    initialStateTexture.depth);

                data.textureIndex = 0;
            }
            else
            {
                data.volumeRes = new Vector3(1.0f, 1.0f, 1.0f);
                data.textureIndex = -1;
            }

            return data;
        }
    }

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Fluid Simulation Volume", 1200)]
    public class FluidSimVolume : MonoBehaviour
    {
        public FluidSimVolumeArtistParameters parameters = new FluidSimVolumeArtistParameters(-1);

        public RTHandleSystem.RTHandle simulationBuffer0 = null;
        public RTHandleSystem.RTHandle simulationBuffer1 = null;

        public bool needToInitialize { get; private set; }

        private List<Texture3D> _volumeTexList = new List<Texture3D>();

        private float _playingTime = 0.0f;
        private float _vectorFieldElapsedTime = 0.0f;
        private int _vectorFieldIndex = 0;

        private AssetBundle _vectorFieldBundles = null;

        private void Start()
        {
            needToInitialize = true;
            UpdateVolumeTexList();
        }

        public FluidSimVolume()
        {
        }

        private void OnEnable()
        {
            needToInitialize = true;
            UpdateVolumeTexList();
            FluidSimVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            needToInitialize = false;
            UnloadVectorFieldBundles();
            FluidSimVolumeManager.manager.DeRegisterVolume(this);
        }

        private void LoadVectorFieldBundles()
        {
            if (_vectorFieldBundles == null)
                _vectorFieldBundles = AssetBundle.LoadFromFile("Assets/VolumeTextures/vectorfields");
        }
        private void UnloadVectorFieldBundles()
        {
            if (_vectorFieldBundles != null)
            {
                AssetBundle.UnloadAllAssetBundles(true);
                _vectorFieldBundles = null;
            }
        }
        private void UpdateVolumeTexList()
        {
            LoadVectorFieldBundles();

            // todo : can get from vector fields or animated density.
            var numVolumeTexs = parameters.numVectorFields;
            var volumeTexName = parameters.initialVectorField.name;
            volumeTexName = volumeTexName.Substring(0, volumeTexName.Length - 1);

            //Debug.Log(volumeTexName);

            _volumeTexList.Clear();

            for (int i = 1; i < numVolumeTexs; i++)
            {
                var res = parameters.initialVectorField.width;
                var format = parameters.initialVectorField.format;
                var volumeTex = _vectorFieldBundles.LoadAsset<Texture3D>(volumeTexName + i + ".vf");

                _volumeTexList.Add(volumeTex);
            }
        }

        private void Update()
        {
            bool recreate =
                parameters.initialStateTexture != null && (
                simulationBuffer0 == null ||
                parameters.initialStateTexture.width  != simulationBuffer0.rt.width ||
                parameters.initialStateTexture.height != simulationBuffer0.rt.height ||
                parameters.initialStateTexture.depth  != simulationBuffer0.rt.volumeDepth);

            if (recreate)
            {
                needToInitialize = true;
                RecreateSimulationBuffers();
            }
            else
            {
                needToInitialize = false;
            }

            _playingTime += Time.deltaTime;

            if (parameters.loopTime > 0.0 && _playingTime >= parameters.loopTime)
            {
                _playingTime = 0.0f;
                needToInitialize = true;
            }

            UpdateMultipleVectorFields();
        }

        private void RecreateSimulationBuffers()
        {
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
        private void UpdateMultipleVectorFields()
        {
            if (parameters.numVectorFields >= 2)
            {
                _vectorFieldElapsedTime += Time.deltaTime;
                if (_vectorFieldElapsedTime >= 1.0f)
                {
                    _vectorFieldElapsedTime = 0.0f;
                    _vectorFieldIndex++;
                    if (_vectorFieldIndex >= parameters.numVectorFields)
                        _vectorFieldIndex = 0;
                }

                if (_vectorFieldIndex == 0)
                {
                    parameters.vectorField = parameters.initialVectorField;
                }
                else
                {
                    parameters.vectorField = _volumeTexList[_vectorFieldIndex - 1];
                }
                //Debug.Log("VectorFieldName: " + parameters.vectorField.name);
            }
            else
            {
                parameters.vectorField = parameters.initialVectorField;
            }
        }
    }
}
