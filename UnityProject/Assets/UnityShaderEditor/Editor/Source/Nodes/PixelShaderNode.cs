using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class PixelShaderNode : BaseMaterialNode, IGeneratesBodyCode
    {
        [SerializeField]
        public string m_LightFunctionClassName;

        private string lightFunctionClassName
        {
            get { return m_LightFunctionClassName; }
            set { m_LightFunctionClassName = value; }
        }

        private static List<BaseLightFunction> s_LightFunctions;

        public PixelShaderNode(BaseMaterialGraph owner) 
            : base(owner)
        {
            name = "PixelMaster";
            GetLightFunction().DoSlotsForConfiguration(this); 
        }

        protected override int previewWidth
        {
            get { return 300; }
        }
        
        protected override int previewHeight
        {
            get { return 300; }
        }

        protected override bool generateDefaultInputs { get { return false; } }

        public override bool canDeleteNode { get { return false; } }

        
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
            var lightFunction = lightFunctions.FirstOrDefault(x => x.GetType().ToString() == lightFunctionClassName);

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

            foreach (var edge in owner.GetEdges(firstPassSlot))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGUID(outputRef.nodeGuid);
                var fromSlot = fromNode.FindOutputSlot(outputRef.slotName);
                shaderBody.AddShaderChunk("o." + firstPassSlot.name + " = " + fromNode.GetOutputVariableNameForSlot(fromSlot, generationMode) + ";", true);
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
                
                foreach (var edge in owner.GetEdges(slot))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGUID(outputRef.nodeGuid);
                    var fromSlot = fromNode.FindOutputSlot(outputRef.slotName);
                    shaderBody.AddShaderChunk("o." + slot.name + " = " + fromNode.GetOutputVariableNameForSlot(fromSlot, generationMode) + ";", true);
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
            lightFunctionClassName = lightFunctions[lightFuncIndex].GetType().ToString();
            if (EditorGUI.EndChangeCheck())
            {
                var function = GetLightFunction();
                function.DoSlotsForConfiguration(this);
                owner.RevalidateGraph();
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

        protected override bool UpdatePreviewShader()
        {
           // if (hasError)
                return false;

            var shaderName = "Hidden/PreviewShader/" + name + "_" + guid;
            List<PropertyGenerator.TextureInfo> defaultTextures;
            var resultShader = ShaderGenerator.GenerateSurfaceShader(owner.owner, shaderName, true, out defaultTextures);
            m_GeneratedShaderMode = PreviewMode.Preview3D;
            hasError = !InternalUpdatePreviewShader(resultShader);
            return true;
        }
    }
}
