using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public class VFXAssetModel : VFXElementModelTyped<VFXElementModel, VFXSystemModel>
    {
        public VFXAssetModel()
        {
            RemovePreviousVFXs();

            gameObject = new GameObject("VFX");
            //gameObject.hideFlags = HideFlags.DontSaveInEditor;
            component = gameObject.AddComponent<VFXComponent>();
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

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(gameObject); 
            for (int i = 0; i < GetNbChildren(); ++i)
                GetChild(i).Dispose();
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
            if (m_NeedsCheck)
            {
                VFXEditor.Log("\n**** VFXAsset is dirty ****");
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXEditor.Log("Recompile system " + i + " if needed ");
                    if (!GetChild(i).RecompileIfNeeded())
                        VFXEditor.Log("No need to recompile");
                }

                // tmp
                for (int i = 0; i < GetNbChildren(); ++i)
                {
                    VFXSystemRuntimeData rtData = GetChild(i).RtData;
                    if (rtData != null)
                    {
                        component.simulationShader = rtData.SimulationShader;
                        component.material = rtData.m_Material;
                        component.outputType = (uint)m_OutputType;
                        component.maxNb = GetChild(i).MaxNb;
                        component.spawnRate = GetChild(i).SpawnRate;
                    }
                }

                m_NeedsCheck = false;
            }

            if (m_ReloadUniforms)
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
        }

        // tmp
        public void UpdateComponentMaxNb(uint MaxNb)
        {
            component.maxNb = MaxNb;
        }

        // tmp
        public void UpdateComponentSpawnRate(float SpawnRate)
        {
            component.spawnRate = SpawnRate;
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

        public int OutputType
        {
            get { return m_OutputType; }
            set
            {
                if (m_OutputType != value)
                {
                    m_OutputType = value;
                    for (int i = 0; i < GetNbChildren(); ++i)
                        GetChild(i).Invalidate(InvalidationCause.kModelChanged);
                }
            }
        }

        public void SwitchOutputType()
        {
            int outputType = OutputType;
            if (++outputType > 2)
                outputType = 0;
            OutputType = outputType;               
        }

        private bool m_NeedsCheck = false;
        private bool m_ReloadUniforms = false;
        private bool m_PhaseShift = false; // Used to remove sampling discretization issue
        private int m_OutputType = 0; // 0: point rendering / 1: billboard rendering

        private VFXComponent component;
        private GameObject gameObject;
    }

    public class VFXSystemModel : VFXElementModelTyped<VFXAssetModel, VFXContextModel>
    {


        public void Dispose()
        {
            if (rtData != null)
            {
                rtData.DisposeBuffers();
                UnityEngine.Object.DestroyImmediate(rtData.m_Material); 
            }
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            if (!base.CanAddChild(element, index))
                return false;

            VFXContextModel.Type contextType = (element as VFXContextModel).GetContextType();
            if (contextType == VFXContextModel.Type.kTypeNone)
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
                Dispose();
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
                {
                    rtData.DisposeBuffers();
                    UnityEngine.Object.DestroyImmediate(rtData.m_Material); 
                }
                rtData = VFXModelCompiler.CompileSystem(this);
                m_Dirty = false;
                return true;
            }

            return false;
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
                    if (rtData != null)
                        GetOwner().UpdateComponentMaxNb(m_MaxNb);
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
                    if (rtData != null)
                        GetOwner().UpdateComponentSpawnRate(m_SpawnRate);
                }
            }
        }
    }


    public class VFXContextModel : VFXElementModelTyped<VFXSystemModel, VFXBlockModel>
    {
        public enum Type
        {
            kTypeNone,
            kTypeInit,
            kTypeUpdate,
            kTypeOutput,
        };

        public const uint s_NbTypes = (uint)Type.kTypeOutput + 1;

        public VFXContextModel(Type type)
        {
            m_Type = type;
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return base.CanAddChild(element, index) && m_Type != Type.kTypeNone;
            // TODO Check if the block is compatible with the context
        }

        public override void Invalidate(InvalidationCause cause)
        {
            if (m_Owner != null && m_Type != Type.kTypeNone)
                m_Owner.Invalidate(cause);
        }

        public Type GetContextType()
        {
            return m_Type;
        }

        private Type m_Type;
    }

    public class VFXBlockModel : VFXElementModelTyped<VFXContextModel, VFXElementModel>
    {
        public override void Invalidate(InvalidationCause cause)
        {
            if (m_Owner != null)
                m_Owner.Invalidate(cause);
        }

        public VFXBlockModel(VFXBlock desc)
        {
            m_BlockDesc = desc;
            int nbParams = desc.m_Params.Length;
            m_ParamValues = new VFXParamValue[nbParams];
            for (int i = 0; i < nbParams; ++i)
                m_ParamValues[i] = VFXParamValue.Create(desc.m_Params[i].m_Type); // Create default bindings
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
                    Invalidate(InvalidationCause.kModelChanged);
                }
            }
        }

        public VFXParamValue GetParamValue(int index)
        {
            return m_ParamValues[index];
        }

        public override bool CanAddChild(VFXElementModel element, int index)
        {
            return false; // Nothing can be attached to Blocks !
        }

        public void BindParam(VFXParamValue param,int index,bool reentrant = false)
        {
            if (index < 0 || index >= m_BlockDesc.m_Params.Length || param.ValueType != m_BlockDesc.m_Params[index].m_Type)
                throw new ArgumentException();

            if (!reentrant)
            {
                if (m_ParamValues[index] != null)
                    m_ParamValues[index].Unbind(this, index, true);
                param.Bind(this, index, true);
            }

            m_ParamValues[index] = param;
            Invalidate(InvalidationCause.kModelChanged);
        }

        public void UnbindParam(int index, bool reentrant = false)
        {
            if (index < 0 || index >= m_BlockDesc.m_Params.Length)
                throw new ArgumentException();

            if (!reentrant && m_ParamValues[index] != null)
                m_ParamValues[index].Unbind(this, index, true);

            m_ParamValues[index] = VFXParamValue.Create(m_BlockDesc.m_Params[index].m_Type);
            Invalidate(InvalidationCause.kModelChanged);
        }

        private VFXBlock m_BlockDesc;
        private VFXParamValue[] m_ParamValues;
    }
}
