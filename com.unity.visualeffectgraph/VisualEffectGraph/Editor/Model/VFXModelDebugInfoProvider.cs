using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public static class VFXModelDebugInfoProvider
    {
        // NodeBlock Info
        public static List<string> GetInfo(List<string> info, VFXBlockModel Model, bool bVerbose)
        {
            info.Add("NODEBLOCK : " + Model.Desc.Name);
            info.Add("");
            info.Add("Flags: " + Model.Desc.Flags);
            info.Add("Category : " + Model.Desc.Category);

            if(bVerbose)
            {
                if(Model.Desc.Attributes != null)
                {
                    info.Add("");
                    info.Add("Attributes (" + Model.Desc.Attributes.Length + "):");

                    for(int i = 0; i< Model.Desc.Attributes.Length; i++)
                    {
                        info.Add("     * ("+ (Model.Desc.Attributes[i].m_Writable ? "rw":"r" ) + ") " + Model.Desc.Attributes[i].m_Name + " : " + Model.Desc.Attributes[i].m_Type);
                    }
                }
                if(Model.Desc.Properties != null)
                {
                    info.Add("");
                    info.Add("Parameters (" + Model.Desc.Properties.Length + "):");
                
                    for(int i = 0; i< Model.Desc.Properties.Length; i++)
                    {
                        info.Add("     * " + Model.Desc.Properties[i].m_Name + " : " + Model.Desc.Properties[i].m_Type + " (" + Model.Desc.Properties[i].m_Type.ValueType + ")");
                    }
                }
                info.Add("");
                info.Add("Source Code : ");
                string[] source =  Model.Desc.Source.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < source.Length; i++)
                {
                    info.Add("          " + source[i]);
                }
                info.Add("");
            }

            info.Add("---");
            info = GetInfo(info , Model.GetOwner(), false);

            return info;
        }

        // Context Model Info
        public static List<string> GetInfo(List<string> info, VFXContextModel Model, bool bVerbose)
        {
            info.Add("CONTEXT : " + Model.GetContextType().ToString());
            info.Add("");
            info.Add("Context Desc: " + Model.Desc.ToString());
            info.Add("Context Nodeblock : " + (Model.Desc.ShowBlock ? Model.Desc.Name : "Absent"));

            if(bVerbose)
            {
                if(Model.Desc.ShowBlock)
                {
                    if(Model.Desc.m_Properties != null)
                    {
                        info.Add("");
                        info.Add("Parameters: ");
                        for(int i = 0; i < Model.Desc.m_Properties.Length; i++)
                        {
                            info.Add("* " + Model.Desc.m_Properties[i].m_Name + " : " + Model.Desc.m_Properties[i].m_Type +" ("+  Model.Desc.m_Properties[i].m_Type.ValueType+")");
                        }
                    }
                }
                info.Add("");
                info.Add(Model.GetNbChildren() + " Nodeblocks in context");

                for(int i = 0; i < Model.GetNbChildren(); i++)
                {
                    info.Add("     * " + Model.GetChild(i).Desc.Name);
                }
                
            }

            info.Add("---");
            info = GetInfo(info , Model.GetOwner(), false);

            return info;
        }

        public static List<string> GetInfo(List<string> info, VFXSystemModel Model, bool bVerbose)
        {
            info.Add("SYSTEM : #" + Model.Id);
            info.Add("");
            info.Add("Allocation Count : " + Model.MaxNb);
            info.Add("Render Priority : " + Model.OrderPriority);
            info.Add("Render mode :" + Model.BlendingMode);

            if(bVerbose)
            {
                info.Add("");
                info.Add("Advanced System Information...");
                info.Add("Work in progress...");
                info.Add("");
            }

            info.Add("---");
            info = GetInfo(info , Model.GetOwner(), false);

            return info;
        }

        // Asset
        public static List<string> GetInfo(List<string> info, VFXAssetModel Model, bool bVerbose)
        {
            info.Add("VFX ASSET");
            info.Add("");
            info.Add("Sampling Correction : " + (Model.PhaseShift ? "Enabled" : "Disabled"));
            info.Add("System Count : " + Model.GetNbChildren());

            if(bVerbose)
            {
                info.Add("---");
                info.Add("Advanced Asset Information...");
                info.Add("Work in progress...");
                info.Add("");
            }

            return info;
        }


    }
}
