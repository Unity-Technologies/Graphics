using System;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdContextMenu
    {
        internal static void CanvasMenu(VFXEdCanvas canvas, Vector2 canvasClickPosition, VFXEdDataSource source ) {

            GenericMenu output = new GenericMenu();

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
            foreach(VFXDataBlock block in VFXEditor.DataBlockLibrary.blocks)
            {
                output.AddItem(new GUIContent("Parameters/"+block.category+"/"+block.name), false, source.SpawnNode, new VFXEdDataNodeSpawner(source, canvas, canvasClickPosition, block));
            }
            output.AddItem(new GUIContent("Parameters/Empty Data Node"), false, source.SpawnNode, new VFXEdDataNodeSpawner(source, canvas, canvasClickPosition));

            // Templates
            output.AddSeparator("");
            foreach(VFXEdSpawnTemplate t in VFXEditor.SpawnTemplates.Templates)
            {
                output.AddItem(new GUIContent("Templates/" + t.Path), false, VFXEditor.SpawnTemplates.SpawnFromMenu, new VFXEdTemplateSpawner(t.Path, source, canvas, canvasClickPosition));
            }

            output.ShowAsContext();
        }

    }
}
