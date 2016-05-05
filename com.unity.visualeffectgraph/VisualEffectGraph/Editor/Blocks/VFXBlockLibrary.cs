using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public void Load()
        {
            Clear();
            LoadFromAssemblies();
            LoadFromBLK();
        }

        public void Clear()
        {
            m_Blocks.Clear();
        }

        public VFXBlockDesc GetBlock(string id)
        {
            return m_Blocks[id];
        }

        public IEnumerable<VFXBlockDesc> GetBlocks()
        {
            return m_Blocks.Values;
        }

        private void LoadFromAssemblies()
        {
            // Search for derived type of VFXBlockType in assemblies
            Type[] blockTypes = ( from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                                from assemblyType in domainAssembly.GetTypes()
                                where (assemblyType.IsSubclassOf(typeof(VFXBlockType)) && !assemblyType.IsAbstract)
                                select assemblyType).ToArray();

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

        private Dictionary<string,VFXBlockDesc> m_Blocks;
    }
}