using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        static List<HDProbe> s_DrawHandles_Target = new List<HDProbe>();
        internal static void DrawHandles(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var probe = d.target;

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawHandles_EditBase(s.probeSettings.influence, d.probeSettings.influence, o, probe.transform);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluence(s.probeSettings.influence, d.probeSettings.influence, o, probe.transform);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluenceNormal(s.probeSettings.influence, d.probeSettings.influence, o, probe.transform);
                    break;
                case EditCapturePosition:
                case EditMirrorPosition:
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;

                        SerializedProperty target;
                        switch (EditMode.editMode)
                        {
                            case EditCapturePosition: target = d.probeSettings.proxyCapturePositionProxySpace; break;
                            case EditMirrorPosition: target = d.probeSettings.proxyMirrorPositionProxySpace; break;
                            default: throw new ArgumentOutOfRangeException();
                        }

                        var position = proxyToWorldMatrix.MultiplyPoint(target.vector3Value);
                        EditorGUI.BeginChangeCheck();
                        position = Handles.PositionHandle(position, proxyToWorldMatrix.rotation);
                        if (EditorGUI.EndChangeCheck())
                            target.vector3Value = proxyToWorldMatrix.inverse.MultiplyPoint(position);
                        break;
                    }
                case EditMirrorRotation:
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;

                        var target = d.probeSettings.proxyMirrorRotationProxySpace;
                        var position = d.probeSettings.proxyMirrorPositionProxySpace.vector3Value;

                        using (new Handles.DrawingScope(proxyToWorldMatrix))
                        {
                            var rotation = target.quaternionValue;
                            EditorGUI.BeginChangeCheck();
                            rotation = Handles.RotationHandle(rotation, position);
                            if (EditorGUI.EndChangeCheck())
                                target.quaternionValue = rotation;
                        }
                        break;
                    }
            }
        }
    }
}
