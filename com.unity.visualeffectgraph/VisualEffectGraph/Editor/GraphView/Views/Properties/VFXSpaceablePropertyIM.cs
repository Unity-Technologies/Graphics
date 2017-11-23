using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    abstract class VFXSpacedPropertyIM : VFXPropertyIM
    {
        protected override object DoOnGUI(VFXDataAnchorPresenter presenter)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, presenter.name);

            Rect spaceRect = GUILayoutUtility.GetRect(24, 0, GUILayout.ExpandHeight(true));
            SpaceButton(spaceRect, (ISpaceable)presenter.value);

            object result = DoOnParameterGUI(presenter);
            GUILayout.EndHorizontal();
            return result;
        }

        protected override object DoOnGUI(Rect rect, string label, object value)
        {
            Label(rect, label);

            Rect spaceRect = rect;
            spaceRect.xMin += m_LabelWidth - 24;
            spaceRect.width = 24;
            SpaceButton(spaceRect, (ISpaceable)value);

            object result = DoOnParameterGUI(rect, value, label);
            return result;
        }

        void SpaceButton(Rect rect, ISpaceable value)
        {
            if (GUI.Button(rect, new GUIContent(Resources.Load<Texture2D>(string.Format("vfx/space{0}", value.space.ToString()))), GUIStyle.none))
            {
                value.space = (CoordinateSpace)((int)(value.space + 1) % CoordinateSpaceInfo.SpaceCount);
                GUI.changed = true;
            }
        }

        protected abstract object DoOnParameterGUI(VFXDataAnchorPresenter presenter);
        protected abstract object DoOnParameterGUI(Rect rect, object value, string label);
    }
    abstract class VFXSpacedPropertyIM<T> : VFXSpacedPropertyIM
    {
        protected override object DoOnParameterGUI(VFXDataAnchorPresenter presenter)
        {
            return OnParameterGUI(presenter, (T)presenter.value, presenter.name);
        }

        protected override object DoOnParameterGUI(Rect rect, object value, string label)
        {
            return OnParameterGUI(rect, (T)value, label);
        }

        public abstract T OnParameterGUI(VFXDataAnchorPresenter presenter, T value, string label);
        public abstract T OnParameterGUI(Rect rect, T value, string label);
    }


    abstract class VFXVector3SpacedPropertyIM<T> : VFXSpacedPropertyIM<T>
    {
        public override T OnParameterGUI(VFXDataAnchorPresenter presenter, T value, string label)
        {
            throw new System.NotImplementedException();
        }

        public override T OnParameterGUI(Rect rect, T value, string label)
        {
            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 3);
            float labelWidth = 20;

            Vector3 val = GetV(value);

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            val.x = EditorGUI.FloatField(rect, val.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            val.y = EditorGUI.FloatField(rect, val.y);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "z");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            val.y = EditorGUI.FloatField(rect, val.z);

            SetV(ref value, val);

            return value;
        }

        protected abstract Vector3 GetV(T value);
        protected abstract void SetV(ref T value, Vector3 vec);
    }

    class VFXPositionPropertyIM : VFXVector3SpacedPropertyIM<Position>
    {
        protected override Vector3 GetV(Position value)
        {
            return value.position;
        }

        protected override void SetV(ref Position value, Vector3 vec)
        {
            value.position = vec;
        }
    }
    class VFXDirectionPropertyIM : VFXVector3SpacedPropertyIM<DirectionType>
    {
        protected override Vector3 GetV(DirectionType value)
        {
            return value.direction;
        }

        protected override void SetV(ref DirectionType value, Vector3 vec)
        {
            value.direction = vec;
        }
    }
    class VFXVectorPropertyIM : VFXVector3SpacedPropertyIM<Vector>
    {
        protected override Vector3 GetV(Vector value)
        {
            return value.vector;
        }

        protected override void SetV(ref Vector value, Vector3 vec)
        {
            value.vector = vec;
        }
    }
}
