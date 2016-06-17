using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.VFX;

namespace UnityEditor.Experimental
{
    public enum BlendMode
    {
        kMasked = 0,
        kAdditive = 1,
        kAlpha = 2,
    }

    public interface VFXModelController
    {
        void SyncView(VFXElementModel model, bool recursive = false);
    }

    public class VFXGraph
    {
        public VFXSystemsModel systems = new VFXSystemsModel();
        public VFXModelContainer models = new VFXModelContainer(); // other model (data nodes...)
    }

    // Generic model container
    public class VFXModelContainer : VFXElementModel<VFXElementModel, VFXElementModel>
    {
        protected override void InnerInvalidate(InvalidationCause cause)
        {
            ++m_InvalidateID;
            Dirty = true;
        }

        public int InvalidateID { get { return m_InvalidateID; } }
        private int m_InvalidateID = 0;

        public bool Dirty = false;
    } 

    public class VFXSystemsModel : VFXElementModel<VFXElementModel, VFXSystemModel>
    {
        public void Dispose()
        {
            for (int i = 0; i < GetNbChildren(); ++i)
            {
                GetChild(i).Dispose();
                //GetChild(i).DeleteAssets();
            }             
        }

        protected override void InnerInvalidate(InvalidationCause cause)
        {
            ++m_InvalidateID;
            Dirty = true;
            switch(cause)
            {
                case InvalidationCause.kModelChanged:
                    m_NeedsCheck = true;
                    break;
                case InvalidationCause.kParamChanged:
                    m_ReloadUniforms = true;
                    break;
            }
        }

        public void Update()
        {
            Profiler.BeginSample("VFXSystemsModel.Update");

            bool needsNativeDataGeneration = false;
            for (int i = 0; i < GetNbChildren(); ++i)
                if (GetChild(i).NeedsComponentUpdate())
                {
                    GetChild(i).UpdateComponentSystem();
                    needsNativeDataGeneration = true;
                }

            bool HasRecompiled = false;
          //  bool NeedsExpressionsUpdate = false;
            if (m_NeedsCheck)
            {
                m_NeedsCheck = false;
            //    NeedsExpressionsUpdate = true;

                VFXEditor.Log("\n**** VFXAsset is dirty ****");
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXEditor.Log("Recompile system " + i + " if needed ");
                    if (!GetChild(i).RecompileIfNeeded())
                        VFXEditor.Log("No need to recompile");
                    else
                    {                     
                        if (GetChild(i).UpdateComponentSystem())
                            HasRecompiled = true;
                        else
                            GetChild(i).RemoveSystem();
                    }
                }

                needsNativeDataGeneration = true;
            }

            // Update assets properties and expressions for C++ evaluation
            if (needsNativeDataGeneration)
            {
                GenerateNativeData();
                m_ReloadUniforms = true;
            }

            if (m_ReloadUniforms) // If has recompiled, re-upload all uniforms as they are not stored in C++. TODO store uniform constant in C++ component ?
            {
                m_ReloadUniforms = false;

                // Update expressions
                VFXAsset asset = VFXEditor.asset;
                foreach (var kvp in m_Expressions)
                {
                    VFXExpression expr = kvp.Key;
                    int index = kvp.Value;
                    if (expr.IsValue(false))
                    {
                        switch (expr.ValueType)
                        {
                            case VFXValueType.kFloat: 
                                asset.SetFloat(index,expr.Get<float>()); 
                                break;
                            case VFXValueType.kFloat2:
                                asset.SetVector2(index,expr.Get<Vector2>()); 
                                break;
                            case VFXValueType.kFloat3:
                                asset.SetVector3(index,expr.Get<Vector3>()); 
                                break;
                            case VFXValueType.kFloat4:
                                asset.SetVector4(index,expr.Get<Vector4>()); 
                                break;
                            case VFXValueType.kTexture2D:
                                asset.SetTexture2D(index,expr.Get<Texture2D>());
                                break;
                            case VFXValueType.kTexture3D:
                                asset.SetTexture3D(index,expr.Get<Texture3D>());
                                break;
                            case VFXValueType.kTransform:
                                asset.SetMatrix(index,expr.Get<Matrix4x4>());
                                break;
                            // curve and gradient uniform dont change, only the correponding textures are updated
                        }
                    }
                }

                m_TextureData.UpdateAndUploadDirty();

               /* VFXEditor.Log("Uniforms have been modified");
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    var system = GetChild(i);

                    system.GeneratedTextureData.UpdateAndUploadDirty();

                    if (system.RtData != null)
                        system.RtData.UpdateAllUniforms();            
                } */
            }

