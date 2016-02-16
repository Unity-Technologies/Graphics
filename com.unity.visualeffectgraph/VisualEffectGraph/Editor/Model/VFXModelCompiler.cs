using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace UnityEditor.Experimental
{
    public static class VFXModelCompiler
    {
        // TODO atm it only validates the system
        public static void CompileSystem(VFXSystemModel system)
        {
            List<VFXBlockModel> initBlocks = new List<VFXBlockModel>();
            List<VFXBlockModel> updateBlocks = new List<VFXBlockModel>();

            // Collapses the contexts into one big init and update
            for (int i = 0; i < system.GetNbChildren(); ++i)
            {
                VFXContextModel context = system.GetChild(i);

                List<VFXBlockModel> currentList = null; ;
                switch (context.GetContextType())
                {
                    case VFXContextModel.Type.kTypeInit: currentList = initBlocks; break;
                    case VFXContextModel.Type.kTypeUpdate: currentList = updateBlocks; break;
                }

                if (currentList == null)
                    continue;

                for (int j = 0; j < context.GetNbChildren(); ++j)
                    currentList.Add(context.GetChild(j));
            }

            if (initBlocks.Count == 0 && updateBlocks.Count == 0)
            {
                // Invalid system, not compiled
                VFXEditor.Log("System is invalid: Empty");
                return;
            }

            HashSet<VFXParamValue> initUniforms = CollectUniforms(initBlocks);
            HashSet<VFXParamValue> updateUniforms = CollectUniforms(updateBlocks);

            // Collect the intersection between init and update uniforms
            HashSet<VFXParamValue> globalUniforms = new HashSet<VFXParamValue>();
            
            foreach (VFXParamValue uniform in initUniforms)
                if (updateUniforms.Contains(uniform))
                    globalUniforms.Add(uniform);

            foreach (VFXParamValue uniform in globalUniforms)
            {
                initUniforms.Remove(uniform);
                updateUniforms.Remove(uniform);
            }

            // Log result
            VFXEditor.Log("Number init blocks: " + initBlocks.Count);
            VFXEditor.Log("Number update blocks: " + updateBlocks.Count);
            VFXEditor.Log("Number global uniforms: " + globalUniforms.Count);
            VFXEditor.Log("Number init uniforms: " + initUniforms.Count);
            VFXEditor.Log("Number update uniforms: " + updateUniforms.Count);
        }

        public static HashSet<VFXParamValue> CollectUniforms(List<VFXBlockModel> blocks)
        {
            HashSet<VFXParamValue> uniforms = new HashSet<VFXParamValue>();

            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.m_Params.Length; ++i)
                    uniforms.Add(block.GetParamValue(i));

            return uniforms;
        }
    }
}