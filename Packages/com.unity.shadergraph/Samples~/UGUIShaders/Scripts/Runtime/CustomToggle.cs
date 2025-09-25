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
    /// A custom version of <see cref="Toggle"/> implementing <see cref="IMaterialModifier"/> to update the <see cref="Graphic.material"/> when
    /// <see cref="Selectable.DoStateTransition(Selectable.SelectionState, bool)"/> is called, or when
    /// <see cref="Toggle.isOn"/> changes.
    /// <para>Implemented as a variant of <see cref="Toggle"/> to make it work nicely with <see cref="ToggleGroup"/>.</para>
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/Toggle")]
    [RequireComponent(typeof(Graphic))]
    public class CustomToggle : Toggle, IMaterialModifier
    {
        public UnityEvent<int> onStateChanged;
        private static readonly string _isOnPropertyName = "_isOn", _statePropertyName = "_State";
        private Material _material;

        private int? _isOnPropertyId;
        private int IsOnPropertyId
        {
            get
            {
                if (!_isOnPropertyId.HasValue)
                    _isOnPropertyId = Shader.PropertyToID(_isOnPropertyName);
                return _isOnPropertyId.Value;
            }
        }

        private int? _statePropertyId;
        private int StatePropertyId
        {
            get
            {
                if (!_statePropertyId.HasValue)
                    _statePropertyId = Shader.PropertyToID(_statePropertyName);
                return _statePropertyId.Value;
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
            // we don't need a transition by default
            transition = Transition.None;
        }

        protected override void OnValidate()
        {
            //base.OnValidate(); // not calling base not to trigger DoStateTransition()
            if (!PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);

            UpdateMaterial(true);
        }
#endif

        public void UpdateMaterial(bool findGroupToggles = false)
        {
            if (group != null)
            {
                if (findGroupToggles) // only used in Edit mode when ToggleGroup isn't initialized already
                {
                    foreach (var t in FindObjectsByType<CustomToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (t.group == group)
                            t.Graphic.SetMaterialDirty();
                }
                else
                {
                    foreach(var t in group.ActiveToggles())
                        if (t is CustomToggle customToggle)
                            customToggle.Graphic.SetMaterialDirty();
                }
            }
            else
            {
                Graphic.SetMaterialDirty();
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            onStateChanged?.Invoke((int)state);
            UpdateMaterial();
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            _material ??= new(baseMaterial);

            _material.SetFloat(StatePropertyId, (int)currentSelectionState);

            if (_material.HasFloat(IsOnPropertyId))
                _material.SetFloat(IsOnPropertyId, isOn ? 1 : 0);
            return _material;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            UpdateMaterial();
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
            UpdateMaterial();
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/ShaderGraph Samples/Toggle", false, 30)]
        static void CreateToggleGameObject(MenuCommand command)
        {
            GameObject go = ObjectFactory.CreateGameObject("SG Toggle", new Type[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CustomToggle) });
            StageUtility.PlaceGameObjectInCurrentStage(go);
            go.GetComponent<CustomToggle>().AssignDefaultMaterial();

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
