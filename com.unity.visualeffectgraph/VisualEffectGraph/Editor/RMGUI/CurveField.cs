using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditorInternal;


namespace UnityEditor.VFX.UIElements
{
    class CurveField : ValueControl<AnimationCurve>
    {
        VisualElement m_Curve;


        void CreateCurve()
        {
            m_Curve = new VisualElement();
            m_Curve.style.minWidth = 4; m_Curve.style.minHeight = 4;
            m_Curve.AddToClassList("curve");

            m_Curve.AddManipulator(new Clickable(OnCurveClick));
        }

        void OnCurveClick()
        {
            if (!enabledInHierarchy)
                return;
            /*
            CurveEditorSettings settings = new CurveEditorSettings();
            if (ranges.width > 0 && ranges.height > 0 && ranges.width != Mathf.Infinity && ranges.height != Mathf.Infinity)
            {
                settings.hRangeMin = ranges.xMin;
                settings.hRangeMax = ranges.xMax;
                settings.vRangeMin = ranges.yMin;
                settings.vRangeMax = ranges.yMax;
            }*/

            CurveEditorSettings settings = new CurveEditorSettings();
            if (m_Value == null)
                m_Value = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });
            CurveEditorWindow.curve = m_Value;

            CurveEditorWindow.color = Color.green;
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
            CreateCurve();

            style.flexDirection = FlexDirection.Row;
            Add(m_Curve);
        }

        public CurveField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateCurve();
            Add(m_Curve);
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
                int previewWidth = (int)m_Curve.layout.width;
                int previewHeight = (int)m_Curve.layout.height;

                if (previewHeight > 0 && previewWidth > 0)
                {
                    Rect range = new Rect(0, 0, 1, 1);
                    // Instantiate because AnimationCurvePreviewCache returns a temporary;
                    m_Curve.style.backgroundImage = AnimationCurvePreviewCache.GenerateCurvePreview(
                            previewWidth,
                            previewHeight,
                            range,
                            m_Value,
                            Color.green,
                            m_Curve.style.backgroundImage.value);
                }
            }

            base.DoRepaint();
        }
    }
}
