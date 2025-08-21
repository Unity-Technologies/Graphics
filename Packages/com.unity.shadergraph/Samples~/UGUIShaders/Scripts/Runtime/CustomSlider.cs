using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.UI.Shaders.Sample
{
    /// <summary>
    /// A custom Slider.
    /// <para>Unlike a <see cref="Slider"/>, this uses only a single RectTransform.</para>
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/Slider")]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    public class CustomSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, IMaterialModifier
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }

        static Vector2 FromDirection(Direction direction) => direction switch
        {
            Direction.LeftToRight => Vector2.right,
            Direction.RightToLeft => Vector2.left,
            Direction.BottomToTop => Vector2.up,
            Direction.TopToBottom => Vector2.down,
            _ => Vector2.zero
        };

        [SerializeField] Direction _direction;
        [SerializeField, Range(0f, 1f)] float _value = 0.5f;

        public UnityEvent<int> onStateChanged;
        public UnityEvent<float> onValueChanged;

        private static readonly string _statePropertyName = "_State";
        private static readonly string _sliderValuePropertyName = "_SliderValue";

        private Material _material;
        float stepSize = 0.1f;
        Vector2 _rangeOffset = Vector2.zero;

        public Direction direction => _direction;

        private int? _statePropertyId, _sliderValuePropertyId;
        private int StatePropertyId
        {
            get
            {
                if (!_statePropertyId.HasValue)
                    _statePropertyId = Shader.PropertyToID(_statePropertyName);
                return _statePropertyId.Value;
            }
        }

        private int SliderValuePropertyId
        {
            get
            {
                if (!_sliderValuePropertyId.HasValue)
                    _sliderValuePropertyId = Shader.PropertyToID(_sliderValuePropertyName);
                return _sliderValuePropertyId.Value;
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

        private Canvas RootCanvas => Graphic.canvas.rootCanvas;

        private RectTransform rectTransform => Graphic.rectTransform;

        public float Value
        {
            get => _value;
            private set
            {
                _value = value;
                Graphic.SetMaterialDirty();
                onValueChanged?.Invoke(value);
            }
        }

        Vector2 DirectionVector => FromDirection(direction);

        Vector3 Vector => new Vector3(Value, DirectionVector.x, DirectionVector.y);


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
            // we don't need a transition by default
            transition = Transition.None;
            Value = _value;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            //Graphic.SetMaterialDirty(); // this makes the Editor hick up when changing the value
            // So instead, we just change the material, but if another IMaterialModifier was added after this,
            // this will have no visible effect.
            if (_material != null && _material.HasVector(SliderValuePropertyId))
                _material.SetVector(SliderValuePropertyId, Vector);
        }
#endif

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            onStateChanged?.Invoke((int)state);
            Graphic.SetMaterialDirty();
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            _material ??= new(baseMaterial);

            _material.SetFloat(StatePropertyId, (int)currentSelectionState);
            _material.SetVector(SliderValuePropertyId, Vector);

            return _material;
        }

        /// <summary>
        /// <see cref="Slider.UpdateDrag(PointerEventData, Camera)"/>
        /// </summary>
        void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, cam, out Vector2 localPoint))
            {
                Debug.LogWarning("missed!?");
                return;
            }

            float pivot = rectTransform.pivot[(int)axis];
            float size = rectTransform.rect.size[(int)axis];
            (float min, float max) range = (size * -pivot, size * (1 - pivot));
            float val = Mathf.InverseLerp(range.min, range.max, localPoint[(int)axis]);

            Value = reverseValue ? 1 - val : val;
        }

        /// <summary>
        /// <see cref="Slider.MayDrag(PointerEventData eventData)"/>
        /// </summary>
        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        Axis axis { get { return (direction == Direction.LeftToRight || direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        bool reverseValue { get { return direction == Direction.RightToLeft || direction == Direction.TopToBottom; } }

        /// <summary>
        /// See <see cref="Slider.OnMove(AxisEventData)"/>
        /// </summary>
        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (axis == Axis.Horizontal && FindSelectableOnLeft() == null)
                        Value = reverseValue ? Value + stepSize : Value - stepSize;
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (axis == Axis.Horizontal && FindSelectableOnRight() == null)
                        Value = reverseValue ? Value - stepSize : Value + stepSize;
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (axis == Axis.Vertical && FindSelectableOnUp() == null)
                        Value = reverseValue ? Value - stepSize : Value + stepSize;
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (axis == Axis.Vertical && FindSelectableOnDown() == null)
                        Value = reverseValue ? Value + stepSize : Value - stepSize;
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        /// <summary>
        /// See <see cref="Selectable.FindSelectableOnLeft"/>
        /// </summary>
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        /// <summary>
        /// See <see cref="Selectable.FindSelectableOnRight"/>
        /// </summary>
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        /// <summary>
        /// See <see cref="Selectable.FindSelectableOnUp"/>
        /// </summary>
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        /// <summary>
        /// See <see cref="Selectable.FindSelectableOnDown"/>
        /// </summary>
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && axis == Axis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/ShaderGraph Samples/Slider", false, 30)]
        static void CreateToggleGameObject(MenuCommand command)
        {
            GameObject go = ObjectFactory.CreateGameObject("Slider", new Type[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectTransformSize), typeof(CustomSlider) });
            StageUtility.PlaceGameObjectInCurrentStage(go);
            go.GetComponent<CustomSlider>().AssignDefaultMaterial();

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