using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection; // for gradient field
using UnityEditor.Experimental;

namespace UnityEngine.Experimental.VFX
{
    // Primitive types
    public partial class VFXFloatType : VFXPrimitiveType<float>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set(EditorGUI.FloatField(area, "", slot.Get<float>(true)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set(EditorGUILayout.FloatField(slot.Name, slot.Get<float>(false)),false);
        }
    }

    public partial class VFXIntType : VFXPrimitiveType<int>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set(EditorGUI.IntField(area, "", slot.Get<int>(true)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set(EditorGUILayout.IntField(slot.Name, slot.Get<int>(false)),false);
        }
    }

    public partial class VFXUintType : VFXPrimitiveType<uint>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set<uint>((uint)EditorGUI.IntField(area, "", (int)slot.Get<uint>(true)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set<uint>((uint)EditorGUILayout.IntField(slot.Name, (int)slot.Get<uint>(false)),false);
        }
    }

    public partial class VFXTexture2DType : VFXPrimitiveType<Texture2D>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set((Texture2D)EditorGUI.ObjectField(area, slot.Get<Texture2D>(true), typeof(Texture2D)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set((Texture2D)EditorGUILayout.ObjectField(slot.Name, slot.Get<Texture2D>(false), typeof(Texture2D)),false);
        }
    }

    public partial class VFXTexture3DType : VFXPrimitiveType<Texture3D>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set((Texture3D)EditorGUI.ObjectField(area, slot.Get<Texture3D>(true), typeof(Texture3D)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set((Texture3D)EditorGUILayout.ObjectField(slot.Name, slot.Get<Texture3D>(false), typeof(Texture3D)),false);
        }
    }

    public partial class VFXCurveType : VFXPrimitiveType<AnimationCurve>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            EditorGUI.BeginChangeCheck();

            // Set color of selected curve (to investigate the focus bug)
            //int id = GUIUtility.GetControlID("s_CurveHash".GetHashCode(), FocusType.Keyboard, area);
            //Color col = Color.green;
            //if (id + 1 == GUIUtility.keyboardControl)
            //    col = Color.red;
            
            var curve = EditorGUI.CurveField(area, slot.Get<AnimationCurve>(true), col, new Rect());

            if (EditorGUI.EndChangeCheck())
            {
                ConstrainCurve(curve);
                Slot(slot, true).NotifyChange(VFXPropertySlot.Event.kValueUpdated); // We need to call this explicitly as the curve reference has not changed
            }
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            EditorGUI.BeginChangeCheck();

            // Set color of selected curve (to investigate the focus bug)
            //int id = GUIUtility.GetControlID("s_CurveHash".GetHashCode(), FocusType.Keyboard, EditorGUILayout.GetControlRect (true, 16, EditorStyles.colorField, null));
            //Color col = Color.green;
            //if (id + 1 == GUIUtility.keyboardControl)
            //    col = Color.red;

            var curve = EditorGUILayout.CurveField(slot.Name, slot.Get<AnimationCurve>(false),col, new Rect(),null);

