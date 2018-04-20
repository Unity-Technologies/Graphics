using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXOrientedBoxGizmo : VFXSpaceableGizmo<OrientedBox>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_SizeProperty;
        IProperty<Vector3> m_AnglesProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_SizeProperty = context.RegisterProperty<Vector3>("size");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
        }
        public override void OnDrawSpacedGizmo(OrientedBox box, VisualEffect component)
        {
            Matrix4x4 rotate = Matrix4x4.Rotate(Quaternion.Euler(box.angles));
            Matrix4x4 fullTranform = Matrix4x4.Translate(box.center) * rotate * Matrix4x4.Translate(-box.center);
            //using (new Handles.DrawingScope(Handles.matrix * fullTranform))
            {
                VFXAABoxGizmo.DrawBoxSizeDataAnchorGizmo(new AABox(){center = box.center,size = box.size},component,this,m_CenterProperty,m_SizeProperty, fullTranform);
            }

            if(m_AnglesProperty.isEditable && RotationGizmo(component, box.center,ref box.angles)) 
            {
                m_AnglesProperty.SetValue(box.angles);
            }

            if (m_CenterProperty.isEditable && PositionGizmo(component, ref box.center))
            {
                m_CenterProperty.SetValue(box.center);
            }
        }

    }

    class VFXAABoxGizmo : VFXSpaceableGizmo<AABox>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<Vector3> m_SizeProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_SizeProperty = context.RegisterProperty<Vector3>("size");
        }
        public override void OnDrawSpacedGizmo(AABox box, VisualEffect component)
        {
            DrawBoxSizeDataAnchorGizmo(box,component,this,m_CenterProperty,m_SizeProperty, Matrix4x4.identity);

            if (m_CenterProperty.isEditable && PositionGizmo(component, ref box.center))
            {
                m_CenterProperty.SetValue(box.center);
            }
        }

        public static bool DrawBoxSizeDataAnchorGizmo(AABox box,VisualEffect component, VFXGizmo gizmo,IProperty<Vector3> centerProperty, IProperty<Vector3> sizeProperty, Matrix4x4 centerMatrix)
        {
            Vector3[] points = new Vector3[8];

            Vector3 center = box.center;
            Vector3 size = box.size;


            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);


            for(int i = 0 ; i < points.Length ; ++i)
            {
                points[i] = centerMatrix.MultiplyPoint(points[i]);
            }

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            if( sizeProperty.isEditable)
            {

                Handles.color = Color.blue;
                {
                    EditorGUI.BeginChangeCheck();
                    // axis +Z
                    Vector3 middle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                    Vector3 othermiddle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.z = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                        {
                           center = (middleResult + othermiddle) * 0.5f;
                        }
                            
                        changed = true;
                    }
                }
                {
                    EditorGUI.BeginChangeCheck();
                    // axis -Z
                    Vector3 middle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                    Vector3 othermiddle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.z = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                            center = (middleResult + othermiddle) * 0.5f;
                        changed = true;
                    }
                }


                Handles.color = Color.red;
                {
                    EditorGUI.BeginChangeCheck();
                    // axis +X
                    Vector3 middle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                    Vector3 othermiddle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.x = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                            center = (middleResult + othermiddle) * 0.5f;
                        changed = true;
                    }
                }
                
                {
                    EditorGUI.BeginChangeCheck();
                    // axis -X
                    Vector3 middle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                    Vector3 othermiddle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.x = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                            center = (middleResult + othermiddle) * 0.5f;
                        changed = true;
                    }
                }

                Handles.color = Color.green;
                {
                    EditorGUI.BeginChangeCheck();
                    // axis +Y
                    Vector3 middle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                    Vector3 othermiddle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.y = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                            center = (middleResult + othermiddle) * 0.5f;
                        changed = true;
                    }
                }
                
                {
                EditorGUI.BeginChangeCheck();
                    // axis -Y
                    Vector3 middle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                    Vector3 othermiddle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                    Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        size.y = (middleResult - othermiddle).magnitude;
                        if( centerProperty.isEditable)
                            center = (middleResult + othermiddle) * 0.5f;
                        changed = true;
                    }
                } 
            }
            if( changed)
            {
                if( centerProperty.isEditable)
                    centerProperty.SetValue(center);
                sizeProperty.SetValue(size);
            }

            return changed;
        }
    }
}
