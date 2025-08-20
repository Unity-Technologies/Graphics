using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.UI.Shaders.Sample
{
    /// <summary>
    /// A simple Meter.
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/Meter")]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class Meter : UIBehaviour, IMaterialModifier
    {
        [SerializeField, Range(0f, 1f)] private float _value = 0.5f;

        private static readonly string _sliderValuePropertyName = "_MeterValue";

        protected Material _material;

        private int? _meterValuePropertyId;

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

        public float Value
        {
            get => _value;
            set
            {
                _value = value;
                Graphic.SetMaterialDirty();
            }
        }

        protected int MeterValuePropertyId
        {
            get
            {
                if (!_meterValuePropertyId.HasValue)
                    _meterValuePropertyId = Shader.PropertyToID(_sliderValuePropertyName);
                return _meterValuePropertyId.Value;
            }
        }

#if UNITY_EDITOR
        [SerializeField, HideInInspector] Material defaultMaterial;

        protected internal void AssignDefaultMaterial()
        {
            if (defaultMaterial != null)
                Graphic.material = defaultMaterial;
        }

        protected override void Reset()
        {
            base.Reset();
            Value = _value;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            //Graphic.SetMaterialDirty(); // this makes the Editor hick up when changing the value
            // So instead, we just change the material, but if another IMaterialModifier was added after this,
            // this will have no visible effect.
            if (_material != null && _material.HasFloat(MeterValuePropertyId))
                _material.SetFloat(MeterValuePropertyId, Value);
        }
#endif

        protected override void Start()
        {
            base.Start();
            Value = _value;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Value = _value;
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            _material ??= new(baseMaterial);

            if (_material.HasFloat(MeterValuePropertyId))
                _material.SetFloat(MeterValuePropertyId, Value);

            return _material;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/ShaderGraph Samples/Meter", false, 30)]
        static void CreateToggleGameObject(MenuCommand command)
        {
            GameObject go = ObjectFactory.CreateGameObject("Meter", new System.Type[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectTransformSize), typeof(Meter) });
            StageUtility.PlaceGameObjectInCurrentStage(go);
            go.GetComponent<Meter>().AssignDefaultMaterial();

            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            GameObject contextObject = command.context as GameObject;
            if (contextObject != null)
            {
                GameObjectUtility.SetParentAndAlign(go, contextObject);
                Undo.SetTransformParent(go.transform, contextObject.transform, "Parent " + go.name);
            }

            Selection.activeGameObject = go;
        }
#endif
    }
}