            if (EditorGUI.EndChangeCheck())
            {
                ConstrainCurve(curve);
                Slot(slot, false).NotifyChange(VFXPropertySlot.Event.kValueUpdated); // We need to call this explicitly as the curve reference has not changed
            }
        }

        private static void ConstrainCurve(AnimationCurve curve)
        {
            // pingpong wrap mode is not supported yet so fallback to loop and notify the user
            bool wrapModeChanged = false;
            if (curve.preWrapMode == WrapMode.PingPong)
            {
                wrapModeChanged = true;
                curve.preWrapMode = WrapMode.Loop;
            }
            if (curve.postWrapMode == WrapMode.PingPong)
            {
                wrapModeChanged = true;
                curve.postWrapMode = WrapMode.Loop;
            }
            if (wrapModeChanged)
                Debug.LogWarning("Ping Pong wrap mode is not supported - Fallback to loop");
        }
    }

    public partial class VFXColorGradientType : VFXPrimitiveType<Gradient>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            InitGradientFieldMethods();
            if (s_EditorGUIGradientField == null)
                return;

            EditorGUI.BeginChangeCheck();
            s_EditorGUIGradientField.Invoke(null, new object[] { area, slot.Get<Gradient>(true) });
            if (EditorGUI.EndChangeCheck())
                Slot(slot, true).NotifyChange(VFXPropertySlot.Event.kValueUpdated); // We need to call this explicitly as the gradient reference has not changed
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            InitGradientFieldMethods();
            if (s_EditorGUILayoutGradientField == null)
                return;

            EditorGUI.BeginChangeCheck();
            s_EditorGUILayoutGradientField.Invoke(null, new object[] { slot.Name, slot.Get<Gradient>(false), null });
            if (EditorGUI.EndChangeCheck())
                Slot(slot, false).NotifyChange(VFXPropertySlot.Event.kValueUpdated); // We need to call this explicitly as the gradient reference has not changed
        }

        // Use reflection to access gradient field as it is internal and we dont have a scriptable object to call property field...
        protected static void InitGradientFieldMethods()
        {
            if (!s_GradientFieldMethodInitialized)
            {
                s_GradientFieldMethodInitialized = true;

                s_EditorGUIGradientField = typeof(EditorGUI).GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Rect), typeof(Gradient) }, null);
                if (s_EditorGUIGradientField == null)
                    Debug.LogError("Cannot get EditorGUI.GradientField method by reflection");

                s_EditorGUILayoutGradientField = typeof(EditorGUILayout).GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), typeof(Gradient), typeof(GUILayoutOption[]) }, null);
                if (s_EditorGUILayoutGradientField == null)
                    Debug.LogError("Cannot get EditorGUILayout.GradientField method by reflection");
            }
        }

        private static bool s_GradientFieldMethodInitialized = false;
        protected static MethodInfo s_EditorGUILayoutGradientField;
        protected static MethodInfo s_EditorGUIGradientField;
    }

    // Proxy types
    public partial class VFXFloat2Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set(EditorGUI.Vector2Field(area, "", Get<Vector2>(slot, true)),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set(EditorGUILayout.Vector2Field(slot.Name, slot.Get<Vector2>(false)), false);
        }
    }

    public partial class VFXFloat3Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set(EditorGUI.Vector3Field(area, "", Get<Vector3>(slot, true)), true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set(EditorGUILayout.Vector3Field(slot.Name, slot.Get<Vector3>(false)), false);
        }
    }

    public partial class VFXFloat4Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot.Set(EditorGUI.Vector4Field(area, "", Get<Vector4>(slot, true)), true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.Set(EditorGUILayout.Vector4Field(slot.Name, slot.Get<Vector4>(false)), false);
        }
    }

    public partial class VFXColorRGBType : VFXFloat3Type
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector3 v = slot.Get<Vector3>(true);
            Color c = new Color(v.x, v.y, v.z);
            c = EditorGUI.ColorField(area, GUIContent.none, c, false, false, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));
            v.Set(c.r, c.g, c.b);
            slot.Set(v, true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector3 v = slot.Get<Vector3>(false);
            Color c = new Color(v.x, v.y, v.z);
            c = EditorGUILayout.ColorField(new GUIContent(slot.Name), c, false, false, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));
            v.Set(c.r, c.g, c.b);
            slot.Set(v, false);
        }
    }

    public partial class VFXPositionType : VFXFloat3Type
    {
        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUIPositionWidget(slot);
        }
    }

    public partial class VFXVectorType : VFXFloat3Type
    {
        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector3 dir = EditorGUILayout.Vector3Field(slot.Name, slot.Get<Vector3>(false));
            float length = dir.magnitude;
            float newLength = EditorGUILayout.DelayedFloatField("    Magnitude", length);
            if (length != newLength)
                dir = newLength == 0.0f || length == 0.0f ? Vector3.zero : dir * (newLength / length);
            slot.Set(dir, false);
        }

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUIVectorWidget(slot, false);
        }
    }

    public partial class VFXDirectionType : VFXFloat3Type
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector3 n = EditorGUI.Vector3Field(area, "", Get<Vector3>(slot, true));
            slot.Set(n.normalized,true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector3 n = EditorGUILayout.Vector3Field(slot.Name, slot.Get<Vector3>(false));
            slot.Set(n.normalized,false);
        }

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUIVectorWidget(slot, true);
        }
    }

    public partial class VFXTransformType : VFXPropertyTypeSemantics
    {
        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            EditorGUILayout.LabelField(new GUIContent(slot.Name));
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                slot.GetChild(i).Semantics.OnInspectorGUI(slot.GetChild(i));
        }

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUITransformWidget(slot, false);
        }
    }

    // Composite types
    public partial class VFXSphereType : VFXPropertyTypeSemantics
    {
        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            EditorGUILayout.LabelField(new GUIContent(slot.Name));
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                slot.GetChild(i).Semantics.OnInspectorGUI(slot.GetChild(i));
        }

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUISphereWidget(slot);
        }
    }

    public partial class VFXAABoxType : VFXPropertyTypeSemantics
    {
        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            EditorGUILayout.LabelField(new GUIContent(slot.Name));
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                slot.GetChild(i).Semantics.OnInspectorGUI(slot.GetChild(i));
        }

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUIBoxWidget(slot);
        }
    }

    public partial class VFXOrientedBoxType : VFXTransformType
    {
        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot)
        {
            return new VFXUITransformWidget(slot, true);
        }
    }

    public partial class VFXPlaneType : VFXPropertyTypeSemantics
    {
        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            EditorGUILayout.LabelField(new GUIContent(slot.Name));
            for (int i = 0; i < slot.GetNbChildren(); ++i)
                slot.GetChild(i).Semantics.OnInspectorGUI(slot.GetChild(i));
        }
    }

}