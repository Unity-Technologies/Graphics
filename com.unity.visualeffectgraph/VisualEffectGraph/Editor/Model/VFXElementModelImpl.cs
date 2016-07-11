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
    internal static class ProgressBarHelper
    {
        public const int GENERATE_DATA_NB_STEPS = 6;
        public const int COMPILE_SYSTEM_NB_STEPS = 8;

        public static void Init(int nbSteps)
        {
            s_CurrentStep = 0;
            s_NbSteps = nbSteps;
        }

        public static void IncrementStep(string message)
        {
            EditorUtility.DisplayProgressBar("Generate VFX...",message,(float)(s_CurrentStep++) / s_NbSteps);
        }

        public static void SkipSteps(int nb)
        {
            s_NbSteps -= nb;
        }

        public static void Clear()
        {
            EditorUtility.ClearProgressBar();
            s_CurrentStep = 0;
            s_NbSteps = 0;
        }

        private static int s_CurrentStep;
        private static int s_NbSteps;
    }

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
                case InvalidationCause.kDataChanged:
                    m_NeedsNativeDataGeneration = true;
                    break;
            }
        }

        public void Update()
        {
            Profiler.BeginSample("VFXSystemsModel.Update");

            bool needsReinit = false;

            for (int i = 0; i < GetNbChildren(); ++i)
                if (GetChild(i).NeedsComponentUpdate() && GetChild(i).UpdateComponentSystem())
                    m_NeedsNativeDataGeneration = true;

            try
            {
                if (m_NeedsCheck)
                {
                    ProgressBarHelper.Init(ProgressBarHelper.GENERATE_DATA_NB_STEPS + GetNbChildren() * ProgressBarHelper.COMPILE_SYSTEM_NB_STEPS);
                    m_NeedsCheck = false;

                    VFXEditor.Log("\n**** VFXAsset is dirty ****");
                    for (int i = 0; i < GetNbChildren(); ++i)
                    {
                        VFXEditor.Log("Recompile system " + i + " if needed ");
                        if (!GetChild(i).RecompileIfNeeded())
                        {
                            VFXEditor.Log("No need to recompile");
                            ProgressBarHelper.SkipSteps(ProgressBarHelper.COMPILE_SYSTEM_NB_STEPS);
                        }
                        else
                        {
                            ProgressBarHelper.IncrementStep("System " + GetChild(i).Id + ": Update asset");
                            if (GetChild(i).UpdateComponentSystem())
                                needsReinit = true;
                            else
                                GetChild(i).RemoveSystem();
                        }
                    }

                    m_NeedsNativeDataGeneration = true;
                }
                else if (m_NeedsNativeDataGeneration)
                    ProgressBarHelper.Init(ProgressBarHelper.GENERATE_DATA_NB_STEPS);

                // Update assets properties and expressions for C++ evaluation
                if (m_NeedsNativeDataGeneration)
                {
                    m_NeedsNativeDataGeneration = false;

                    GenerateNativeData();

                    m_ReloadUniforms = true;
                    needsReinit = true;

                    ProgressBarHelper.Clear();
                }
            }
            catch(Exception e)
            {
                ProgressBarHelper.Clear();  // Clear the progress bar in case of exception
                throw e; // And rethrow
            }

            if (m_ReloadUniforms) // If has recompiled, re-upload all uniforms as they are not stored in C++. TODO store uniform constant in C++ component ?
            {
                m_ReloadUniforms = false;

                // Update expressions
                VFXAsset asset = VFXEditor.asset;
                foreach (var kvp in m_Expressions)
                {
                    VFXExpression expr = kvp.Key;
                    int index = kvp.Value.index;
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
            }

            if (needsReinit) // Restart component 
                VFXEditor.ForeachComponents(c => c.Reinit());

            Profiler.EndSample();
        }

        private void GenerateNativeData()
        {
            ProgressBarHelper.IncrementStep("Generate data: Collect Expressions");

            m_Expressions.Clear();

            for (int i = 0; i < GetNbChildren(); ++i)
            {
                VFXSystemModel system = GetChild(i);
                VFXSystemRuntimeData rtData = system.RtData;
                if (rtData != null)
                    foreach (var expr in rtData.m_RawExpressions)
                        AddExpressionRecursive(m_Expressions, expr, 0);
            }

            // Collect linked spawners
            HashSet<VFXExpression> spawnerExpressions = new HashSet<VFXExpression>();
            HashSet<VFXSpawnerNodeModel> spawners = new HashSet<VFXSpawnerNodeModel>(); // spawner and index

            for (int i = 0; i < GetNbChildren(); ++i)
            {
                VFXSystemModel system = GetChild(i);
                if (system.RtData != null) // The system has been compiled
                {
                    VFXContextModel context = system.GetChild(0);
                    if (context.GetContextType() != VFXContextDesc.Type.kTypeInit)
                        continue;

                    foreach (var spawner in context.GetSpawners())
                        spawners.Add(spawner);
                }
            }

            // Collect spawner expressions
            foreach (var spawner in spawners)
            {
                int nbBlocks = spawner.GetNbChildren();
                for (int i = 0; i < nbBlocks; ++i)
                {
                    VFXSpawnerBlockModel block = spawner.GetChild(i);
                    for (int j = 0; j < block.GetNbInputSlots(); ++j)
                        block.GetInputSlot(j).CollectExpressions(spawnerExpressions);
                }
            }

            // Add Spawner expressions
            foreach (var expr in spawnerExpressions)
                AddExpressionRecursive(m_Expressions, expr, 0);

            // Exposed expressions
            var exposedDescs = new List<VFXExposedEditorDesc>();
            var exposedExpressions = new List<VFXNamedValue>();
            for (int i = 0; i < VFXEditor.Graph.models.GetNbChildren(); ++i)
            {
                var child = VFXEditor.Graph.models.GetChild(i);
                if (child is VFXDataNodeModel)
                {
                    var dataNode = (VFXDataNodeModel)child;
                    if (!dataNode.Exposed)
                        continue;

                    for (int j = 0; j < dataNode.GetNbChildren(); ++j)
                    {
                        var dataBlock = dataNode.GetChild(j);

                        var desc = new VFXExposedEditorDesc();
                        desc.semanticType = dataBlock.Slot.Semantics.ID;
                        desc.exposedName = dataBlock.ExposedName;
                        desc.worldSpace = dataBlock.Slot.IsTransformWorld();
                        exposedDescs.Add(desc);
                    }
                  
                    dataNode.CollectExposedExpressions(exposedExpressions);
                }
            }

            foreach (var exposedExpr in exposedExpressions)
            {
                if (m_Expressions.ContainsKey(exposedExpr.m_Value))
                {
                    var exprData = m_Expressions[exposedExpr.m_Value];
                    exprData.exposedName = exposedExpr.m_Name;
                    exprData.depth = int.MaxValue; // So that expressions are placed at the very beginning of the expression list
                }
                else
                    Debug.LogWarning("Exposed expression \"" + exposedExpr.m_Name + "\" is not used. Discarded!");
            }

            // TODO Filter unused exposed desc

            // Sort expression by depth so that dependencies will be evaluated in order
            var sortedList = m_Expressions.ToList();
            sortedList.Sort((kvpA, kvpB) =>
            {
                return kvpB.Value.depth.CompareTo(kvpA.Value.depth);
            });
            var expressionList = sortedList.Select(kvp => kvp.Key).ToList();

            // Store the index in the expression data
            for (int i = 0; i < expressionList.Count; ++i)
                m_Expressions[expressionList[i]].index = i;

           /* string debugStr = expressionList.Count + " Expressions:\n";
            foreach (var expr in expressionList)
            {
                var exprData = m_Expressions[expr];
                debugStr += exprData.index + ": " + expr + " " + exprData.exposedName + "\n"; 
            }
            Debug.Log(debugStr);*/

            // Generate signal texture if needed
            ProgressBarHelper.IncrementStep("Generate data: Generate textures");

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

            ProgressBarHelper.IncrementStep("Generate data: Update asset expressions");
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
                        }
                    }
                    else
                    {
                        // Needs to fill the dependencies
                        VFXExpression[] parents = expr.GetParents();
                        int nbParents = parents.Length;
                        int[] parentIds = new int[4];
                        for (int i = 0; i < nbParents; ++i)
                            parentIds[i] = m_Expressions[parents[i]].index;

                        // For transform expression store the type in index 1
                        if (expr is VFXTransformExpression)
                        {
                            // VFXTransformExpression ensures the number of parents is 1, so 1 is a free slot
                            parentIds[1] = (int)((VFXTransformExpression)expr).GetSpaceRef();
                        }

                        asset.AddExpression(expr.Operation, parentIds[0], parentIds[1], parentIds[2], parentIds[3]);
                    }
                }

                // Generate spawner native data

                ProgressBarHelper.IncrementStep("Generate data: Sync spawners");
                asset.ClearSpawnerData();
                foreach (var spawner in spawners)
                {
                    List<uint> spawnerStream = new List<uint>();

                    int nbBlocks = spawner.GetNbChildren();
                    if (nbBlocks == 0)
                        continue;

                    spawnerStream.Add((uint)nbBlocks);
                    for (int i = 0; i < nbBlocks; ++i)
                    {
                        VFXSpawnerBlockModel block = spawner.GetChild(i);
                        spawnerStream.Add((uint)block.SpawnerType);
                        for (int j = 0; j < block.GetNbInputSlots(); ++j)
                            spawnerStream.Add((uint)m_Expressions[block.GetInputSlot(j).ValueRef].index); // Warning: This wont work for composite type but we dont have any in spawners atm
                    }

                    int spawnerIndex = asset.AddSpawner(spawnerStream.ToArray());
                    foreach (var context in spawner.LinkedContexts)
                        asset.LinkSpawner(context.GetOwner().Id, spawnerIndex);

                    // Add events
                    bool hasStartEvents = false;
                    bool hasStopInStart = false;
                    foreach (var e in spawner.StartEvents)
                    {
                        asset.LinkStartEvent(e.Name, spawnerIndex);
                        hasStartEvents = true;
                        if (e.Name == "OnStop")
                            hasStopInStart = true;
                    }
                    if (!hasStartEvents)
                        asset.LinkStartEvent("OnStart", spawnerIndex); // Implicit start event

                    bool hasStopEvents = false;
                    foreach (var e in spawner.StopEvents)
                    {
                        asset.LinkStopEvent(e.Name, spawnerIndex);
                        hasStopEvents = true;
                    }
                    if (!hasStopEvents && !hasStopInStart)
                        asset.LinkStopEvent("OnStop", spawnerIndex); // Implicit stop event (only if stop event is not linked to start)

                }
                // Sync components runtime spawners data with asset data
                VFXEditor.ForeachComponents(c => c.SyncSpawners());

                ProgressBarHelper.IncrementStep("Generate data: Sync exposed values");
                // Sync exposed names
                var exposedName = new List<string>();
                for (int i = 0; i < expressionList.Count; ++i)
                {
                    var expr = m_Expressions[expressionList[i]];
                    if (expr.exposedName != null)
                        exposedName.Add(expr.exposedName);
                }

                asset.SetEditorExposedData(exposedDescs.ToArray());
                asset.SetExposedNames(exposedName.ToArray());
                VFXEditor.ForeachComponents(c => c.SyncExposedValues());

                ProgressBarHelper.IncrementStep("Generate data: Generate uniforms");

                // Finally generate the uniforms
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXSystemModel system = GetChild(i);
                    VFXSystemRuntimeData rtData = system.RtData;
                    if (rtData == null)
                        continue;

                    foreach (var uniform in rtData.uniforms)
                    {
                        int index = m_Expressions[uniform.Key].index;
                        if (uniform.Value.StartsWith("init"))
                            asset.AddInitUniform(system.Id, uniform.Value, index);
                        else if (uniform.Value.StartsWith("update"))
                            asset.AddUpdateUniform(system.Id, uniform.Value, index);
                        else if (uniform.Value.StartsWith("global"))
                        {
                            asset.AddInitUniform(system.Id, uniform.Value, index);
                            asset.AddUpdateUniform(system.Id, uniform.Value, index);
                        }
                    }

                    foreach (var uniform in rtData.outputUniforms)
                    {
                        int index = m_Expressions[uniform.Key].index;
                        asset.AddOutputUniform(system.Id, uniform.Value, index);
                    }
                }
            }
        }

        private void AddExpressionRecursive(Dictionary<VFXExpression, ExpressionData> expressions, VFXExpression expr, int depth)
        {
            ExpressionData exprData;
            if (!expressions.TryGetValue(expr, out exprData))
                exprData = new ExpressionData();
            exprData.depth = Math.Max(exprData.depth, depth);

            expressions[expr] = exprData;
            var parents = expr.GetParents();
            if (parents != null)
                foreach (var parent in parents)
                    AddExpressionRecursive(expressions, parent, exprData.depth + 1);
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
        private bool m_NeedsNativeDataGeneration = false;

        private bool m_PhaseShift = false; // Used to remove sampling discretization issue

        private class ExpressionData
        {
            public ExpressionData()
            {
                exposedName = null;
                index = -1;
                depth = -1;
            }

            public string exposedName;  // null if not exposed else store the exposed name
            public int index;           // index of the expression in the native buffer
            public int depth;           // depth of the dependencies tree of the expression (0 means no parent, 1 one level of parents...) - Used to sort expressions in correct order
        }

        private Dictionary<VFXExpression, ExpressionData> m_Expressions = new Dictionary<VFXExpression, ExpressionData>();

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
        }

        public void DeleteAssets()
        {
            if (VFXEditor.asset == null)
                return;

            string shaderName = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(VFXEditor.asset));
            shaderName += m_ID;

            string localShaderPath = VFXEditor.LocalShaderDir() + shaderName;
            string simulationShaderPath = localShaderPath + ".compute";
            string outputShaderPath = localShaderPath + ".shader";

            AssetDatabase.DeleteAsset(simulationShaderPath);
            AssetDatabase.DeleteAsset(outputShaderPath);

            VFXEditor.Graph.systems.Invalidate(VFXElementModel.InvalidationCause.kParamChanged); // TMP Trigger a uniform reload as importing asset cause material properties to be invalidated
        }

        public override bool CanAddChild(VFXElementModel element, int index = -1)
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
                rtData = VFXModelCompiler.CompileSystem(this);
                m_Dirty = false;
                return true;
            }

            return false;
        }

        public void RemoveSystem()
        {
            Dispose();
            if (rtData == null)
                return;

            VFXEditor.ForeachComponents(c => c.RemoveSystem(m_ID));

            if (VFXEditor.asset != null)
                VFXEditor.asset.RemoveSystem(m_ID);

            DeleteAssets();
            rtData = null;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            RemoveSystem();
            m_Owner.Invalidate(InvalidationCause.kDataChanged);
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
                    bool needsRecompile = value < 256 || m_MaxNb < 256;
                    m_MaxNb = value;
                    m_ForceComponentUpdate = true;
                    if (needsRecompile)
                        Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                    else
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

        private Vector2 m_CameraFadeDistance = Vector2.zero;
        public Vector2 CameraFadeDistance
        {
            get { return m_CameraFadeDistance; }
            set
            {
                Vector2 newDistance = Vector2.Max(Vector2.zero, value);
                if (newDistance.x > newDistance.y)
                {
                    float tmp = newDistance.x;
                    newDistance.x = newDistance.y;
                    newDistance.y = tmp;
                }
                if (m_CameraFadeDistance != newDistance)
                {
                    m_CameraFadeDistance = newDistance;
                    Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                }
            }
        }

        private int m_RenderQueueDelta = 0;
        public int RenderQueueDelta
        {
            get { return m_RenderQueueDelta; }
            set
            {
                int newDelta = value;
                if (m_RenderQueueDelta != newDelta)
                {
                    m_RenderQueueDelta = newDelta;
                    Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                }
            }
        }

        public bool HasSoftParticles()
        {
            return m_BlendMode != BlendMode.kMasked && m_SoftParticlesFadeDistance > 0.0f;
        }

        public bool HasCameraFade()
        {
            return m_BlendMode != BlendMode.kMasked && m_CameraFadeDistance.y > 0.0f;
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

        private bool m_WorldSpace = false;
        public bool WorldSpace
        {
            get { return m_WorldSpace; }
            set 
            {
                if (m_WorldSpace != value)
                {
                    m_WorldSpace = value;
                    Invalidate(InvalidationCause.kModelChanged); // Force a recompilation
                }
            }
        }

        public SpaceRef GetSpaceRef()
        {
            return WorldSpace ? SpaceRef.kWorld : SpaceRef.kLocal;
        }

        public VFXGeneratedTextureData GeneratedTextureData { get { return GetOwner().GeneratedTextureData; } }

        public bool UpdateComponentSystem()
        {
            if (rtData == null)
                return false;

            m_ForceComponentUpdate = false;

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
   
            return true;
        }

        public bool NeedsComponentUpdate() { return m_ForceComponentUpdate; }
        private bool m_ForceComponentUpdate = false;

        // Careful with that !
        public static void ReinitIDsProvider()
        {
            NextSystemID = 0;
        }

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

        public bool Accept(VFXBlockDesc block)
        {
            if ((block.CompatibleContexts & m_Desc.m_Type) == 0)
                return false;

            if (m_Desc.m_Type == VFXContextDesc.Type.kTypeOutput)
            {
                if (block.IsSet(VFXBlockDesc.Flag.kHasRand) || block.IsSet(VFXBlockDesc.Flag.kHasKill))
                    return false; // Cant add block that needs a random number or can kill to output
            }

            return true;
        }

        public override bool CanAddChild(VFXElementModel element, int index = -1)
        {
            if (!base.CanAddChild(element, index) || m_Desc.m_Type == VFXContextDesc.Type.kTypeNone)
                return false;

            VFXBlockModel block = (VFXBlockModel)element; // Wont throw as we know it is a block model at this point
            return Accept(block.Desc);
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

        public bool Link(VFXSpawnerNodeModel spawner,bool reentrant = false)
        {
            if (reentrant || spawner.Link(this,true))
            {
                m_Spawners.Add(spawner);
                Invalidate(InvalidationCause.kDataChanged);
                return true;
            }

            return false;
        }

        public bool Unlink(VFXSpawnerNodeModel spawner,bool reentrant = false)
        {
            if (reentrant || spawner.Unlink(this, true))
            {
                bool res = m_Spawners.Remove(spawner);
                Invalidate(InvalidationCause.kDataChanged);
                return res;
            }

            return false;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            while (m_Spawners.Count > 0)
                Unlink(m_Spawners[0]);
        }

        public IEnumerable<VFXSpawnerNodeModel> GetSpawners() { return m_Spawners; }

        private VFXContextDesc m_Desc;

        public bool UICollapsed     { get { return m_UICollapsed; } }
        public Vector2 UIPosition   { get {return m_UIPosition; } }
        
        private bool m_UICollapsed;
        private Vector2 m_UIPosition;

        private List<VFXSpawnerNodeModel> m_Spawners = new List<VFXSpawnerNodeModel>();
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

        public override bool CanAddChild(VFXElementModel element, int index = -1)
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
