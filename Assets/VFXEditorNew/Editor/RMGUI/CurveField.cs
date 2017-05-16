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
            m_Curve = new VisualElement() { minWidth = 4, minHeight = 4 };
            m_Curve.AddToClassList("curve");

            m_Curve.AddManipulator(new Clickable(OnCurveClick));
        }

        void OnCurveClick()
        {
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

            if (onValueChanged != null)
                onValueChanged();
        }

        public CurveField(string label) : base(label)
        {
            CreateCurve();

            flexDirection = FlexDirection.Row;
            AddChild(m_Curve);
        }

        public CurveField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateCurve();
            AddChild(m_Curve);
        }

        protected internal override void OnPostLayout(bool hasNewLayout)
        {
            ValueToGUI();
        }

        protected override void ValueToGUI()
        {
            int previewWidth = (int)m_Curve.position.width;
            int previewHeight = (int)m_Curve.position.height;

            if (previewHeight > 0 && previewWidth > 0)
            {
                Rect range = new Rect(0, 0, 1, 1);
                m_Curve.backgroundImage = AnimationCurvePreviewCache.GetPreview(previewWidth,
                        previewHeight,
                        m_Value,
                        Color.green,
                        Color.clear,
                        Color.clear,
                        range);
            }
        }
    }
}
