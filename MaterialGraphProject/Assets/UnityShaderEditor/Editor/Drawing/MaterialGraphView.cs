using System;
using System.Reflection;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.RMGUI;
using Edge = RMGUI.GraphView.Edge;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing
{
    [StyleSheet("Assets/UnityShaderEditor/Editor/Drawing/Styles/MaterialGraph.uss")]
    public class MaterialGraphView : SerializableGraphView
    {
        public MaterialGraphView()
        {
            AddManipulator(new ContextualMenu(DoContextMenu));

            dataMapper[typeof(MaterialNodeDrawData)] = typeof(MaterialNodeDrawer);
            dataMapper[typeof(AnchorDrawData)] = typeof(NodeAnchor);
            dataMapper[typeof(EdgePresenter)] = typeof(Edge);
        }

        public virtual bool CanAddToNodeMenu(Type type)
        {
            return true;
        }

        protected EventPropagation DoContextMenu(Event evt, Object customData)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0 && CanAddToNodeMenu(type))
                    {
                        gm.AddItem(new GUIContent(attrs[0].m_Title), false, AddNode, new AddNodeCreationObject(type, evt.mousePosition));
                    }
                }
            }

            //gm.AddSeparator("");
            // gm.AddItem(new GUIContent("Convert To/SubGraph"), true, ConvertSelectionToSubGraph);
            gm.ShowAsContext();
            return EventPropagation.Stop;
        }

        private class AddNodeCreationObject : object
        {
            public Vector2 m_Pos;
            public readonly Type m_Type;

            public AddNodeCreationObject(Type t, Vector2 p)
            {
                m_Type = t;
                m_Pos = p;
            }
        };

        private void AddNode(object obj)
        {
            var posObj = obj as AddNodeCreationObject;
            if (posObj == null)
                return;

            INode node;
            try
            {
                node = Activator.CreateInstance(posObj.m_Type) as INode;
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("Could not construct instance of: {0} - {1}", posObj.m_Type, e);
                return;
            }

            if (node == null)
                return;
            var drawstate = node.drawState;
            drawstate.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, 0, 0);
            node.drawState = drawstate;

            var graphDataSource = GetPresenter<AbstractGraphDataSource>();
            graphDataSource.AddNode(node);
        }

        /*
        public EventPropagation Export()
        {
            var path = EditorUtility.SaveFilePanelInProject("Export shader to file...", "shader.shader", "shader", "Enter file name");

            var ds = dataSource as AbstractGraphDataSource;
            if (ds != null && !string.IsNullOrEmpty(path))
            {
                ExportShader(ds.graphAsset as MaterialGraphAsset, path);
            }
            else
                EditorUtility.DisplayDialog("Export Shader Error", "Cannot export shader", "Ok");

            return EventPropagation.Stop;
        }

        public static Shader ExportShader(MaterialGraphAsset graphAsset, string path)
        {
            if (graphAsset == null)
                return null;

            var materialGraph = graphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph;
            if (materialGraph == null)
                return null;

            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = materialGraph.masterNode.GetShader(GenerationMode.ForReals, out configuredTextures);
            File.WriteAllText(path, shaderString);
            AssetDatabase.Refresh(); // Investigate if this is optimal

            var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader)) as Shader;
            if (shader == null)
                return null;

            var shaderImporter = AssetImporter.GetAtPath(path) as ShaderImporter;
            if (shaderImporter == null)
                return null;

            var textureNames = new List<string>();
            var textures = new List<Texture>();
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.Modifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetDefaultTextures(textureNames.ToArray(), textures.ToArray());

            textureNames.Clear();
            textures.Clear();
            foreach (var textureInfo in configuredTextures.Where(x => x.modifiable == TexturePropertyChunk.ModifiableState.NonModifiable))
            {
                var texture = EditorUtility.InstanceIDToObject(textureInfo.textureId) as Texture;
                if (texture == null)
                    continue;
                textureNames.Add(textureInfo.name);
                textures.Add(texture);
            }
            shaderImporter.SetNonModifiableTextures(textureNames.ToArray(), textures.ToArray());

            shaderImporter.SaveAndReimport();

            return shaderImporter.GetShader();
        }*/
    }
}
