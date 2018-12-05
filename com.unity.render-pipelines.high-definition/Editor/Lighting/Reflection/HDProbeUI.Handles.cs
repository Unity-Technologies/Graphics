using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static partial class HDProbeUI
    {
        static List<HDProbe> s_DrawHandles_Target = new List<HDProbe>();
        internal static void DrawHandles(SerializedHDProbe serialized, Editor owner)
        {
            var probe = serialized.target;

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawHandles_EditBase(serialized.probeSettings.influence, owner, probe.transform);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluence(serialized.probeSettings.influence, owner, probe.transform);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluenceNormal(serialized.probeSettings.influence, owner, probe.transform);
                    break;
                case EditCapturePosition:
                case EditMirrorPosition:
                    {
                        var proxyToWorldMatrix = probe.proxyToWorld;

                        SerializedProperty target;
                        switch (EditMode.editMode)
                        {
                            case EditCapturePosition: target = serialized.probeSettings.proxyCapturePositionProxySpace; break;
                            case EditMirrorPosition: target = serialized.probeSettings.proxyMirrorPositionProxySpace; break;
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

                        var target = serialized.probeSettings.proxyMirrorRotationProxySpace;
                        var position = serialized.probeSettings.proxyMirrorPositionProxySpace.vector3Value;

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
