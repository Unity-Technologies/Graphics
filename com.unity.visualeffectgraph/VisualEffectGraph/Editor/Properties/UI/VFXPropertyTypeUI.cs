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
            slot = slot.CurrentValueRef;
            slot.SetValue(EditorGUI.FloatField(area, "", slot.GetValue<float>()));
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.SetValue(EditorGUILayout.FloatField(slot.Name, slot.GetValue<float>()));
        }
    }

    public partial class VFXIntType : VFXPrimitiveType<int>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot = slot.CurrentValueRef;
            slot.SetValue(EditorGUI.IntField(area, "", slot.GetValue<int>()));
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.SetValue(EditorGUILayout.IntField(slot.Name, slot.GetValue<int>()));
        }
    }

    public partial class VFXUintType : VFXPrimitiveType<uint>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot = slot.CurrentValueRef;
            slot.SetValue<uint>((uint)EditorGUI.IntField(area, "", (int)slot.GetValue<uint>()));
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.SetValue<uint>((uint)EditorGUILayout.IntField(slot.Name, (int)slot.GetValue<uint>()));
        }
    }

    public partial class VFXTexture2DType : VFXPrimitiveType<Texture2D>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot = slot.CurrentValueRef;
            slot.SetValue<Texture2D>((Texture2D)EditorGUI.ObjectField(area, slot.GetValue<Texture2D>(), typeof(Texture2D)));
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.SetValue<Texture2D>((Texture2D)EditorGUILayout.ObjectField(slot.Name, slot.GetValue<Texture2D>(), typeof(Texture2D)));
        }
    }

    public partial class VFXTexture3DType : VFXPrimitiveType<Texture3D>
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            slot = slot.CurrentValueRef;
            slot.SetValue<Texture3D>((Texture3D)EditorGUI.ObjectField(area, slot.GetValue<Texture3D>(), typeof(Texture3D)));
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            slot.SetValue<Texture3D>((Texture3D)EditorGUILayout.ObjectField(slot.Name, slot.GetValue<Texture3D>(), typeof(Texture3D)));
        }
    }

    // Proxy types
    public partial class VFXFloat2Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector2 v = Get<Vector2>(slot, true);
            Set(slot, EditorGUI.Vector2Field(area, "", v), true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector2 v = Get<Vector2>(slot, false);
            Set(slot, EditorGUILayout.Vector2Field(slot.Name, v), false);
        }
    }

    public partial class VFXFloat3Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector3 v = Get<Vector3>(slot, true);
            Set(slot,EditorGUI.Vector3Field(area, "", v),true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector3 v = Get<Vector3>(slot, false);
            Set(slot, EditorGUILayout.Vector3Field(slot.Name, v), false);
        }
    }

    public partial class VFXFloat4Type : VFXProxyVectorType
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector4 v = Get<Vector4>(slot, true);
            Set(slot, EditorGUI.Vector4Field(area, "", v), true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector4 v = Get<Vector4>(slot, false);
            Set(slot, EditorGUILayout.Vector4Field(slot.Name, v), false);
        }
    }

    public partial class VFXColorRGBType : VFXFloat3Type
    {
        public override void OnCanvas2DGUI(VFXPropertySlot slot, Rect area)
        {
            Vector3 v = Get<Vector3>(slot, true);

            Color c = new Color(v.x, v.y, v.z);
            c = EditorGUI.ColorField(area, GUIContent.none, c, false, false, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));

            v.Set(c.r, c.g, c.b);
            Set(slot, v, true);
        }

        public override void OnInspectorGUI(VFXPropertySlot slot)
        {
            Vector3 v = Get<Vector3>(slot, false);

            Color c = new Color(v.x, v.y, v.z);
            c = EditorGUILayout.ColorField(new GUIContent(slot.Name), c, false, false, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));

            v.Set(c.r, c.g, c.b);
            Set(slot, v, false);
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