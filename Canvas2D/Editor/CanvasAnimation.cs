using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Experimental
{
    public class CanvasAnimation
    {
        CanvasElement m_ElementBeingAnimated;
        Stack<CanvasAnimationCallback> m_Callbacks = new Stack<CanvasAnimationCallback>();
        Stack<PropertyData> m_UserData = new Stack<PropertyData>();

        CanvasAnimationCallback m_CurrentCallback;
        PropertyData m_CurrentData;

        public CanvasAnimation(CanvasElement e)
        {
            m_ElementBeingAnimated = e;
        }

        public void Tick()
        {
            if (m_CurrentCallback == null)
                return;

            m_CurrentCallback(m_ElementBeingAnimated, this, m_CurrentData);
            m_ElementBeingAnimated.Invalidate();
        }

        public CanvasAnimation Lerp(string prop, object from, object to)
        {
            FieldInfo fi = GetFieldBeingAnimated(prop);
            if (fi.FieldType != from.GetType())
            {
                Debug.LogError("Cannot set a " + from.GetType() + " to " + prop + " because it is a " + fi.FieldType);
                return this;
            }

            var propData = new PropertyData(fi, from, to);

            switch (fi.FieldType.Name)
            {
                case "Single":
                    AddCallback(LerpFloat, propData);
                    break;
                case "Vector3":
                    AddCallback(LerpVector3, propData);
                    break;
                default:
                    Debug.LogError("No handler found to lerp " + fi.FieldType.Name);
                    break;
            }

            return this;
        }

        private void LerpFloat(CanvasElement element, CanvasAnimation owner, object userData)
        {
            var pData = userData as PropertyData;
            float result = Mathf.Lerp((float)pData.data0, (float)pData.data1, pData.curve.Evaluate(pData.time));
            pData.field.SetValue(m_ElementBeingAnimated, result);
            pData.time += 0.08f;
            if (pData.time > 1.0f)
            {
                pData.field.SetValue(m_ElementBeingAnimated, (float)pData.data1);
                owner.Done();
            }
        }

        private void LerpVector3(CanvasElement element, CanvasAnimation owner, object userData)
        {
            var pData = userData as PropertyData;
            Vector3 result = Vector3.Lerp((Vector3)pData.data0, (Vector3)pData.data1, pData.curve.Evaluate(pData.time));
            pData.field.SetValue(m_ElementBeingAnimated, result);
            pData.time += 0.08f;
            if (pData.time > 1.0f)
            {
                pData.field.SetValue(m_ElementBeingAnimated, (Vector3)pData.data1);
                owner.Done();
            }
        }

        public CanvasAnimation Then(CanvasAnimationCallback callback)
        {
            AddCallback(callback, null);
            return this;
        }

        public void Done()
        {
            if (m_Callbacks.Count == 0)
            {
                m_ElementBeingAnimated.ParentCanvas().EndAnimation(this);
                return;
            }

            m_CurrentData = m_UserData.Pop();
            m_CurrentCallback = m_Callbacks.Pop();
        }

        private void AddCallback(CanvasAnimationCallback callback, PropertyData userdata)
        {
            m_UserData.Push(userdata);
            m_Callbacks.Push(callback);

            if (m_Callbacks.Count == 1 && m_CurrentCallback == null)
                Done();
        }

        private FieldInfo GetFieldBeingAnimated(string fieldName)
        {
            foreach (FieldInfo fieldInfo in m_ElementBeingAnimated.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (fieldInfo.Name == fieldName)
                    return fieldInfo;
            }

            return null;
        }
    }
}
