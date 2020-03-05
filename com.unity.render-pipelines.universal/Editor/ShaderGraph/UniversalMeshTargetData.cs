using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal class UniversalMeshTargetData : TargetImplementationData
    {
        public enum MaterialType
        {
            Lit,
            Unlit,
            SpriteLit,
            SpriteUnlit,
        }

        public enum WorkflowMode
        {
            Specular,
            Metallic,
        }

#region Fields
        [SerializeField]
        MaterialType m_MaterialType = MaterialType.Lit;

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        SurfaceType m_SurfaceType;

        [SerializeField]
        AlphaMode m_AlphaMode;

        [SerializeField]
        bool m_TwoSided;

        [SerializeField]
        bool m_AlphaClip;

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        [SerializeField]
        bool m_DOTSInstancing = false;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
#endregion

#region Properties
        public MaterialType materialType => m_MaterialType;
        public WorkflowMode workflowMode => m_WorkflowMode;
        public SurfaceType surfaceType => m_SurfaceType;
        public AlphaMode alphaMode => m_AlphaMode;
        public bool twoSided => m_TwoSided;
        public bool alphaClip => m_AlphaClip;
        public bool addPrecomputedVelocity => m_AddPrecomputedVelocity;
        public bool dotsInstancing => m_DOTSInstancing;
        public NormalDropOffSpace normalDropOffSpace => m_NormalDropOffSpace;
#endregion

#region GUI
        internal override void GetProperties(PropertySheet propertySheet, InspectorView inspectorView)
        {
            propertySheet.Add(new PropertyRow(new Label("Material")), (row) =>
                {
                    row.Add(new EnumField(MaterialType.Lit), (field) =>
                    {
                        field.value = materialType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(materialType, evt.newValue))
                                return;

                            m_MaterialType = (MaterialType)evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            if(materialType == MaterialType.Lit)
            {
                propertySheet.Add(new PropertyRow(new Label("Workflow")), (row) =>
                    {
                        row.Add(new EnumField(WorkflowMode.Metallic), (field) =>
                        {
                            field.value = workflowMode;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(workflowMode, evt.newValue))
                                    return;

                                m_WorkflowMode = (WorkflowMode)evt.newValue;
                                inspectorView.OnChange();
                            });
                        });
                    });
            }

            propertySheet.Add(new PropertyRow(new Label("Surface")), (row) =>
                {
                    row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                    {
                        field.value = surfaceType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(surfaceType, evt.newValue))
                                return;

                            m_SurfaceType = (SurfaceType)evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Blend")), (row) =>
                {
                    row.Add(new EnumField(AlphaMode.Additive), (field) =>
                    {
                        field.value = alphaMode;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(alphaMode, evt.newValue))
                                return;

                            m_AlphaMode = (AlphaMode)evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Alpha Clip")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = alphaClip;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(alphaClip, evt.newValue))
                                return;
                            
                            m_AlphaClip = evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Two Sided")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = twoSided;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(twoSided, evt.newValue))
                                return;
                            
                            m_TwoSided = evt.newValue;
                            inspectorView.OnChange();
                        });
                    });
                });

            if(materialType == MaterialType.Lit)
            {
                propertySheet.Add(new PropertyRow(new Label("Fragment Normal Space")), (row) =>
                    {
                        row.Add(new EnumField(NormalDropOffSpace.Tangent), (field) =>
                        {
                            field.value = normalDropOffSpace;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(normalDropOffSpace, evt.newValue))
                                    return;

                                inspectorView.OnChange();
                                m_NormalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                            });
                        });
                    });
            }
        }
#endregion
    }
}
