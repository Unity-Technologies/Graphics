using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class VFXSpacedPropertyIM : VFXPropertyIM
    {
        protected override object DoOnGUI(VFXDataAnchorPresenter presenter)
        {
            GUILayout.BeginHorizontal();
            Label(presenter, presenter.name);

            Rect spaceRect = GUILayoutUtility.GetRect(24, 0, GUILayout.ExpandHeight(true));
            SpaceButton(spaceRect, (Spaceable)presenter.value);

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
            SpaceButton(spaceRect, (Spaceable)value);

            object result = DoOnParameterGUI(rect, value, label);
            return result;
        }

        void SpaceButton(Rect rect, Spaceable value)
        {
            if (GUI.Button(rect, new GUIContent(Resources.Load<Texture2D>(string.Format("vfx/space{0}", value.space.ToString()))), GUIStyle.none))
            {
                value.space = (CoordinateSpace)((int)(value.space + 1) % (int)CoordinateSpace.SpaceCount);
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


    class VFXPositionPropertyIM : VFXSpacedPropertyIM<Position>
    {
        public override Position OnParameterGUI(VFXDataAnchorPresenter presenter, Position value, string label)
        {
            throw new System.NotImplementedException();
        }

        public override Position OnParameterGUI(Rect rect, Position value, string label)
        {
            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 3);
            float labelWidth = 20;

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.position.x = EditorGUI.FloatField(rect, value.position.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.position.y = EditorGUI.FloatField(rect, value.position.y);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "z");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.position.y = EditorGUI.FloatField(rect, value.position.z);

            return value;
        }
    }
    class VFXDirectionPropertyIM : VFXSpacedPropertyIM<DirectionType>
    {
        public override DirectionType OnParameterGUI(VFXDataAnchorPresenter presenter, DirectionType value, string label)
        {
            throw new System.NotImplementedException();
        }

        public override DirectionType OnParameterGUI(Rect rect, DirectionType value, string label)
        {
            rect.xMin += m_LabelWidth;

            float paramWidth = Mathf.Floor(rect.width / 3);
            float labelWidth = 20;

            rect.width = labelWidth;
            GUI.Label(rect, "x");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.direction.x = EditorGUI.FloatField(rect, value.direction.x);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "y");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.direction.y = EditorGUI.FloatField(rect, value.direction.y);

            rect.xMin += rect.width;
            rect.width = labelWidth;
            GUI.Label(rect, "z");
            rect.xMin += rect.width;
            rect.width = paramWidth - labelWidth;
            value.direction.y = EditorGUI.FloatField(rect, value.direction.z);

            return value;
        }
    }
}
