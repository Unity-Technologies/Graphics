using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class IMGUISlotEditorView : DataWatchContainer
    {
        [SerializeField]
        IMGUISlotEditorPresenter m_Presenter;

        ConcreteSlotValueType m_CurrentValueType = ConcreteSlotValueType.Error;

        public override void OnDataChanged()
        {
            if (presenter == null)
            {
                Clear();
                return;
            }

            if (presenter.slot.concreteValueType == m_CurrentValueType)
                return;

            Clear();
            m_CurrentValueType = presenter.slot.concreteValueType;

            Action onGUIHandler;
            if (presenter.slot.concreteValueType == ConcreteSlotValueType.Vector4)
                onGUIHandler = Vector4OnGUIHandler;
            else if (presenter.slot.concreteValueType == ConcreteSlotValueType.Vector3)
                onGUIHandler = Vector3OnGUIHandler;
            else if (presenter.slot.concreteValueType == ConcreteSlotValueType.Vector2)
                onGUIHandler = Vector2OnGUIHandler;
            else if (presenter.slot.concreteValueType == ConcreteSlotValueType.Vector1)
                onGUIHandler = Vector1OnGUIHandler;
            else
                return;

            Add(new IMGUIContainer(onGUIHandler) { executionContext = presenter.GetInstanceID() });
        }

        void Vector4OnGUIHandler()
        {
            if (presenter.slot == null)
                return;
            var previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            presenter.value = EditorGUILayout.Vector4Field(presenter.slot.displayName, presenter.value);
            EditorGUIUtility.wideMode = previousWideMode;
        }

        void Vector3OnGUIHandler()
        {
            if (presenter.slot == null)
                return;
            var previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            presenter.value = EditorGUILayout.Vector3Field(presenter.slot.displayName, presenter.value);
            EditorGUIUtility.wideMode = previousWideMode;
        }

        void Vector2OnGUIHandler()
        {
            if (presenter.slot == null)
                return;
            var previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            presenter.value = EditorGUILayout.Vector2Field(presenter.slot.displayName, presenter.value);
            EditorGUIUtility.wideMode = previousWideMode;
        }

        void Vector1OnGUIHandler()
        {
            if (presenter.slot == null)
                return;
            var previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            presenter.value = new Vector4(EditorGUILayout.FloatField(presenter.slot.displayName, presenter.value.x), 0, 0, 0);
            EditorGUIUtility.wideMode = previousWideMode;
        }

        protected override Object[] toWatch
        {
            get { return new Object[] { presenter }; }
        }

        public IMGUISlotEditorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (value == m_Presenter)
                    return;
                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }
    }
}
