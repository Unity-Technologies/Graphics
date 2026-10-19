using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-153102: the Light2D Material fields list a Light2D or Shadow2D Shader Graph in their pickers.
    // Dropping the same graph from the Project window did not assign it, though. A Project-window drag
    // carries the graph's main asset, which is a Shader, while the field takes the Material sub-asset
    // the importer adds beside it.
#if ENABLE_CORECLR
    [Explicit("MultiJson-serialized ShaderGraph written to disk fails to reimport on CoreCLR (JSON parse error: The document root must not follow by other values), so no Shader or Material sub-assets are produced, see https://jira.unity3d.com/browse/UUM-150436")]
#endif
    class Light2DMaterialDragTests
    {
        const string k_Folder = "Assets/Light2DMaterialDragTests";
        const string k_LightGraphPath = k_Folder + "/Light2DDragGraph.shadergraph";
        const string k_ShadowGraphPath = k_Folder + "/Shadow2DDragGraph.shadergraph";

        class DropHost : EditorWindow
        {
            // The test sets these and the window only lives for one test, so they never need to survive a domain reload.
            [NonSerialized] public SerializedObject serializedObject;
            public string propertyPath;
            [NonSerialized] public Func<Material, bool> isCompatible;
            public GUIContent label = GUIContent.none;

            // Where the field was drawn, and the mouse position OnGUI last saw. SendEvent takes window
            // coordinates, which OnGUI sees shifted by the tab area, so the test measures that shift.
            public Rect fieldRect;
            public Vector2 lastMousePosition;

            void OnGUI()
            {
                if (serializedObject == null)
                    return;

                if (Event.current.type != EventType.Layout)
                    lastMousePosition = Event.current.mousePosition;
                bool isLayout = Event.current.type == EventType.Layout;

                serializedObject.Update();
                Light2DEditorUtility.DrawFilteredMaterialField(serializedObject.FindProperty(propertyPath), label, isCompatible);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                if (!isLayout)
                    fieldRect = GUILayoutUtility.GetLastRect();
            }
        }

        GameObject m_GameObject;
        Light2D m_Light;
        DropHost m_Host;

        static void WriteGraph(string path, Type subTarget, BlockFieldDescriptor[] blocks)
        {
            var target = (UniversalTarget)Activator.CreateInstance(typeof(UniversalTarget));
            target.TrySetActiveSubTarget(subTarget);

            var graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new Target[] { target }, blocks);
            graph.AddCategory(CategoryData.DefaultCategory());
            graph.path = "Shader Graphs";

            FileUtilities.WriteShaderGraphToDisk(path, graph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        [OneTimeSetUp]
        public void CreateGraphs()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(k_Folder));

            WriteGraph(k_LightGraphPath, typeof(UniversalLight2DSubTarget), new[]
            {
                BlockFields.VertexDescription.Position,
                UniversalBlockFields.SurfaceDescription.Light2DColor,
                UniversalBlockFields.SurfaceDescription.Light2DShadowColor,
                UniversalBlockFields.SurfaceDescription.Light2DVolumetricColor,
            });

            WriteGraph(k_ShadowGraphPath, typeof(UniversalShadowCaster2DSubTarget), new[]
            {
                BlockFields.VertexDescription.Position,
                UniversalBlockFields.VertexDescription.ShadowDisplacement,
                UniversalBlockFields.VertexDescription.ShadowSideSoftness,
                UniversalBlockFields.VertexDescription.ShadowBackSoftness,
                BlockFields.SurfaceDescription.Alpha,
            });
        }

        [OneTimeTearDown]
        public void DeleteGraphs()
        {
            AssetDatabase.DeleteAsset(k_Folder);
        }

        [SetUp]
        public void SetUp()
        {
            m_GameObject = new GameObject("Light2DMaterialDragTests");
            m_Light = m_GameObject.AddComponent<Light2D>();
            m_Light.material = null;
            m_Light.shadowMaterial = null;
        }

        [TearDown]
        public void TearDown()
        {
            DragAndDrop.PrepareStartDrag();
            if (m_Host != null)
            {
                m_Host.serializedObject?.Dispose();
                m_Host.serializedObject = null;
                m_Host.Close();
            }
            UnityEngine.Object.DestroyImmediate(m_GameObject);
        }

        void ShowField(string propertyPath, Func<Material, bool> isCompatible)
        {
            m_Host = ScriptableObject.CreateInstance<DropHost>();
            m_Host.serializedObject = new SerializedObject(m_Light);
            m_Host.propertyPath = propertyPath;
            m_Host.isCompatible = isCompatible;
            m_Host.position = new Rect(100, 100, 400, 100);
            m_Host.Show();
        }

        void ShowLightMaterialField() => ShowField("m_Material", RendererLighting.IsCompatibleLightMaterial);
        void ShowShadowMaterialField() => ShowField("m_ShadowMaterial", ShadowRendering.IsCompatibleShadowMaterial);

        static Material GraphMaterial(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Single();
        }

        void Drop(UnityEngine.Object dragged) => Drop(dragged, rect => rect.center);

        // `target` picks the drop point inside the rect the field was drawn in, label included.
        void Drop(UnityEngine.Object dragged, Func<Rect, Vector2> target)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new[] { dragged };
            DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(dragged) };

            // A first event finds the field and the offset between window and OnGUI coordinates.
            // It lands outside the field, so it cannot drop anything.
            m_Host.SendEvent(new Event { type = EventType.DragUpdated, mousePosition = Vector2.zero });
            Assert.AreNotEqual(Rect.zero, m_Host.fieldRect, "The host window never drew the field.");
            var mouse = target(m_Host.fieldRect) - m_Host.lastMousePosition;

            m_Host.SendEvent(new Event { type = EventType.DragUpdated, mousePosition = mouse });
            m_Host.SendEvent(new Event { type = EventType.DragPerform, mousePosition = mouse });
        }

        // Control: the field takes the graph's Material sub-asset, so the drop harness itself works.
        [Test]
        public void DroppingTheGraphMaterialSubAsset_AssignsIt()
        {
            ShowLightMaterialField();
            var material = GraphMaterial(k_LightGraphPath);
            Assert.IsTrue(RendererLighting.IsCompatibleLightMaterial(material));

            Drop(material);

            Assert.AreEqual(material, m_Light.material);
        }

        // What a Project-window drag of a .shadergraph carries: the main asset, a Shader.
        [Test]
        public void DroppingTheLight2DShaderGraphAsset_AssignsItsMaterial()
        {
            ShowLightMaterialField();
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);
            Assert.IsInstanceOf<Shader>(mainAsset);

            Drop(mainAsset);

            Assert.AreEqual(GraphMaterial(k_LightGraphPath), m_Light.material,
                "Dropping the Light2D Shader Graph asset must assign the Material it imports.");
        }

        [Test]
        public void DroppingTheShadow2DShaderGraphAsset_OnTheShadowField_AssignsItsMaterial()
        {
            ShowShadowMaterialField();
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_ShadowGraphPath);
            Assert.IsInstanceOf<Shader>(mainAsset);
            Assert.IsTrue(ShadowRendering.IsCompatibleShadowMaterial(GraphMaterial(k_ShadowGraphPath)));

            Drop(mainAsset);

            Assert.AreEqual(GraphMaterial(k_ShadowGraphPath), m_Light.shadowMaterial,
                "Dropping the Shadow2D Shader Graph asset must assign the Material it imports.");
        }

        // The shadow field's filter still applies to the Material a graph drop resolves to.
        [Test]
        public void DroppingTheLight2DShaderGraphAsset_OnTheShadowField_IsRejected()
        {
            ShowShadowMaterialField();
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);
            Assert.IsFalse(ShadowRendering.IsCompatibleShadowMaterial(GraphMaterial(k_LightGraphPath)));

            Drop(mainAsset);

            Assert.IsNull(m_Light.shadowMaterial);
            Assert.AreEqual(DragAndDropVisualMode.Rejected, DragAndDrop.visualMode);
        }

        // With a label, the drop lands on the object field part of the control, as the inspector draws it.
        [Test]
        public void DroppingTheLight2DShaderGraphAsset_OnALabelledField_AssignsItsMaterial()
        {
            ShowLightMaterialField();
            m_Host.label = new GUIContent("Material");
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);

            Drop(mainAsset, rect => new Vector2(rect.xMax - 30, rect.center.y));

            Assert.AreEqual(GraphMaterial(k_LightGraphPath), m_Light.material);
        }

        // A drop on the label does not assign a Material, so it does not assign a graph either.
        [Test]
        public void DroppingTheLight2DShaderGraphAsset_OnTheLabel_DoesNotAssign()
        {
            ShowLightMaterialField();
            m_Host.label = new GUIContent("Material");
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);

            Drop(mainAsset, rect => new Vector2(rect.x + 5, rect.center.y));

            Assert.IsNull(m_Light.material);
        }

        // The drop assigns the sub-asset without rewriting what the drag carries.
        [Test]
        public void DroppingAShaderGraphAsset_LeavesTheDragPayloadAlone()
        {
            ShowLightMaterialField();
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);

            Drop(mainAsset);

            CollectionAssert.AreEqual(new[] { mainAsset }, DragAndDrop.objectReferences);
        }

        [Test]
        public void GetShaderGraphMaterial_IgnoresPayloadsItShouldNotMap()
        {
            var shader = AssetDatabase.LoadMainAssetAtPath(k_LightGraphPath);
            var material = GraphMaterial(k_LightGraphPath);

            Assert.AreEqual(material, Light2DEditorUtility.GetShaderGraphMaterial(new[] { shader }));
            Assert.IsNull(Light2DEditorUtility.GetShaderGraphMaterial(null));
            Assert.IsNull(Light2DEditorUtility.GetShaderGraphMaterial(new UnityEngine.Object[0]));
            // A payload with a Material goes through the usual path.
            Assert.IsNull(Light2DEditorUtility.GetShaderGraphMaterial(new[] { shader, material }));
            // A built-in Shader has no Material beside it.
            Assert.IsNull(Light2DEditorUtility.GetShaderGraphMaterial(new[] { Shader.Find("Hidden/Internal-Colored") }));
            // A Shader with no Material beside it does not hide a graph later in the payload.
            Assert.AreEqual(material, Light2DEditorUtility.GetShaderGraphMaterial(new[] { Shader.Find("Hidden/Internal-Colored"), shader }));
        }
    }
}
