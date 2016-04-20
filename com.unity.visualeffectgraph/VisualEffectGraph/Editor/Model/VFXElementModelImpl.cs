using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public enum BlendMode
    {
        kMasked = 0,
        kAdditive = 1,
        kAlpha = 2,
    }

    public class VFXAssetModel : VFXElementModel<VFXElementModel, VFXSystemModel>
    {
        public VFXAssetModel()
        {
            RemovePreviousVFXs();
            RemovePreviousShaders();

            m_GameObject = new GameObject("VFX");
            //gameObject.hideFlags = HideFlags.DontSaveInEditor;
            m_Component = m_GameObject.AddComponent<VFXComponent>();
        }

        private void RemovePreviousVFXs() // Hack method to remove previous VFXs just in case...
        {
            var vfxs = GameObject.FindObjectsOfType(typeof(VFXComponent)) as VFXComponent[];
           
            int nbDeleted = 0;
            foreach (var vfx in vfxs)
                if (vfx != null && vfx.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(vfx.gameObject);
                    ++nbDeleted;
                }

            if (nbDeleted > 0)
                Debug.Log("Remove " + nbDeleted + " old VFX gameobjects");
        }

        private void RemovePreviousShaders()
        {
            // Remove any shader assets in generated path
            string[] guids = AssetDatabase.FindAssets("",new string[] {"Assets/VFXEditor/Generated"});

            foreach (var guid in guids)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            if (guids.Length > 0)
                Debug.Log("Remove " + guids.Length + " old VFX shaders");
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(gameObject);
            for (int i = 0; i < GetNbChildren(); ++i)
            {
                GetChild(i).Dispose();
                GetChild(i).DeleteAssets();
            }             
        }

        public override void Invalidate(InvalidationCause cause)
        {
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
            bool HasRecompiled = false;
            if (m_NeedsCheck)
            {
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

                m_NeedsCheck = false;
            }

            if (m_ReloadUniforms) // If has recompiled, re-upload all uniforms as they are not stored in C++. TODO store uniform constant in C++ component ?
            {
                VFXEditor.Log("Uniforms have been modified");
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXSystemRuntimeData rtData = GetChild(i).RtData;
                    if (rtData != null)
                        rtData.UpdateAllUniforms();
                }
                m_ReloadUniforms = false;
            }

            if (HasRecompiled) // Restart component 
                m_Component.Reinit();
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

        public GameObject gameObject { get { return m_GameObject; } }
        public VFXComponent component { get { return m_Component; } }

        private bool m_NeedsCheck = false;
        private bool m_ReloadUniforms = false;
        private bool m_PhaseShift = false; // Used to remove sampling discretization issue

        private VFXComponent m_Component;
        private GameObject m_GameObject;
    }

    public class VFXSystemModel : VFXElementModel<VFXAssetModel, VFXContextModel>
    {
        public VFXSystemModel()
        {
            m_ID = NextSystemID;
            NextSystemID += 1;
        }

        public void Dispose()
        {
            if (rtData != null)
                UnityEngine.Object.DestroyImmediate(rtData.m_Material); 
        }

        public void DeleteAssets()
        {
            string shaderName = "VFX_";
            shaderName += m_ID;

            string simulationShaderPath = "Assets/VFXEditor/Generated/" + shaderName + ".compute";
            string outputShaderPath = "Assets/VFXEditor/Generated/" + shaderName + ".shader";

            AssetDatabase.DeleteAsset(simulationShaderPath);
            AssetDatabase.DeleteAsset(outputShaderPath);

            VFXEditor.AssetModel.Invalidate(VFXElementModel.InvalidationCause.kParamChanged); // TMP Trigger a uniform reload as importing asset cause material properties to be invalidated
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

        public static bool ConnectContext(VFXContextModel context0, VFXContextModel context1)
        {
            if (context0 == context1)
                return false;

            VFXSystemModel system0 = context0.GetOwner();
            int context0Index = system0.m_Children.IndexOf(context0);

            if (!system0.CanAddChild(context1, context0Index + 1))
                return false;

            // If context0 is not the last one in system0, we need to reattach following contexts to a new one
            if (system0.GetNbChildren() > context0Index + 1)
            {
                VFXSystemModel newSystem = new VFXSystemModel();
                while (system0.GetNbChildren() > context0Index + 1)
                    system0.m_Children[context0Index + 1].Attach(newSystem);
                VFXEditor.AssetModel.AddChild(newSystem);
            }

            VFXSystemModel system1 = context1.GetOwner();
            int context1Index = system1.m_Children.IndexOf(context1);

            // Then we append context1 and all following contexts to system0
            while (system1.GetNbChildren() > context1Index)
                system1.m_Children[context1Index].Attach(system0);

            return true;
        }

        public override void Invalidate(InvalidationCause cause)
        {
            if (m_Children.Count == 0 && m_Owner != null) // If the system has no more attached contexts, remove it
            {
                RemoveSystem();

                var oldOwner = GetOwner();
                Detach();

                return;
            }

            if (cause == InvalidationCause.kModelChanged)
                m_Dirty = true;

            if (m_Owner != null)
                m_Owner.Invalidate(cause);
        }

        public bool RecompileIfNeeded()
        {
            if (m_Dirty)
            {
                if (rtData != null)
                    UnityEngine.Object.DestroyImmediate(rtData.m_Material); 
                rtData = VFXModelCompiler.CompileSystem(this);
                m_Dirty = false;
                return true;
            }

            return false;
        }

        public void RemoveSystem(/*bool force = false*/)
        {
            Dispose();
            //if (force || rtData != null)
            GetOwner().component.RemoveSystem(m_ID);
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
                    UpdateComponentSystem();
                    GetOwner().component.Reinit();
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
                    UpdateComponentSystem();
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

        private int m_OrderPriority = 0; // TODO Get last priority
        public int OrderPriority
        {
            get { return m_OrderPriority; }
            set
            {
                if (m_OrderPriority != value)
                {
                    m_OrderPriority = value;
                    UpdateComponentSystem();
                }
            }
        }

        public bool UpdateComponentSystem()
        {
            if (rtData == null)
                return false;

            GetOwner().component.SetSystem(
                m_ID,
                MaxNb,
                rtData.SimulationShader,
                rtData.m_Material,
                rtData.buffersDesc,
                rtData.outputType,
                SpawnRate,
                OrderPriority,
                rtData.hasKill);

            return true;
        }

        private static uint NextSystemID = 0;
        private uint m_ID; 

        public uint Id
        {
            get { return m_ID; }
        }
    }

    public class VFXContextModel : VFXModelWithSlots<VFXSystemModel, VFXBlockModel>
    {
        public VFXContextModel(VFXContextDesc desc)
        {
            m_Desc = desc;
            InitSlots(desc.m_Properties);
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return base.CanAddChild(element, index) && m_Desc.m_Type != VFXContextDesc.Type.kTypeNone;
            // TODO Check if the block is compatible with the context
        }

        public override void Invalidate(InvalidationCause cause)
        {
            if (m_Owner != null && Desc.m_Type != VFXContextDesc.Type.kTypeNone)
                m_Owner.Invalidate(cause);
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
                        InitSlots(value.m_Properties);
                        Invalidate(InvalidationCause.kModelChanged);
                    }
                    else
                        throw new ArgumentException("Cannot dynamically change the type of a context");
            }
            get { return m_Desc; }
        }

        private VFXContextDesc m_Desc;
    }

    public class VFXBlockModel : VFXModelWithSlots<VFXContextModel, VFXElementModel>
    {
        public override void Invalidate(InvalidationCause cause)
        {
            if (m_Owner != null)
                m_Owner.Invalidate(cause);
        }

        public VFXBlockModel(VFXBlock desc)
        {
            m_BlockDesc = desc;
            InitProperties();
        }

        public VFXBlock Desc
        {
            get { return m_BlockDesc; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (m_BlockDesc == null || !m_BlockDesc.m_Hash.Equals(value.m_Hash)) // block desc has changed
                {
                    m_BlockDesc = value;
                    InitProperties();
                    Invalidate(InvalidationCause.kModelChanged);
                }
            }
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        public VFXProperty[] Properties { get { return m_Properties; } }

        private void InitProperties()
        {
            m_Properties = VFXPropertyConverter.CreateProperties(m_BlockDesc.m_Params); // TMP Refactor
            InitSlots(m_Properties);
        }

        private VFXBlock m_BlockDesc;
        private VFXProperty[] m_Properties; // TODO Refactor : that goes in VFXBlock
    }
}
