using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Unity.UI.Shaders.Sample
{
    /// <summary>
    /// Example script controlling a BlurredHexagon Material with <see cref="Slider"/>s.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class BlurredHexagonController : UIBehaviour, IMaterialModifier
    {
        const string _saturationRef = "_Background_Saturation", _tintRef = "_Tint", _hexagonDissolveRef = "_Hexagon_Dissolve",
            _hexagonTilesRef = "_Hexagon_Tiles", _blurRef = "_Blur", _blurCyclesRef = "_Blur_Cycles", _blurSamplesPerCycleRef = "_Blur_Samples_Per_Cycle";

        [SerializeField] Slider _saturationSlider, _tintRedSlider, _tintGreenSlider, _tintBlueSlider,
            _hexagonDissolveSlider, _hexagonTilesSlider, _blurSlider, _blurCyclesSlider, _blurSamplesPerCycleSlider;

        static int _saturationId, _tintId, _hexagonDissolveId, _hexagonTilesId, _blurId, _blurCyclesId, _blurSamplesPerCycleId;

        private float _saturation, _hexagonDissolve, _hexagonTiles, _blur, _blurCycles, _blurSamplesPerCycle;
        private Color _tint = Color.gray;

        public float Saturation
        {
            get => _saturation;
            set
            {
                _saturation = value;
                Graphic.SetMaterialDirty();
            }
        }

        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                Graphic.SetMaterialDirty();
            }
        }

        public float TintRed { get => Tint.r; set => Tint = new Color(value, _tint.g, _tint.b); }
        public float TintGreen { get => Tint.g; set => Tint = new Color(_tint.r, value, _tint.b); }
        public float TintBlue { get => Tint.b; set => Tint = new Color(_tint.r, _tint.g, value); }

        public float HexagonDissolve
        {
            get => _hexagonDissolve;
            set
            {
                _hexagonDissolve = value;
                Graphic.SetMaterialDirty();
            }
        }

        public float HexagonTiles
        {
            get => _hexagonTiles;
            set
            {
                _hexagonTiles = value;
                Graphic.SetMaterialDirty();
            }
        }

        public float Blur
        {
            get => _blur;
            set
            {
                _blur = value;
                Graphic.SetMaterialDirty();
            }
        }

        public float BlurCycles
        {
            get => _blurCycles;
            set
            {
                _blurCycles = value;
                Graphic.SetMaterialDirty();
            }
        }

        public float BlurSamplesPerCycle
        {
            get => _blurSamplesPerCycle;
            set
            {
                _blurSamplesPerCycle = value;
                Graphic.SetMaterialDirty();
            }
        }

        private Graphic _graphic;
        public Graphic Graphic
        {
            get
            {
                if (_graphic == null)
                    _graphic = GetComponent<Graphic>();
                return _graphic;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // get ids from prop names
            _saturationId = Shader.PropertyToID(_saturationRef);
            _tintId = Shader.PropertyToID(_tintRef);
            _hexagonDissolveId = Shader.PropertyToID(_hexagonDissolveRef);
            _hexagonTilesId = Shader.PropertyToID(_hexagonTilesRef);
            _blurId = Shader.PropertyToID(_blurRef);
            _blurCyclesId = Shader.PropertyToID(_blurCyclesRef);
            _blurSamplesPerCycleId = Shader.PropertyToID(_blurSamplesPerCycleRef);

            // initialize values with the material's values
            var mat = Graphic.material;
            _saturation = mat.GetFloat(_saturationId);
            _tint = mat.GetColor(_tintId);
            _hexagonDissolve = mat.GetFloat(_hexagonDissolveId);
            _hexagonTiles = mat.GetFloat(_hexagonTilesId);
            _blur = mat.GetFloat(_blurId);
            _blurCycles = mat.GetFloat(_blurCyclesId);
            _blurSamplesPerCycle = mat.GetFloat(_blurSamplesPerCycleId);

            // initialize the sliders with the material's values
            _saturationSlider?.SetValueWithoutNotify(Saturation);
            _tintRedSlider?.SetValueWithoutNotify(TintRed);
            _tintGreenSlider?.SetValueWithoutNotify(TintGreen);
            _tintBlueSlider?.SetValueWithoutNotify(TintBlue);
            _hexagonDissolveSlider?.SetValueWithoutNotify(HexagonDissolve);
            _hexagonTilesSlider?.SetValueWithoutNotify(HexagonTiles);
            _blurSlider?.SetValueWithoutNotify(Blur);
            _blurCyclesSlider?.SetValueWithoutNotify(BlurCycles);
            _blurSamplesPerCycleSlider?.SetValueWithoutNotify(BlurSamplesPerCycle);

            // add listeners to sliders' value changed events
            _saturationSlider?.onValueChanged.AddListener((x) => { Saturation = x; });
            _tintRedSlider?.onValueChanged?.AddListener((x) => { TintRed = x; });
            _tintGreenSlider?.onValueChanged?.AddListener((x) => { TintGreen = x; });
            _tintBlueSlider?.onValueChanged?.AddListener((x) => { TintBlue = x; });
            _hexagonDissolveSlider?.onValueChanged.AddListener((x) => { HexagonDissolve = x; });
            _hexagonTilesSlider?.onValueChanged.AddListener((x) => { HexagonTiles = x; });
            _blurSlider?.onValueChanged.AddListener((x) => { Blur = x; });
            _blurCyclesSlider?.onValueChanged.AddListener((x) => { BlurCycles = x; });
            _blurSamplesPerCycleSlider?.onValueChanged.AddListener((x) => { BlurSamplesPerCycle = x; });
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            // nothing's initialized unless we're in play mode
            if (!Application.isPlaying || enabled == false)
                return baseMaterial;

            baseMaterial.SetFloat(_saturationId, Saturation);
            baseMaterial.SetColor(_tintId, Tint);
            baseMaterial.SetFloat(_hexagonDissolveId, HexagonDissolve);
            baseMaterial.SetFloat(_hexagonTilesId, HexagonTiles);
            baseMaterial.SetFloat(_blurId, Blur);
            baseMaterial.SetFloat(_blurCyclesId, BlurCycles);
            baseMaterial.SetFloat(_blurSamplesPerCycleId, BlurSamplesPerCycle);
            return baseMaterial;
        }
    }
}
