using System;
using System.Linq;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.VFX;

[CustomEditor(typeof(VFXContext), true)]
[CanEditMultipleObjects]
class VFXContextEditor : VFXSlotContainerEditor
{
    SerializedProperty spaceProperty;
    SerializedObject dataObject;

    SerializedObject srpSubOutputObject;
    private bool subOutputObjectInitialized = false;

    float m_Width;

    protected void OnEnable()
    {
        UnityEngine.Object[] allData = targets.Cast<VFXContext>().Select(t => t.GetData()).Distinct().Where(t => t != null).Cast<UnityEngine.Object>().ToArray();
        if (allData.Length > 0)
        {
            dataObject = new SerializedObject(allData);
            spaceProperty = dataObject.FindProperty("m_Space");
        }
        else
        {
            dataObject = null;
            spaceProperty = null;
        }

        subOutputObjectInitialized = false;
    }

    protected override SerializedProperty FindProperty(VFXSetting setting)
    {
        if (setting.instance is VFXContext)
            return serializedObject.FindProperty(setting.name);
        if (setting.instance is VFXSRPSubOutput)
            return srpSubOutputObject.FindProperty(setting.name);
        if (setting.instance is VFXData)
            return dataObject.FindProperty(setting.name);
        throw new ArgumentException("VFXSetting is from an unexpected instance: " + setting.instance);
    }

    public static readonly GUIContent spaceLabel = EditorGUIUtility.TrTextContent("Space", "Specifies simulated space of the system.");

    enum ContextSpace
    {
        Local = VFXCoordinateSpace.Local,
        World = VFXCoordinateSpace.World
    }

    protected void DisplaySpace()
    {
        if (spaceProperty != null)
        {
            var contextSpace = (ContextSpace)spaceProperty.intValue;
            EditorGUI.BeginChangeCheck();
            var newSpace = (ContextSpace)EditorGUILayout.EnumPopup(spaceLabel, contextSpace);
            if (EditorGUI.EndChangeCheck())
                spaceProperty.intValue = (int)newSpace;
        }
    }

    public override SerializedProperty DoInspectorGUI()
    {
        if (!subOutputObjectInitialized)
        {
            UnityEngine.Object[] allSRPSubOutputs = targets.OfType<VFXAbstractRenderedOutput>().Select(t => t.subOutput).Where(t => t != null).ToArray();
            srpSubOutputObject = allSRPSubOutputs.Length > 0 ? new SerializedObject(allSRPSubOutputs) : null;
            subOutputObjectInitialized = true;
        }

        DisplaySpace();
        return base.DoInspectorGUI();
    }

    void DoAttributeLayoutGUI(string label, StructureOfArrayProvider.BucketInfo[] layout)
    {
        GUILayout.Label(label, Styles.header);

        // Used to distribute width evenly for each cell, induces a one-frame latency
        var w = GUILayoutUtility.GetLastRect().width;
        if (Event.current.type != EventType.Layout && w > 0)
            m_Width = w - 48;

        int maxSize = 0;

        foreach (StructureOfArrayProvider.BucketInfo bucket in layout)
            maxSize = Math.Max(maxSize, bucket.size);

        DrawAttributeLayoutGrid(layout, maxSize);
    }

    void DrawAttributeLayoutGrid(StructureOfArrayProvider.BucketInfo[] layout, int maxSize)
    {
        int i = 0;
        float height = 16.0f;

        Rect r = GUILayoutUtility.GetRect(m_Width, layout.Length * height);

        foreach (var bucket in layout)
        {
            float x = r.x;
            float y = r.y + i * height;
            float cellwidth = (m_Width - 16) / maxSize;

            Rect cellRect = new Rect(x, y, 16, height);
            GUI.Label(cellRect, i.ToString(), Styles.cell);

            int bucketSize = bucket.size;
            int usedSize = bucket.usedSize;

            x += 16;

            for (int j = 0; j < maxSize; j++)
            {
                cellRect = new Rect(x, y, cellwidth, height);
                if (j < usedSize)
                {
                    var attrib = bucket.attributes[j];
                    if (attrib.name != null)
                        Styles.DataTypeLabel(cellRect, attrib.name, attrib.type, Styles.cell);
                    else
                        Styles.DataTypeLabel(cellRect, "", VFXValueType.None, Styles.cell);
                }
                else
                {
                    if (j < bucketSize)
                        Styles.DataTypeLabel(cellRect, "", VFXValueType.None, Styles.cell);
                    else
                        GUI.Label(cellRect, "");
                }
                x += cellwidth;
            }
            i++;
        }
    }

