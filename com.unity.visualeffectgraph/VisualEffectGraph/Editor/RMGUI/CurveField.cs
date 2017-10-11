using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditorInternal;


namespace UnityEditor.VFX.UIElements
{
    class CurveField : ValueControl<AnimationCurve>
    {
        static readonly Color kCurveColor = Color.green;

        void SetupCurve()
        {
            this.AddManipulator(new Clickable(OnCurveClick));
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void OnDetach(DetachFromPanelEvent e)
        {
            if (style.backgroundImage.value != null)
            {
                Object.DestroyImmediate(style.backgroundImage.value);
                style.backgroundImage = null;
            }
        }

        void OnCurveClick()
        {
            if (!enabledInHierarchy)
                return;

            CurveEditorSettings settings = new CurveEditorSettings();
            if (m_Value == null)
                m_Value = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });
            CurveEditorWindow.curve = m_Value;

            CurveEditorWindow.color = kCurveColor;
            CurveEditorWindow.instance.Show(OnCurveChanged, settings);
        }

        void OnCurveChanged(AnimationCurve curve)
        {
            m_Value = new AnimationCurve();
            m_Value.keys = curve.keys;
            m_Value.preWrapMode = curve.preWrapMode;
            m_Value.postWrapMode = curve.postWrapMode;
            ValueToGUI();

            if (OnValueChanged != null)
                OnValueChanged();
        }

        public CurveField(string label) : base(label)
        {
            SetupCurve();

            style.flexDirection = FlexDirection.Row;
        }

        public CurveField(VisualElement existingLabel) : base(existingLabel)
        {
            SetupCurve();
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();
            ValueToGUI();
        }

        public bool m_Dirty;

        protected override void ValueToGUI()
        {
            m_Dirty = true;
        }

        public override void DoRepaint()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                int previewWidth = (int)layout.width;
                int previewHeight = (int)layout.height;

                if (previewHeight > 0 && previewWidth > 0)
                {
                    Rect range = new Rect(0, 0, 1, 1);
                    // Instantiate because AnimationCurvePreviewCache returns a temporary;
                    style.backgroundImage = AnimationCurvePreviewCache.GenerateCurvePreview(
                            previewWidth,
                            previewHeight,
                            range,
                            m_Value,
                            kCurveColor,
                            style.backgroundImage.value);
                }
            }

            base.DoRepaint();
        }
    }
}
