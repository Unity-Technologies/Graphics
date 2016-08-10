using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental;
using UnityEditor.MaterialGraph;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.Graphing.Drawing
{
    public class GraphEditWindow : AbstractGraphEditWindow<IGraphAsset>
    {
        [MenuItem("Window/Graph Editor")]
        public static void OpenMenu()
        {
            GetWindow<GraphEditWindow>();
        }
    }

    public abstract class AbstractGraphEditWindow<T> : EditorWindow, ISerializationCallbackReceiver where T : class, IGraphAsset
    {
        public RenderTexture rt;

        [NonSerialized]
        private T m_LastSelection;

        [SerializeField]
        private ScriptableObject m_LastSelectedGraphSerialized;

        [NonSerialized]
        private Canvas2D m_Canvas;
        [NonSerialized]
        private EditorWindow m_HostWindow;
        [NonSerialized]
        private GraphDataSource m_DataSource;

        private bool shouldRepaint
        {
            get
            {
                return m_LastSelection != null && m_LastSelection.shouldRepaint;
            }
        }

        void Update()
        {
            if (shouldRepaint)
                Repaint();
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject == null || !EditorUtility.IsPersistent(Selection.activeObject))
                return;

            if (Selection.activeObject is ScriptableObject)
            {
                var selection = Selection.activeObject as T;
                if (selection != m_LastSelection)
                {
                    var graph = selection.graph;
                    graph.OnEnable();
                    graph.ValidateGraph();
                    m_LastSelection = selection;
                    Rebuild();
                    Repaint();
                }
            }
        }

        private void InitializeCanvas()
        {
            if (m_Canvas == null)
            {
                m_DataSource = new GraphDataSource();
                m_Canvas = new Canvas2D(this, m_HostWindow, m_DataSource);

                // draggable manipulator allows to move the canvas around. Note that individual elements can have the draggable manipulator on themselves
                m_Canvas.AddManipulator(new Draggable(2, EventModifiers.None));
                m_Canvas.AddManipulator(new Draggable(0, EventModifiers.Alt));

                // make the canvas zoomable
                m_Canvas.AddManipulator(new Zoomable());

                // allow framing the selection when hitting "F" (frame) or "A" (all). Basically shows how to trap a key and work with the canvas selection
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.All));
                m_Canvas.AddManipulator(new Frame(Frame.FrameType.Selection));

                // The following manipulator show how to work with canvas2d overlay and background rendering
                m_Canvas.AddManipulator(new RectangleSelect());
                m_Canvas.AddManipulator(new ScreenSpaceGrid());
                m_Canvas.AddManipulator(new ContextualMenu(DoContextMenu));

                m_Canvas.AddManipulator(new DeleteSelected(m_DataSource.DeleteElements));
                m_Canvas.AddManipulator(new CopySelected());
            }

            Rebuild();
        }

        private class AddNodeCreationObject : object
        {
            public Vector2 m_Pos;
            public readonly Type m_Type;

            public AddNodeCreationObject(Type t, Vector2 p) { m_Type = t; m_Pos = p; }
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
            drawstate.position = new Rect(posObj.m_Pos.x, posObj.m_Pos.y, drawstate.position.width, drawstate.position.height);
            node.drawState = drawstate;
            m_DataSource.Addnode(node);
            Rebuild();
            Repaint();
        }

        public virtual bool CanAddToNodeMenu(Type type) { return true; }
        protected bool DoContextMenu(Event @event, Canvas2D parent, Object customData)
        {
            var gm = new GenericMenu();
            foreach (Type type in Assembly.GetAssembly(typeof(AbstractMaterialNode)).GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(AbstractMaterialNode))))
                {
                    var attrs = type.GetCustomAttributes(typeof(TitleAttribute), false) as TitleAttribute[];
                    if (attrs != null && attrs.Length > 0 && CanAddToNodeMenu(type))
                    {
                        gm.AddItem(new GUIContent(attrs[0].m_Title), false, AddNode, new AddNodeCreationObject(type, parent.MouseToCanvas(@event.mousePosition)));
                    }
                }
            }

            //gm.AddSeparator("");
            // gm.AddItem(new GUIContent("Convert To/SubGraph"), true, ConvertSelectionToSubGraph);
            gm.ShowAsContext();
            return true;
        }

        private void ConvertSelectionToSubGraph()
        {
            if (m_Canvas.dataSource == null)
                return;

            var dataSource = m_Canvas.dataSource as GraphDataSource;
            if (dataSource == null)
                return;

            var asset = dataSource.graphAsset;
            if (asset == null)
                return;

            var targetGraph = asset.graph;
            if (targetGraph == null)
                return;

            if (!m_Canvas.selection.Any())
                return;

            var serialzied = CopySelected.SerializeSelectedElements(m_Canvas);
            var deserialized = CopySelected.DeserializeSelectedElements(serialzied);
            if (deserialized == null)
                return;

            string path = EditorUtility.SaveFilePanelInProject("Save subgraph", "New SubGraph", "ShaderSubGraph", "");
            path = path.Replace(Application.dataPath, "Assets");
            if (path.Length == 0)
                return;

            var graphAsset = CreateInstance<MaterialSubGraphAsset>();
            graphAsset.name = Path.GetFileName(path);
            graphAsset.PostCreate();

            var graph = graphAsset.subGraph;
            if (graphAsset.graph == null)
                return;

            var nodeGuidMap = new Dictionary<Guid, Guid>();
            foreach (var node in deserialized.GetNodes<INode>())
            {
                var oldGuid = node.guid;
                var newGuid = node.RewriteGuid();
                nodeGuidMap[oldGuid] = newGuid;
                graph.AddNode(node);
            }

            // remap outputs to the subgraph
            var inputEdgeNeedsRemap = new List<IEdge>();
            var outputEdgeNeedsRemap = new List<IEdge>();
            foreach (var edge in deserialized.edges)
            {
                var outputSlot = edge.outputSlot;
                var inputSlot = edge.inputSlot;

                Guid remappedOutputNodeGuid;
                Guid remappedInputNodeGuid;
                var outputRemapExists = nodeGuidMap.TryGetValue(outputSlot.nodeGuid, out remappedOutputNodeGuid);
                var inputRemapExists = nodeGuidMap.TryGetValue(inputSlot.nodeGuid, out remappedInputNodeGuid);

                // pasting nice internal links!
                if (outputRemapExists && inputRemapExists)
                {
                    var outputSlotRef = new SlotReference(remappedOutputNodeGuid, outputSlot.slotId);
                    var inputSlotRef = new SlotReference(remappedInputNodeGuid, inputSlot.slotId);
                    graph.Connect(outputSlotRef, inputSlotRef);
                }
                // one edge needs to go to outside world
                else if (outputRemapExists)
                {
                    inputEdgeNeedsRemap.Add(edge);
                }
                else if (inputRemapExists)
                {
                    outputEdgeNeedsRemap.Add(edge);
                }
            }

            // we do a grouping here as the same output can
            // point to multiple inputs
            var uniqueOutputs = outputEdgeNeedsRemap.GroupBy(edge => edge.outputSlot);
            var inputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueOutputs)
            {
                var inputNode = graph.inputNode;
                var slotId = inputNode.AddSlot();

                var outputSlotRef = new SlotReference(inputNode.guid, slotId);

                foreach (var edge in group)
                {
                    var newEdge = graph.Connect(outputSlotRef, new SlotReference(nodeGuidMap[edge.inputSlot.nodeGuid], edge.inputSlot.slotId));
                    inputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }

            var uniqueInputs = inputEdgeNeedsRemap.GroupBy(edge => edge.inputSlot);
            var outputsNeedingConnection = new List<KeyValuePair<IEdge, IEdge>>();
            foreach (var group in uniqueInputs)
            {
                var outputNode = graph.outputNode;
                var slotId = outputNode.AddSlot();

                var inputSlotRef = new SlotReference(outputNode.guid, slotId);

                foreach (var edge in group)
                {
                    var newEdge = graph.Connect(new SlotReference(nodeGuidMap[edge.outputSlot.nodeGuid], edge.outputSlot.slotId), inputSlotRef);
                    outputsNeedingConnection.Add(new KeyValuePair<IEdge, IEdge>(edge, newEdge));
                }
            }
            AssetDatabase.CreateAsset(graphAsset, path);

            var subGraphNode = new SubGraphNode();
            targetGraph.AddNode(subGraphNode);
            subGraphNode.subGraphAsset = graphAsset;

            foreach (var edgeMap in inputsNeedingConnection)
            {
                targetGraph.Connect(edgeMap.Key.outputSlot, new SlotReference(subGraphNode.guid, edgeMap.Value.outputSlot.slotId));
            }
            foreach (var edgeMap in outputsNeedingConnection)
            {
                targetGraph.Connect(new SlotReference(subGraphNode.guid, edgeMap.Value.inputSlot.slotId), edgeMap.Key.inputSlot);
            }

            var toDelete = m_Canvas.selection.Where(x => x is DrawableNode).ToList();
            dataSource.DeleteElements(toDelete);

            targetGraph.ValidateGraph();
            m_Canvas.ReloadData();
            m_Canvas.Invalidate();
            m_Canvas.selection.Clear();

            var toSelect = m_Canvas.elements.OfType<DrawableNode>().FirstOrDefault(x => x.m_Node == subGraphNode);
            if (toSelect != null)
            {
                toSelect.selected = true;
                m_Canvas.selection.Add(toSelect);
            }
            m_Canvas.Repaint();
        }

        private void Rebuild()
        {
            if (m_Canvas == null || m_LastSelection == null)
                return;

            m_DataSource.graphAsset = m_LastSelection;
            m_Canvas.ReloadData();
        }

        void OnGUI()
        {
            m_HostWindow = this;
            if (m_Canvas == null)
            {
                InitializeCanvas();
            }

            if (m_LastSelection == null || m_LastSelection.graph == null)
            {
                GUILayout.Label("No Graph selected");
                return;
            }

            m_Canvas.OnGUI(this, new Rect(0, 0, position.width - 250, position.height));

            if (GUI.Button(new Rect(position.width - 250, 0, 250, 50), "Convert to Sub-Graph"))
                ConvertSelectionToSubGraph();

            if (GUI.Button(new Rect(position.width - 250, 70, 250, 50), "Export"))
                Export(false);


            if (GUI.Button(new Rect(position.width - 250, 140, 250, 50), "Export - quick"))
                Export(true);


            EditorGUI.ObjectField(new Rect(position.width - 250, 210, 250, 50), rt, typeof(RenderTexture), false);
        }

        private string m_LastPath;
        public void Export(bool quickExport)
        {
            var path = quickExport ? m_LastPath : EditorUtility.SaveFilePanelInProject("Export shader to file...", "shader.shader", "shader", "Enter file name");
            m_LastPath = path; // For quick exporting
            if (!string.IsNullOrEmpty(path))
                ExportShader(m_DataSource.graphAsset as MaterialGraphAsset, path);
            else
                EditorUtility.DisplayDialog("Export Shader Error", "Cannot export shader", "Ok");
        }

        public static Shader ExportShader(MaterialGraphAsset graphAsset, string path)
        {
            if (graphAsset == null)
                return null;

            var materialGraph = graphAsset.graph as PixelGraph;
            if (materialGraph == null)
                return null;

            List<PropertyGenerator.TextureInfo> configuredTextures;
            var shaderString = ShaderGenerator.GenerateSurfaceShader(materialGraph.pixelMasterNode, new MaterialOptions(), materialGraph.name, false, out configuredTextures);
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
        }

        /*public void RenderOptions(MaterialGraph graph)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, EditorStyles.textArea, GUILayout.width(250), GUILayout.ExpandHeight(true));
            graph.materialOptions.DoGUI();
            EditorGUILayout.Separator();

            m_NodeExpanded = MaterialGraphStyles.Header("Selected", m_NodeExpanded);
            if (m_NodeExpanded)
                DrawableMaterialNode.OnGUI(m_Canvas.selection);

            GUILayout.EndScrollView();
            if (GUILayout.Button("Export"))
                m_DataSource.Export(false);

            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }*/

        public void OnBeforeSerialize()
        {
            var o = m_LastSelection as ScriptableObject;
            if (o != null)
                m_LastSelectedGraphSerialized = o;
        }

        public void OnAfterDeserialize()
        {
            if (m_LastSelectedGraphSerialized != null)
                m_LastSelection = m_LastSelectedGraphSerialized as T;

            m_LastSelectedGraphSerialized = null;
        }
    }
}