            if (HasRecompiled) // Restart component 
                VFXEditor.ForeachComponents(c => c.Reinit());

            Profiler.EndSample();
        }

        private void GenerateNativeData()
        {
            m_Expressions.Clear();

            for (int i = 0; i < GetNbChildren(); ++i)
            {
                VFXSystemModel system = GetChild(i);
                VFXSystemRuntimeData rtData = system.RtData;
                if (rtData != null)
                    foreach (var expr in rtData.m_RawExpressions)
                        AddExpressionRecursive(m_Expressions, expr, 0);
            }

            /*Debug.Log("NB EXPRESSIONS: " + m_Expressions.Count);
            foreach (var expr in m_Expressions)
            {
                Debug.Log(expr.Key.ToString() + " | " + expr.Value);
            }*/

            // Sort expression per depth so that we're sure dependencies will be evaluated after dependents
            var sortedList = m_Expressions.ToList();
            sortedList.Sort((kvpA, kvpB) =>
            {
                return kvpB.Value.CompareTo(kvpA.Value);
            });
            var expressionList = sortedList.Select(kvp => kvp.Key).ToList();

            //Debug.Log("SORTED EXPRESSIONS: " + expressionList.Count);
            // Finally we dont need the depth anymore, so use that int to store the index in the array instead
            for (int i = 0; i < expressionList.Count; ++i)
            {
                m_Expressions[expressionList[i]] = i;
                //Debug.Log(expressionList[i].ToString());
            }

            // Generate signal texture if needed
            List<VFXValue> signals = new List<VFXValue>();
            foreach (var expr in expressionList)
                if (expr.IsValue(false) && (expr.ValueType == VFXValueType.kColorGradient || expr.ValueType == VFXValueType.kCurve))
                    signals.Add((VFXValue)expr);

            m_TextureData.RemoveAllValues();
            m_TextureData.AddValues(signals);
            m_TextureData.Generate();

            VFXAsset asset = VFXEditor.asset;

            if (m_TextureData.HasColorTexture())
                asset.SetGradientTexture(m_TextureData.ColorTexture);
            else
                asset.SetGradientTexture(null);
            if (m_TextureData.HasFloatTexture())
                asset.SetCurveTexture(m_TextureData.FloatTexture);
            else
                asset.SetCurveTexture(null);

            if (asset != null)
            {
                asset.ClearPropertyData();
                foreach (var expr in expressionList)
                {
                    if (expr.IsValue(false)) // check non reduced value
                    {
                        VFXValue value = (VFXValue)expr;
                        switch (value.ValueType)
                        {
                            case VFXValueType.kFloat:
                                asset.AddFloat(value.Get<float>());
                                break;
                            case VFXValueType.kFloat2:
                                asset.AddVector2(value.Get<Vector2>());
                                break;
                            case VFXValueType.kFloat3:
                                asset.AddVector3(value.Get<Vector3>());
                                break;
                            case VFXValueType.kFloat4:
                                asset.AddVector4(value.Get<Vector4>());
                                break;
                            case VFXValueType.kTexture2D:
                                asset.AddTexture2D(value.Get<Texture2D>());
                                break;
                            case VFXValueType.kTexture3D:
                                asset.AddTexture3D(value.Get<Texture3D>());
                                break;
                            case VFXValueType.kTransform:
                                asset.AddMatrix(value.Get<Matrix4x4>());
                                break;
                            case VFXValueType.kCurve:
                                asset.AddVector4(m_TextureData.GetCurveUniform(value));
                                break;
                            case VFXValueType.kColorGradient:
                                asset.AddFloat(m_TextureData.GetGradientUniform(value));
                                break;
                            default:
                                throw new Exception("Invalid value");
                                break;
                        }
                    }
                    else
                    {
                        // Needs to fill the dependencies
                        VFXExpression[] parents = expr.GetParents();
                        int nbParents = parents.Length;
                        int[] parentIds = new int[4];
                        for (int i = 0; i < nbParents; ++i)
                            parentIds[i] = m_Expressions[parents[i]];
                        asset.AddExpression(expr.Operation, parentIds[0], parentIds[1], parentIds[2], parentIds[3]);
                    }
                }

                // Finally generate the uniforms
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXSystemModel system = GetChild(i);
                    VFXSystemRuntimeData rtData = system.RtData;
                    if (rtData == null)
                        continue;

                    foreach (var uniform in rtData.uniforms)
                    {
                        int index = m_Expressions[uniform.Key];
                        if (uniform.Value.StartsWith("init"))
                        {
                            asset.AddInitUniform(system.Id, uniform.Value, index);
                            //Debug.Log("ADD INIT UNIFORM: " + system.Id + " " + uniform.Value + " " + index);
                        }
                        else if (uniform.Value.StartsWith("update"))
                        {
                            asset.AddUpdateUniform(system.Id, uniform.Value, index);
                            //Debug.Log("ADD UPDATE UNIFORM: " + system.Id + " " + uniform.Value + " " + index);
                        }
                        else if (uniform.Value.StartsWith("global"))
                        {
                            asset.AddInitUniform(system.Id, uniform.Value, index);
                            asset.AddUpdateUniform(system.Id, uniform.Value, index);
                            //Debug.Log("ADD GLOBAL UNIFORM: " + system.Id + " " + uniform.Value + " " + index);
                        }
                    }

                    foreach (var uniform in rtData.outputUniforms)
                    {
                        int index = m_Expressions[uniform.Key];
                        asset.AddOutputUniform(system.Id, uniform.Value, index);
                        //Debug.Log("ADD OUTPUT UNIFORM: " + system.Id + " " + uniform.Value + " " + index);
                    }
                }
            }
        }

        private void AddExpressionRecursive(Dictionary<VFXExpression, int> expressions, VFXExpression expr, int depth)
        {
            int exprDepth;
            if (!expressions.TryGetValue(expr, out exprDepth))
                exprDepth = -1;
            exprDepth = Math.Max(exprDepth, depth);

            expressions[expr] = exprDepth;
            var parents = expr.GetParents();
            if (parents != null)
                foreach (var parent in parents)
                    AddExpressionRecursive(expressions, parent, exprDepth + 1);
        }

        public bool PhaseShift
        {
            get { return m_PhaseShift; }
            set
            {
                if (m_PhaseShift != value)
                {
                    m_PhaseShift = value;
                    for (int i = 0; i < GetNbChildren(); ++i)
                        GetChild(i).Invalidate(InvalidationCause.kModelChanged);
                }
            }
        }

        public int InvalidateID { get { return m_InvalidateID; }}
        private int m_InvalidateID = 0;

        private bool m_NeedsCheck = false;
        private bool m_ReloadUniforms = false;
        private bool m_PhaseShift = false; // Used to remove sampling discretization issue

        private Dictionary<VFXExpression, int> m_Expressions = new Dictionary<VFXExpression, int>();

        public VFXGeneratedTextureData GeneratedTextureData { get { return m_TextureData; } }
        private VFXGeneratedTextureData m_TextureData = new VFXGeneratedTextureData();

        public bool Dirty = false;
    }

    public class VFXSystemModel : VFXElementModel<VFXSystemsModel, VFXContextModel>
    {
        public VFXSystemModel()
        {
            m_ID = NextSystemID;
            NextSystemID += 1;
        }

        public void Dispose()
        {
            //if (rtData != null)
            //    UnityEngine.Object.DestroyImmediate(rtData.m_Material);

            //m_GeneratedTextureData.Dispose();
        }

        public void DeleteAssets()
        {
            if (VFXEditor.asset == null)
                return;

            string shaderName = shaderName = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(VFXEditor.asset)); ;
            shaderName += m_ID;

            string simulationShaderPath = "Assets/VFXEditor/Generated/" + shaderName + ".compute";
            string outputShaderPath = "Assets/VFXEditor/Generated/" + shaderName + ".shader";
            string materialPath = "Assets/VFXEditor/Generated/" + shaderName + ".mat";

            AssetDatabase.DeleteAsset(simulationShaderPath);
            AssetDatabase.DeleteAsset(outputShaderPath);
            AssetDatabase.DeleteAsset(materialPath);

            VFXEditor.Graph.systems.Invalidate(VFXElementModel.InvalidationCause.kParamChanged); // TMP Trigger a uniform reload as importing asset cause material properties to be invalidated
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            if (!base.CanAddChild(element, index))
                return false;

            VFXContextDesc.Type contextType = (element as VFXContextModel).GetContextType();
            if (contextType == VFXContextDesc.Type.kTypeNone)
                return false;

            // Check if context types are inserted in the right order
            int realIndex = index == -1 ? m_Children.Count : index;
            if (realIndex > 0 && GetChild(realIndex - 1).GetContextType() > contextType)
                return false;
            //if (realIndex < m_Children.Count && GetChild(realIndex).GetContextType() < contextType)
            //	return false;

            return true;
        }

        public static bool ConnectContext(VFXContextModel context0, VFXContextModel context1, VFXModelController controller = null)
        {
            if (context0 == context1)
                return false;

            VFXSystemModel system0 = context0.GetOwner();
            int context0Index = system0.GetIndex(context0);

            if (system0 == context1.GetOwner() && context0Index > context1.GetOwner().GetIndex(context1))
                return false;

            if (!system0.CanAddChild(context1, context0Index + 1))
                return false;

            if (system0.GetNbChildren() > context0Index + 1)
            {
                VFXSystemModel newSystem = new VFXSystemModel();

                while (system0.GetNbChildren() > context0Index + 1)
                    system0.m_Children[context0Index + 1].Attach(newSystem,true);

                VFXEditor.Graph.systems.AddChild(newSystem);
                if (controller != null)
                    controller.SyncView(newSystem);
            }

            VFXSystemModel system1 = context1.GetOwner();
            int context1Index = system1.m_Children.IndexOf(context1);

            // Then we append context1 and all following contexts to system0
            while (system1.GetNbChildren() > context1Index)
                system1.m_Children[context1Index].Attach(system0,true);

            if (controller != null)
            {
                controller.SyncView(system0);
                controller.SyncView(system1);
            }

            return true;
        }


        public static bool DisconnectContext(VFXContextModel context,VFXModelController controller = null)
        {
            VFXSystemModel system = context.GetOwner();
            if (system == null)
                return false;

            int index = system.GetIndex(context);
            if (index == 0)
                return false;

            VFXSystemModel newSystem = new VFXSystemModel();
            while (system.GetNbChildren() > index)
                system.GetChild(index).Attach(newSystem,true);
            newSystem.Attach(VFXEditor.Graph.systems);

            if (controller != null)
            {
                controller.SyncView(newSystem);
                controller.SyncView(system);
            }
            
            return true;
        }

        protected override void InnerInvalidate(InvalidationCause cause)
        {
            if (m_Children.Count == 0 && m_Owner != null) // If the system has no more attached contexts, remove it
            {
                RemoveSystem();
                Detach();
                return;
            }

            if (cause == InvalidationCause.kModelChanged)
                m_Dirty = true;
        }

        public bool RecompileIfNeeded()
        {
            if (m_Dirty)
            {
                //if (rtData != null)
                //    UnityEngine.Object.DestroyImmediate(rtData.m_Material); 
                rtData = VFXModelCompiler.CompileSystem(this);
                m_Dirty = false;
                return true;
            }

            return false;
        }

        public void RemoveSystem()
        {
            Dispose();
            //if (force || rtData != null)

            if (VFXEditor.asset != null)
                VFXEditor.asset.RemoveSystem(m_ID);

            VFXEditor.ForeachComponents(c => c.RemoveSystem(m_ID));

            DeleteAssets();   
        }

        private bool m_Dirty = true;

        private VFXSystemRuntimeData rtData;
        public VFXSystemRuntimeData RtData
        {
            get { return rtData; }
        }

        private const uint INITIAL_MAX_NB = 1 << 20;

        private uint m_MaxNb = INITIAL_MAX_NB;
        public uint MaxNb
        {
            get { return m_MaxNb; }
            set 
            {
                if (m_MaxNb != value)
                {
                    m_MaxNb = value;
                    m_ForceComponentUpdate = true;
                }
            }
        }

        private float m_SpawnRate = INITIAL_MAX_NB / 10.0f;
        public float SpawnRate
        {
            get { return m_SpawnRate; }
            set
            {
                if (m_SpawnRate != value)
                {
                    m_SpawnRate = value;
                    m_ForceComponentUpdate = true;
                }
            }
        }

        private BlendMode m_BlendMode = BlendMode.kAdditive;
        public BlendMode BlendingMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode != value)
                {
                    m_BlendMode = value;
                    Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                }
            }
        }

        private float m_SoftParticlesFadeDistance = 0.0f;
        public float SoftParticlesFadeDistance
        {
            get { return m_SoftParticlesFadeDistance; }
            set
            {
                float newDistance = Mathf.Max(0.0f, value);
                if (m_SoftParticlesFadeDistance != newDistance)
                {
                    m_SoftParticlesFadeDistance = newDistance;
                    Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                }
            }
        }

        public bool HasSoftParticles()
        {
            return m_BlendMode != BlendMode.kMasked && m_SoftParticlesFadeDistance > 0.0f;
        }

        private int m_OrderPriority = 0; // TODO Get last priority
        public int OrderPriority
        {
            get { return m_OrderPriority; }
            set
            {
                if (m_OrderPriority != value)
                {
                    m_OrderPriority = value;
                    m_ForceComponentUpdate = true;
                }
            }
        }

        public VFXGeneratedTextureData GeneratedTextureData { get { return GetOwner().GeneratedTextureData; } }
        //private VFXGeneratedTextureData m_GeneratedTextureData = new VFXGeneratedTextureData();

        public bool UpdateComponentSystem()
        {
            if (rtData == null)
                return false;

            if (VFXEditor.asset != null)
            {
                VFXEditor.asset.SetSystem(
                    m_ID,
                    MaxNb,
                    rtData.SimulationShader,
                    rtData.OutputShader,
                    rtData.buffersDesc,
                    rtData.outputType,
                    SpawnRate,
                    OrderPriority,
                    rtData.hasKill
                );

                VFXEditor.ForeachComponents(c => c.vfxAsset = VFXEditor.asset);
            }  

           /* VFXEditor.component.SetSystem(
                m_ID,
                MaxNb,
                rtData.SimulationShader,
                rtData.m_Material,
                rtData.buffersDesc,
                rtData.outputType,
                SpawnRate,
                OrderPriority,
                rtData.hasKill);*/

            m_ForceComponentUpdate = false;
            return true;
        }

        public bool NeedsComponentUpdate() { return m_ForceComponentUpdate; }
        private bool m_ForceComponentUpdate = false;

        private static uint NextSystemID = 0;
        private uint m_ID; 

        public uint Id
        {
            get { return m_ID; }
        }
    }

    public class VFXContextModel : VFXModelWithSlots<VFXSystemModel, VFXBlockModel>, VFXUIDataHolder
    {
        public VFXContextModel(VFXContextDesc desc)
        {
            m_Desc = desc;
            InitSlots(desc.m_Properties,null);
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return base.CanAddChild(element, index) && m_Desc.m_Type != VFXContextDesc.Type.kTypeNone;
            // TODO Check if the block is compatible with the context
        }

        public override void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            if (slot.ValueType == VFXValueType.kColorGradient || slot.ValueType == VFXValueType.kCurve)
            {
                var system = GetOwner();
                if (system != null)
                    system.GeneratedTextureData.SetDirty(slot.ValueRef.Reduce() as VFXValue);
            }

            base.OnSlotEvent(type, slot);
        }

        public VFXContextDesc.Type GetContextType()
        {
            return Desc.m_Type;
        }

        public VFXContextDesc Desc
        {
            set
            {
                if (m_Desc != value)
                    if (m_Desc.m_Type == value.m_Type)
                    {
                        m_Desc = value;
                        InitSlots(value.m_Properties,null);
                        Invalidate(InvalidationCause.kModelChanged);
                    }
                    else
                        throw new ArgumentException("Cannot dynamically change the type of a context");
            }
            get { return m_Desc; }
        }

        public int GetNbSlots() { return GetNbInputSlots(); }
        public VFXInputSlot GetSlot(int index) { return GetInputSlot(index); }

        public void UpdateCollapsed(bool collapsed)
        {
            if (m_UICollapsed != collapsed)
            {
                m_UICollapsed = collapsed;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

        public void UpdatePosition(Vector2 position)
        {
            if (m_UIPosition != position)
            {
                m_UIPosition = position;
                Invalidate(InvalidationCause.kUIChanged);
            }
        }

       /* public bool Link(VFXSpawnerNodeModel spawner,bool reentrant = false)
        {
            if (reentrant || spawner.Link(this,true))
            {
                spawners.Add(spawner);
                return true;
            }

            return false;
        }

        public bool Unlink(VFXSpawnerNodeModel spawner,bool reentrant = false)
        {
            if (reentrant || spawner.Unlink(this,true))
                return spawners.Remove(spawner);

            return false;
        }*/

        private VFXContextDesc m_Desc;

        public bool UICollapsed     { get { return m_UICollapsed; } }
        public Vector2 UIPosition   { get {return m_UIPosition; } }
        
        private bool m_UICollapsed;
        private Vector2 m_UIPosition;

        //private List<VFXSpawnerNodeModel> spawners;
    }

    public class VFXBlockModel : VFXModelWithSlots<VFXContextModel, VFXElementModel>, VFXUIDataHolder
    {
        public override void OnSlotEvent(VFXPropertySlot.Event type, VFXPropertySlot slot)
        {
            if (slot.ValueType == VFXValueType.kColorGradient || slot.ValueType == VFXValueType.kCurve)
            {
                var context = GetOwner();
                if (context != null)
                {
                    var system = context.GetOwner();
                    if (system != null)
                        system.GeneratedTextureData.SetDirty(slot.ValueRef.Reduce() as VFXValue);
                }
            }

            base.OnSlotEvent(type, slot);
        }

        public VFXBlockModel(VFXBlockDesc desc)
        {
            m_BlockDesc = desc;
            InitSlots(Properties,null);
        }

        public VFXBlockDesc Desc
        {
            get { return m_BlockDesc; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (m_BlockDesc == null || !m_BlockDesc.Equals(value)) // block desc has changed
                {
                    m_BlockDesc = value;
                    InitSlots(Properties,null);
                    Invalidate(InvalidationCause.kModelChanged);
                }
            }
        }

        public bool Enabled
        {
            get { return m_Enabled; }
            set
            {
                bool oldValue = m_Enabled;
                if (oldValue != value)
                {
                    m_Enabled = value;
                    Invalidate(InvalidationCause.kModelChanged); // Trigger a recompilation
                }
            }

        }

        public int GetNbSlots()                 { return GetNbInputSlots(); }
        public VFXInputSlot GetSlot(int index)  { return GetInputSlot(index); }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        public void UpdatePosition(Vector2 position) {}
        public void UpdateCollapsed(bool collapsed)
        {
            if (m_UICollapsed != collapsed)
            {
                m_UICollapsed = collapsed;
                Invalidate(InvalidationCause.kUIChanged);
            }

        }

        public VFXProperty[] Properties { get { return m_BlockDesc.Properties; } }

        private VFXBlockDesc m_BlockDesc;
        private bool m_Enabled = true;

        public bool UICollapsed { get { return m_UICollapsed; } }
        private bool m_UICollapsed;   
    }
}
