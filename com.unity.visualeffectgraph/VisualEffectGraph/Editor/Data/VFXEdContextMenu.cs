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
        internal static GenericMenu CanvasMenu(VFXEdCanvas canvas, Vector2 canvasClickPosition, VFXEdDataSource source ) {

            GenericMenu output = new GenericMenu();

            output.AddItem(new GUIContent("New Node/Initialize"), false, source.AddEmptyNode, new VFXEdSpawnData(canvas, canvasClickPosition, "", VFXEdContext.Initialize ,SpawnType.Node));
            output.AddItem(new GUIContent("New Node/Update"), false, source.AddEmptyNode, new VFXEdSpawnData(canvas, canvasClickPosition, "", VFXEdContext.Update ,SpawnType.Node));
            output.AddItem(new GUIContent("New Node/Output"), false, source.AddEmptyNode, new VFXEdSpawnData(canvas, canvasClickPosition, "", VFXEdContext.Output ,SpawnType.Node));
            output.AddSeparator("New Node/");
            output.AddItem(new GUIContent("New Node/Data Node"), false, source.AddEmptyNode, new VFXEdSpawnData(canvas, canvasClickPosition, "", VFXEdContext.None ,SpawnType.DataNode));
            

            return output;
        }

        internal static GenericMenu NodeBlockMenu(VFXEdCanvas canvas, VFXEdNode node, Vector2 canvasClickPosition, VFXEdDataSource source ) {

            GenericMenu output = new GenericMenu();
            ReadOnlyCollection<VFXBlock> blocks = VFXEditor.BlockLibrary.GetBlocks();

            foreach (VFXBlock block in blocks)
            {
                output.AddItem(new GUIContent(block.m_Category + block.m_Name), false, node.MenuAddNodeBlock, new VFXEdSpawnData(canvas, canvasClickPosition, block.m_Name, VFXEdContext.None, SpawnType.NodeBlock));
            }

            return output;
        }

    }
}
