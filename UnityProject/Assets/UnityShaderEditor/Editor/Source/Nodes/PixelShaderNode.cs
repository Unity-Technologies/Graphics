using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Output/Pixel Shader")]
     public class PixelShaderNode : BaseMaterialNode, IGeneratesBodyCode
    {

        [SerializeField]
        private string m_LightFunctionClassName;

        private static List<BaseLightFunction> s_LightFunctions;

        protected override int previewWidth
        {
            get { return 300; }
        }
        
        protected override int previewHeight
        {
            get { return 300; }
        }

        protected override bool generateDefaultInputs { get { return false; } }

        public override void OnEnable()
        {
            base.OnEnable();

            var lightFunction = GetLightFunction();
            lightFunction.DoSlotsForConfiguration(this);
        }

        public override void OnCreate()
        {
            name = "PixelMaster";
            base.OnCreate();
        }

        private static List<BaseLightFunction> GetLightFunctions()
        {
            if (s_LightFunctions == null)
            {
                s_LightFunctions = new List<BaseLightFunction>();

                foreach (Type type in Assembly.GetAssembly(typeof(BaseLightFunction)).GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(BaseLightFunction))))
                    {
                        var func = Activator.CreateInstance(type) as BaseLightFunction;
                        s_LightFunctions.Add(func);
                    }
                }
            }
            return s_LightFunctions;
        }

        private BaseLightFunction GetLightFunction()
        {
            var lightFunctions = GetLightFunctions();
            var lightFunction = lightFunctions.FirstOrDefault(x => x.GetType().ToString() == m_LightFunctionClassName);

            if (lightFunction == null && lightFunctions.Count > 0)
                lightFunction = lightFunctions[0];

            return lightFunction;
        }

        public virtual void GenerateLightFunction(ShaderGenerator visitor)
        {
            var lightFunction = GetLightFunction();
            lightFunction.GenerateLightFunctionName(visitor);
            lightFunction.GenerateLightFunctionBody(visitor);
        }

        public void GenerateSurfaceOutput(ShaderGenerator visitor)
        {
            var lightFunction = GetLightFunction();
            lightFunction.GenerateSurfaceOutputStructureName(visitor);
        }
        
        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var lightFunction = GetLightFunction();
            var firstPassSlotName = lightFunction.GetFirstPassSlotName();
            // do the normal slot first so that it can be used later in the shader :)
            var firstPassSlot = FindInputSlot(firstPassSlotName);
            var nodes = ListPool<BaseMaterialNode>.Get();
            CollectChildNodesByExecutionOrder(nodes, firstPassSlot, false);

            for (int index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            for (int index = 0; index < firstPassSlot.edges.Count; index++)
            {
                var edge = firstPassSlot.edges[index];
                var node = edge.fromSlot.node as BaseMaterialNode;
                shaderBody.AddShaderChunk("o." + firstPassSlot.name + " = " + node.GetOutputVariableNameForSlot(edge.fromSlot, generationMode) + ";", true);
            }

            // track the last index of nodes... they have already been processed :)
            int pass2StartIndex = nodes.Count;

            //Get the rest of the nodes for all the other slots
            CollectChildNodesByExecutionOrder(nodes, null, false);
            for (var i = pass2StartIndex; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

           ListPool<BaseMaterialNode>.Release(nodes);

            foreach (var slot in inputSlots)
            {
                if (slot == firstPassSlot)
                    continue;

                foreach (var edge in slot.edges)
                {
                    var node = edge.fromSlot.node as BaseMaterialNode;
                    shaderBody.AddShaderChunk("o." + slot.name + " = " + node.GetOutputVariableNameForSlot(edge.fromSlot, generationMode) + ";", true);
                }
            }
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return GetOutputVariableNameForNode();
        }

        public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            var lightFunctions = GetLightFunctions();
            var lightFunction = GetLightFunction();

            int lightFuncIndex = 0;
            if (lightFunction != null)
                lightFuncIndex = lightFunctions.IndexOf(lightFunction);

            EditorGUI.BeginChangeCheck();
            lightFuncIndex = EditorGUI.Popup(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), lightFuncIndex, lightFunctions.Select(x => x.GetLightFunctionName()).ToArray(), EditorStyles.popup);
            m_LightFunctionClassName = lightFunctions[lightFuncIndex].GetType().ToString();
            if (EditorGUI.EndChangeCheck())
            {
                var function = GetLightFunction();
                function.DoSlotsForConfiguration(this);
                pixelGraph.RevalidateGraph();
                return GUIModificationType.ModelChanged;
            }
            return GUIModificationType.None;
        }
        public override IEnumerable<Slot> GetDrawableInputProxies()
        {
            return new List<Slot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        protected override bool UpdatePreviewMaterial()
        {
            if (hasError)
                return false;

            var shaderName = "Hidden/PreviewShader/" + name + "_" + Math.Abs(GetInstanceID());
            List<PropertyGenerator.TextureInfo> defaultTextures;
            var resultShader = ShaderGenerator.GenerateSurfaceShader(pixelGraph.owner, shaderName, true, out defaultTextures);
            m_GeneratedShaderMode = PreviewMode.Preview3D;
            hasError = !InternalUpdatePreviewShader(resultShader);
            return true;
        }
    }
}
