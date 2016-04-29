using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
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
        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUIPositionWidget(slot, editor);
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

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUIVectorWidget(slot, editor, false);
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

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUIVectorWidget(slot, editor, true);
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

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUITransformWidget(slot, editor, false);
        }
    }

    public partial class VFXOrientedBoxType : VFXTransformType
    {
        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUITransformWidget(slot, editor, true);
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

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot,Editor editor)
        {
            return new VFXUISphereWidget(slot,editor);
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

        public override VFXUIWidget CreateUIWidget(VFXPropertySlot slot, Editor editor)
        {
            return new VFXUIBoxWidget(slot,editor);
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