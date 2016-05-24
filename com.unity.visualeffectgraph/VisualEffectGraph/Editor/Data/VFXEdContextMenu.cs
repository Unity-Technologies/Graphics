using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdContextMenu
    {
        internal static void CanvasMenu(VFXEdCanvas canvas, Vector2 canvasClickPosition, VFXEdDataSource source ) {

            GenericMenu output = new GenericMenu();

            // TODO : Add Again Triggers & Events
            
            output.AddItem(new GUIContent("New Node/Event/On Start"), false, source.SpawnNode, new VFXEdEventNodeSpawner(source, canvas, canvasClickPosition,"Start"));
            output.AddItem(new GUIContent("New Node/Event/On Stop"), false, source.SpawnNode, new VFXEdEventNodeSpawner(source, canvas, canvasClickPosition,"Stop"));
            output.AddItem(new GUIContent("New Node/Event/On Pause"), false, source.SpawnNode, new VFXEdEventNodeSpawner(source, canvas, canvasClickPosition,"Pause"));

            output.AddItem(new GUIContent("New Node/Trigger"), false, source.SpawnNode, new VFXEdTriggerNodeSpawner(source, canvas, canvasClickPosition));
            output.AddSeparator("New Node/");

            // Context Nodes
            foreach (var desc in VFXEditor.ContextLibrary.GetContexts())
            {
                output.AddItem(new GUIContent("New Node/" + VFXContextDesc.GetTypeName( desc.m_Type) + "/" + desc.Name), false, source.SpawnNode, new VFXEdContextNodeSpawner(source, canvas, canvasClickPosition, desc));
            }

            // Data Nodes
            var blocks = new List<VFXDataBlockDesc>(VFXEditor.BlockLibrary.GetDataBlocks());
            blocks.Sort((blockA, blockB) =>
            {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            foreach (var block in blocks)
            {
                output.AddItem(new GUIContent("Parameters/"+block.Category+"/"+block.Name), false, source.SpawnNode, new VFXEdDataNodeSpawner(source, canvas, canvasClickPosition, block));
            }
            output.AddItem(new GUIContent("Parameters/Empty Data Node"), false, source.SpawnNode, new VFXEdDataNodeSpawner(source, canvas, canvasClickPosition));

            /*
            // Templates
            output.AddSeparator("");
            foreach(VFXEdSpawnTemplate t in VFXEditor.SpawnTemplates.Templates)
            {
                output.AddItem(new GUIContent("Templates/" + t.Path), false, VFXEditor.SpawnTemplates.SpawnFromMenu, new VFXEdTemplateSpawner(t.Path, source, canvas, canvasClickPosition));
            }
            */

            output.ShowAsContext();
        }

    }
}