    protected void DisplaySummary()
    {
        if (serializedObject.isEditingMultipleObjects) return; // Summary Only visible for single selection

        // Context / SystemData
        if (dataObject == null) return;
        var context = (VFXContext)target;
        var data = (VFXData)dataObject.targetObject;

        // Particle context data
        if (data.type == VFXDataType.Particle)
        {
            VFXDataParticle particleData = data as VFXDataParticle;
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Capacity", particleData.GetSettingValue("capacity").ToString());

                EditorGUILayout.Space();

                var attributes = data.GetAttributes();

                if (attributes.Count() > 0)
                {
                    EditorGUILayout.LabelField("System Attribute Summary", Styles.header);

                    foreach (var attr in attributes)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(attr.attrib.name, Styles.cell);
                            Styles.DataTypeLabel(attr.attrib.type.ToString(), attr.attrib.type, Styles.cell, GUILayout.Width(64));
                            int size = VFXExpressionHelper.GetSizeOfType(attr.attrib.type) * 4;
                            GUILayout.Label(size + " byte" + (size > 1 ? "s" : ""), Styles.cell, GUILayout.Width(64));
                            var mode = attr.mode;
                            GUILayout.Label(mode.ToString(), Styles.cell, GUILayout.Width(72));
                        }
                    }
                }

                StructureOfArrayProvider.BucketInfo[] current = particleData.GetCurrentAttributeLayout();
                StructureOfArrayProvider.BucketInfo[] source = particleData.GetSourceAttributeLayout();

                if (current.Length > 0)
                {
                    GUILayout.Space(24);
                    DoAttributeLayoutGUI("Current Attribute Layout", current);
                }

                if (source.Length > 0)
                {
                    GUILayout.Space(12);
                    DoAttributeLayoutGUI("Source Attribute Layout", source);
                }
            }
        }

        if (VFXViewPreference.displayExtraDebugInfo)
        {
            // Extra debug data
            EditorGUILayout.Space();
            {
                Styles.Row(Styles.header, "Name", "Value");
                Styles.Row(Styles.cell, "Context Type", context.contextType.ToString());
                Styles.Row(Styles.cell, "Task Type", context.taskType.ToString());
                Styles.Row(Styles.cell, "Input Data Type", context.inputType.ToString());
                Styles.Row(Styles.cell, "Context Data Type", data.type.ToString());
                Styles.Row(Styles.cell, "Can Be Compiled", context.CanBeCompiled().ToString());

                EditorGUILayout.Space();

                var attributeInfos = data.GetAttributesForContext(context);
                VFXAttributeInfo[] infos;

                // Early check for context consistency
                try
                {
                    infos = attributeInfos.ToArray();
                }
                catch
                {
                    return;
                }

                EditorGUILayout.LabelField("Attributes used by Context", Styles.header);

                foreach (var info in infos)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(info.attrib.name, Styles.cell);
                        Styles.DataTypeLabel(info.attrib.type.ToString(), info.attrib.type, Styles.cell, GUILayout.Width(80));
                        Styles.AttributeModeLabel(info.mode.ToString(), info.mode, Styles.cell, GUILayout.Width(80));
                    }
                }

                EditorGUILayout.Space();

                Styles.Row(Styles.header, "Blocks");
                foreach (var block in context.activeChildrenWithImplicit)
                    Styles.Row(Styles.cell, block.name, !context.children.Contains(block) ? "implicit" : "");

                EditorGUILayout.Space();
            }
        }
    }

    protected void DisplayName()
    {
        if (!serializedObject.isEditingMultipleObjects)
        {
            if (target is VFXContext context)
            {
                var label = string.IsNullOrEmpty(context.label) ? context.letter.ToString() : context.label;
                GUIStyle style = null;
                switch (context.contextType)
                {
                    case VFXContextType.Event:
                    case VFXContextType.SpawnerGPU:
                    case VFXContextType.Spawner:
                    case VFXContextType.Init:
                        style = Styles.spawnStyle;
                        break;
                    case VFXContextType.Update:
                    case VFXContextType.Output:
                        switch (context.inputType)
                        {
                            case VFXDataType.Particle:
                                style = Styles.particleStyle;
                                break;
                            case VFXDataType.ParticleStrip:
                                style = Styles.particleStripeStyle;
                                break;
                            case VFXDataType.Mesh:
                                style = Styles.meshStyle;
                                break;
                        }
                        break;
                }

                if (label != null && style != null)
                {
                    GUILayout.Label(label, style);
                }
            }
        }
    }

    public virtual void DisplayWarnings()
    {
    }

    protected void ApplyAndInvalidate()
    {
        bool invalidate = false;

        if (serializedObject != null && serializedObject.ApplyModifiedProperties())
            invalidate = true;

        if (dataObject != null && dataObject.ApplyModifiedProperties())
            invalidate = true;

        if (srpSubOutputObject != null && srpSubOutputObject.ApplyModifiedProperties())
            invalidate = true;

        if (invalidate)
        {
            foreach (VFXContext ctx in targets.OfType<VFXContext>())
            {
                // notify that something changed, this will also invalidate contexts.
                ctx.GetData().Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (dataObject != null)
            dataObject.Update();

        if (srpSubOutputObject != null)
            srpSubOutputObject.Update();

        DisplayName();

        base.OnInspectorGUI();

        ApplyAndInvalidate();

        DisplayWarnings();
        DisplaySummary();
    }
}
