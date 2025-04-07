using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
#endif

namespace Unity.UI.Shaders.Sample
{
    /// <summary>
    /// A component that works like a <see cref="Button"/> implementing <see cref="IMaterialModifier"/> to update the <see cref="Graphic.material"/> when
    /// <see cref="Selectable.DoStateTransition(Selectable.SelectionState, bool)"/> is called.
    /// <para>Implemented as a <see cref="Selectable"/>.</para>
    /// </summary>
    [AddComponentMenu("UI/ShaderGraph Samples/Button")]
    public class CustomButton : Selectable, IPointerClickHandler, ISubmitHandler /*Button*/, IMaterialModifier
    {
        public UnityEvent onClick;
        public UnityEvent<int> onStateChanged;

        private static readonly string _statePropertyName = "_State";
        
        private Material _material;

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
#endif

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            onStateChanged?.Invoke((int)state);
            Graphic.SetMaterialDirty();
        }
        
        void Click()
        {
            if (IsActive() && IsInteractable())
                onClick?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;
            Click();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Click();
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            _material = new Material(baseMaterial);

            _material.SetFloat(StatePropertyId, (int)currentSelectionState);

            return _material;
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/UI/ShaderGraph Samples/Button", false, 30)]
        static void CreateButtonGameObject(MenuCommand command)
        {
            GameObject go = ObjectFactory.CreateGameObject("SG Button", new Type[] { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CustomButton) });
            StageUtility.PlaceGameObjectInCurrentStage(go);
            go.GetComponent<CustomButton>().AssignDefaultMaterial();

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
