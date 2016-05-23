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
        public enum InfoFlag
        {
            kNone           = 0,
            kVerbose        = 1 << 0,
            kFullyVerbose   = 1 << 1,
            kRecursive      = 1 << 2,
            kMemoryInfo     = 1 << 3,
            kDefault        = kVerbose | kRecursive
        }

        public static bool HasFlag(InfoFlag input, InfoFlag flag)
        {
            return (input & flag) == flag;
        }

        // NodeBlock Info
        public static List<string> GetInfo(List<string> info, VFXBlockModel Model, InfoFlag flags)
        {
            info.Add("NODEBLOCK : " + Model.Desc.Name);
            info.Add("");
            info.Add("Flags: " + Model.Desc.Flags);
            info.Add("Category : " + Model.Desc.Category);

            if(HasFlag(flags, InfoFlag.kVerbose))
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
                        info.Add("     * " + Model.Desc.Properties[i].m_Name + " : " + Model.GetSlot(i).Value + " (" + Model.Desc.Properties[i].m_Type.GetType().Name + "/" + Model.Desc.Properties[i].m_Type.ValueType + ")");
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

            if(HasFlag(flags, InfoFlag.kRecursive))
            {
                info.Add("---");
                if (!HasFlag(flags, InfoFlag.kFullyVerbose) && HasFlag(flags, InfoFlag.kVerbose))
                    flags = flags &~ InfoFlag.kVerbose;
                info = GetInfo(info , Model.GetOwner(), flags);
            }

            return info;
        }

        // Context Model Info
        public static List<string> GetInfo(List<string> info, VFXContextModel Model, InfoFlag flags)
        {
            info.Add("CONTEXT : " + Model.GetContextType().ToString());
            info.Add("");
            info.Add("Context Desc: " + Model.Desc.GetType().Name);
            info.Add("Context Nodeblock : " + (Model.Desc.ShowBlock ? Model.Desc.Name : "Absent"));

            if(HasFlag(flags, InfoFlag.kVerbose))
            {
                if(Model.Desc.ShowBlock)
                {
                    if(Model.Desc.m_Properties != null)
                    {
                        info.Add("");
                        info.Add("Parameters: ");
                        for(int i = 0; i < Model.Desc.m_Properties.Length; i++)
                        {
                            info.Add("     * " + Model.Desc.m_Properties[i].m_Name + " : " + Model.GetSlot(i).Value.Reduce() + " (" + Model.Desc.m_Properties[i].m_Type.GetType().Name + "/" + Model.Desc.m_Properties[i].m_Type.ValueType + ")");
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

            if(HasFlag(flags, InfoFlag.kRecursive))
            {
                info.Add("---");
                if (!HasFlag(flags, InfoFlag.kFullyVerbose) && HasFlag(flags, InfoFlag.kVerbose))
                    flags = flags &~ InfoFlag.kVerbose;
                info = GetInfo(info , Model.GetOwner(), flags);
            }

            return info;
        }

        // System Model
        public static List<string> GetInfo(List<string> info, VFXSystemModel Model, InfoFlag flags)
        {
            info.Add("SYSTEM : #" + Model.Id);
            info.Add("");
            info.Add("Allocation Count : " + Model.MaxNb);
            info.Add("Render Priority : " + Model.OrderPriority);
            info.Add("Render mode :" + Model.BlendingMode);

            if(HasFlag(flags, InfoFlag.kVerbose))
            {
                info.Add("");
                info.Add("Advanced System Information...");
                info.Add("Work in progress...");
                info.Add("");
            }

            return info;
        }

        // Asset Model
        public static List<string> GetInfo(List<string> info, VFXSystemsModel Model, InfoFlag flags)
        {
            int childcount = Model.GetNbChildren();

            info.Add("VFX ASSET");
            info.Add("");
            info.Add("Sampling Correction : " + (Model.PhaseShift ? "Enabled" : "Disabled"));
            info.Add("System Count : " + childcount);

            if(HasFlag(flags, InfoFlag.kVerbose))
            {
                info.Add("");
                info.Add("Advanced Asset Information...");
                info.Add("(Work in progress)");
                info.Add("");
            }

            if(HasFlag(flags, InfoFlag.kRecursive) && childcount > 0)
            {
                if (!HasFlag(flags, InfoFlag.kFullyVerbose) && HasFlag(flags, InfoFlag.kVerbose))
                    flags = flags &~ InfoFlag.kVerbose;

                for(int i = 0 ; i < childcount; i++)
                {
                    info.Add("---");
                    info = GetInfo(info , Model.GetChild(i), flags);
                }

            }

            return info;
        }

        // VFXPropertySlot
        public static List<string> GetInfo(List<string> info, VFXPropertySlot Slot, InfoFlag flags )
        {
            if(Slot.Value != null)
                info.Add(Slot.Name + " (" + Slot.Value.ValueType + ") : " + Slot.Value.Reduce());
            else
                info.Add(Slot.Name + " : (null)");
            
            int nbChildren = Slot.GetNbChildren();

            if(HasFlag(flags, InfoFlag.kRecursive) && nbChildren > 0)
            {
                for(int i = 0 ; i < nbChildren ; i++)
                {
                    info = GetInfo(info, Slot.GetChild(i), flags);
                }
            }
            return info;

        }

        // VFXDataNodeBlock
        internal static List<string> GetInfo(List<string> info, VFXEdDataNodeBlock Block, InfoFlag flags )
        {
            info.Add("DATA NODEBLOCK : " + Block.m_exposedName + " (" + Block.LibraryName + ")");
            info.Add("");
            info.Add("Slot : " + Block.Slot.Name + " (" + Block.Slot.ValueType+")");
            
            if(HasFlag(flags, InfoFlag.kVerbose))
            {
                info.Add("");
                info.Add("PropertySlots : ");
                info = GetInfo(info, Block.Slot, InfoFlag.kRecursive);
            }
            return info;
        }


    }
}
