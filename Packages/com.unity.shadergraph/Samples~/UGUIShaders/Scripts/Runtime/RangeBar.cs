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
    /// A simple ProgressBar.
    /// <para>Becomes interactable when associated with a <see cref="CustomSlider"/></para>
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/Range Bar")]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class RangeBar : UIBehaviour, IMaterialModifier
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }

        public struct Range
        {
            public float min, max;
        }

        static Vector2 FromDirection(Direction direction) => direction switch
        {
            Direction.LeftToRight => Vector2.right,
            Direction.RightToLeft => Vector2.left,
            Direction.BottomToTop => Vector2.up,
            Direction.TopToBottom => Vector2.down,
            _ => Vector2.zero
        };

        [SerializeField, MinMaxSlider(0f, 1f)] private Vector2 _value = Vector2.up;
        [SerializeField] Direction _direction;

        private static readonly string _rangeBarValuePropertyName = "_RangeBarValue";

        protected Material _material;

        private int? _rangeBarValuePropertyId;

        public Direction direction => _direction;

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

        public Vector2 Value
        {
            get => _value;
            set
            {
                _value.x = Mathf.Clamp01(value.x);
                _value.y = Mathf.Clamp01(value.y);
                _value.y = _value.y > _value.x ? _value.y : _value.x;
                Graphic.SetMaterialDirty();
            }
        }

        public float Min { get => Value.x; set => Value = new Vector2(value, Value.y); }
        public float Max { get => Value.y; set => Value = new Vector2(Value.x, value); }

        Vector2 DirectionVector => FromDirection(direction);

        Vector4 Vector => new Vector4(Value.x, Value.y, DirectionVector.x, DirectionVector.y);

        protected int RangeBarValuePropertyId
        {
            get
            {
                if (!_rangeBarValuePropertyId.HasValue)
                    _rangeBarValuePropertyId = Shader.PropertyToID(_rangeBarValuePropertyName);
                return _rangeBarValuePropertyId.Value;
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
            if (_material != null && _material.HasVector(RangeBarValuePropertyId))
                _material.SetVector(RangeBarValuePropertyId, Vector);
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

            _material.CopyPropertiesFromMaterial(baseMaterial);

            if (_material.HasVector(RangeBarValuePropertyId))
                _material.SetVector(RangeBarValuePropertyId, Vector);

            return _material;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/ShaderGraph Samples/Range Bar", false, 30)]
        static void CreateToggleGameObject(MenuCommand command)
        {
            GameObject go = ObjectFactory.CreateGameObject("Range Bar", new System.Type[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectTransformSize), typeof(RangeBar) });
            StageUtility.PlaceGameObjectInCurrentStage(go);
            go.GetComponent<RangeBar>().AssignDefaultMaterial();

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
