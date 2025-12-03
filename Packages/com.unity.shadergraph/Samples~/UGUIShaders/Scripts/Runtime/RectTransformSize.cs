using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.UI.Shaders.Sample
{
    /// <summary>
    /// Sets the UI Material's Property to the <see cref="UnityEngine.RectTransform"/>'s screen size.
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/RectTransform Size")]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    [DisallowMultipleComponent]
    public class RectTransformSize : UIBehaviour, IMaterialModifier
    {
        private static readonly string _propertyName = "_RectTransformInfo";

        Material _material;

        private RectTransform _rectTransform;
        private RectTransform RectTransform
        {
            get
            {
                _rectTransform = _rectTransform != null ? _rectTransform : GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private Canvas _rootCanvas;
        private Canvas RootCanvas
        {
            get
            {
                if (_rootCanvas == null)
                    _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
                return _rootCanvas;
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

        int? _propertyId;
        int PropertyId
        {
            get
            {
                if (!_propertyId.HasValue)
                    _propertyId = Shader.PropertyToID(_propertyName);
                return _propertyId.Value;
            }
        }

        private Vector2 RectSize => RectTransformUtility.PixelAdjustRect(RectTransform, RootCanvas).size;

        private Vector4 RectTransformInfo
        {
            get
            {
                Vector4 rectTransformInfo = RectSize;
                rectTransformInfo.z = RootCanvas.scaleFactor;
                rectTransformInfo.w = RootCanvas.referencePixelsPerUnit;
                return rectTransformInfo;
            }
        }

        protected override void Start()
        {
            base.Start();
            UpdateMaterial();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            UpdateMaterial();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateMaterial();
        }
#endif

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            UpdateMaterial();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateMaterial();
        }

        [ContextMenu("Update Material")]
        public void UpdateMaterial()
        {
            Graphic.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            _material ??= new(baseMaterial);

            _material.CopyPropertiesFromMaterial(baseMaterial);

            if (_material.HasVector(PropertyId))
                _material.SetVector(PropertyId, RectTransformInfo);

            return _material;
        }
    }
}
