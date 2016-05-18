using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using VFXBLKLibrary = UnityEditor.VFXBlockLibrary;

namespace UnityEditor.Experimental.VFX
{
    public class VFXBlockLibrary
    {
        public VFXBlockLibrary()
        {
            m_Blocks = new Dictionary<string,VFXBlockDesc>();
            m_DataBlocks = new Dictionary<string,VFXDataBlockDesc>();
        }

        public void Load()
        {
            Clear();
            
            LoadFromAssemblies();
            LoadFromBLK();

            LoadDataBlocks();
        }

        public void Clear()
        {
            m_Blocks.Clear();
            m_DataBlocks.Clear();
        }

        public VFXBlockDesc GetBlock(string id)                 { return m_Blocks[id]; }
        public IEnumerable<VFXBlockDesc> GetBlocks()            { return m_Blocks.Values; }

        public VFXDataBlockDesc GetDataBlock(string id)         { return m_DataBlocks[id]; }
        public IEnumerable<VFXDataBlockDesc> GetDataBlocks()    { return m_DataBlocks.Values; }

        private void LoadFromAssemblies()
        {
            // Search for derived type of VFXBlockType in assemblies
            var blockTypes = FindConcreteSubclasses<VFXBlockType>();
            foreach (var blockType in blockTypes)
            {
                try
                {
                    VFXBlockType instance = (VFXBlockType)blockType.Assembly.CreateInstance(blockType.FullName);
                    VFXBlockDesc desc = new VFXBlockDesc(instance);
                    m_Blocks.Add(desc.ID,desc);
                    //Debug.Log("SLOTHASH FOR " + desc.ID + ": " + desc.SlotHash);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading block desc from type " + blockType.FullName + ": " + e.Message);
                }
            }
        }

        private void LoadFromBLK()
        {
            string[] guids = AssetDatabase.FindAssets("t:VFXBlockLibrary");
            VFXBLKLibrary[] blkLibraries = new VFXBLKLibrary[guids.Length];
            for (int i = 0; i < guids.Length; ++i)
            {
                blkLibraries[i] = AssetDatabase.LoadAssetAtPath<VFXBLKLibrary>(AssetDatabase.GUIDToAssetPath(guids[i]));
                for (int j = 0; j < blkLibraries[i].GetNbBlocks(); ++j)
                {
                    VFXBlock block = blkLibraries[i].GetBlock(j);
                    try
                    {
                        VFXBlockDesc desc = new VFXBlockDesc(block);
                        m_Blocks.Add(desc.ID, desc);
                        //Debug.Log("SLOTHASH FOR " + desc.ID + ": " + desc.SlotHash);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error while loading block desc from legacy block " + block.m_Name + ": " + e.Message);
                    }
                }
            }
        }

        private void LoadDataBlocks()
        {
            // Search for semantic types in assemblies
            var semanticTypes = FindConcreteSubclasses<VFXPropertyTypeSemantics>();
            foreach (var semanticType in semanticTypes)
            {
                try
                {
                    var descMethod = semanticType.GetMethod("Description", BindingFlags.Public | BindingFlags.Static);
                    if (descMethod != null)
                    {
                        VFXPropertyTypeSemantics.Desc desc = (VFXPropertyTypeSemantics.Desc)descMethod.Invoke(null, null);
                        VFXPropertyTypeSemantics semantics = (VFXPropertyTypeSemantics)semanticType.Assembly.CreateInstance(semanticType.FullName);

                        var dataBlockDesc = new VFXDataBlockDesc(new VFXProperty(semantics,desc.m_Name),desc.m_Icon,desc.m_Category);

                        m_DataBlocks.Add(semanticType.FullName, dataBlockDesc);
                        //Debug.Log("DATABLOCK "+semanticType.Name+" "+dataBlockDesc.Name+" "+dataBlockDesc.Icon+" "+dataBlockDesc.Category);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading data block desc from semantics " + semanticType.FullName + ": " + e.Message);
                }
            }
        }

        private IEnumerable<Type> FindConcreteSubclasses<T>()
        {
            List<Type> types = new List<Type>();
            foreach (var domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = null;
                try
                {
                    assemblyTypes = domainAssembly.GetTypes();
                }
                catch(Exception)
                {
                    Debug.Log("Cannot access assembly: "+domainAssembly);
                    assemblyTypes = null;
                }
                if (assemblyTypes != null)
                    foreach (var assemblyType in assemblyTypes)
                        if (assemblyType.IsSubclassOf(typeof(T)) && !assemblyType.IsAbstract)
                            types.Add (assemblyType);
            }
            return types;
        }

        private Dictionary<string,VFXBlockDesc> m_Blocks;
        private Dictionary<string,VFXDataBlockDesc> m_DataBlocks;
    }
}