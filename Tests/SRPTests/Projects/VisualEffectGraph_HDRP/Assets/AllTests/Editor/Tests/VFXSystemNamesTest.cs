using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements.TestFramework;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    class VFXSystemNamesTest
    {
        private string m_TestAssetRandomFileName;

        [SetUp]
        public void Setup()
        {
            EventHelpers.TestSetUp();
            CloseAllWindows();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            CloseAllWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();
            EventHelpers.TestTearDown();
        }

        private void CloseAllWindows()
        {
            VFXViewWindow.GetAllWindows()
                .ToList()
                .ForEach(x => x.Close());
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>()?.Close();
            }
        }

        [Test]
        public void UniqueSystemNames()
        {
            string[] names =
            {
                "foo",
                "bar",
                null,
                "foo",
                "bar",
                "foobar",
                "foobar (1)",
                "foobar (1)",
                null,
                "",
                "System", //VFXSystemNames.DefaultSystemName
                "System", //VFXSystemNames.DefaultSystemName
                "foo",
                "bar",
                "J'aime les panoramas",
                "Vous savez, moi je ne crois pas qu’il y ait de bonne ou de mauvaise situation." +
                "Moi, si je devais résumer ma vie aujourd’hui avec vous, je dirais que c’est d’abord des rencontres. " +
                "Des gens qui m’ont tendu la main, peut-être à un moment où je ne pouvais pas, où j’étais seul chez moi. " +
                "Et c’est assez curieux de se dire que les hasards, les rencontres forgent une destinée… " +
                "Parce que quand on a le goût de la chose, quand on a le goût de la chose bien faite, le beau geste, " +
                "parfois on ne trouve pas l’interlocuteur en face je dirais, le miroir qui vous aide à avancer. " +
                "Alors ça n’est pas mon cas, comme je disais là, puisque moi au contraire, j’ai pu : " +
                "et je dis merci à la vie, je lui dis merci, je chante la vie, je danse la vie… je ne suis qu’amour ! " +
                "Et finalement, quand beaucoup de gens aujourd’hui me disent \" Mais comment fais-tu pour avoir cette humanité ? \", " +
                "et bien je leur réponds très simplement, je leur dis que c’est ce goût de l’amour ce goût donc qui m’a poussé aujourd’hui à entreprendre une construction mécanique, " +
                "mais demain qui sait ? Peut-être simplement à me mettre au service de la communauté, à faire le don, le don de soi… "
            };

            var spawnerCount = names.Length / 2;
            var GPUSystemCount = names.Length - spawnerCount;

            var models = new List<VFXContext>();
            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();

            int i = 0;
            for (; i < spawnerCount; ++i)
            {
                var context = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                VFXSystemNames.SetSystemName(context, names[i]);
                graph.AddChild(context);
                models.Add(context);
            }
            for (; i < spawnerCount + GPUSystemCount; ++i)
            {
                var context = ScriptableObject.CreateInstance<VFXBasicGPUEvent>();
                VFXSystemNames.SetSystemName(context, names[i]);
                graph.AddChild(context);
                models.Add(context);
            }

            var systemNames = graph.systemNames;
            systemNames.Sync(graph);
            var uniqueNames = models
                .Select(model => systemNames.GetUniqueSystemName(model.GetData()))
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            Assert.IsTrue(uniqueNames.Count == names.Length, "Some systems have the same name or are null or empty.");
        }

        [Test]
        public void Create_Two_Systems()
        {
            // Prepare
            if (!Directory.Exists(VFXTestCommon.tempBasePath))
            {
                Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            }

            m_TestAssetRandomFileName = $"{VFXTestCommon.tempBasePath}random_{Guid.NewGuid()}.vfx";
            // Create default VFX Graph
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(m_TestAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(m_TestAssetRandomFileName);

            // Open this vfx the same way it would be done by a user
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(m_TestAssetRandomFileName);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));

            var window = VFXViewWindow.GetWindow(asset, true);
            var viewController = window.graphView.controller;

            // Act
            // Create a new system
            var spawnerContext = viewController.AddVFXContext(new Vector2(0, 0), VFXLibrary.GetContexts().Single(o => o.name == "Spawn").variant);
            var initializeContext = viewController.AddVFXContext(new Vector2(0, 200), VFXLibrary.GetContexts().Single(o => o.name == "Initialize Particle").variant);
            var updateContext = viewController.AddVFXContext(new Vector2(0, 500), VFXLibrary.GetContexts().Single(o => o.name == "Update Particle").variant);
            var outputContext = viewController.AddVFXContext(new Vector2(0, 700), VFXLibrary.GetContexts().Single(o => o.name == "Output Particle".AppendLabel("Unlit").AppendLabel("Quad")).variant);

            spawnerContext.LinkTo(initializeContext);
            initializeContext.LinkTo(updateContext);
            updateContext.LinkTo(outputContext);
            viewController.ApplyChanges();
            var systemNames = viewController.graph.systemNames;
            systemNames.Sync(viewController.graph);

            // Assert
            // Spawner makes up a system by itself
            Assert.AreEqual("System (1)", systemNames.GetUniqueSystemName(spawnerContext.GetData()));
            Assert.AreEqual("System (2)", systemNames.GetUniqueSystemName(initializeContext.GetData()));
            Assert.AreEqual("System (2)", systemNames.GetUniqueSystemName(updateContext.GetData()));
            Assert.AreEqual("System (2)", systemNames.GetUniqueSystemName(outputContext.GetData()));
        }

        [UnityTest]
        public IEnumerator Overwrite_Opened_VFX()
        {
            VFXViewWindow.ShowWindow();
            yield return null;
            Assert.True(EditorWindow.HasOpenInstances<VFXViewWindow>());
            var vfxViewWindow = EditorWindow.GetWindowDontShow<VFXViewWindow>();

            var onCreateAssetMethod = vfxViewWindow.graphView.GetType().GetMethod("OnCreateAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onCreateAssetMethod);

            // Create first VFX using firework template
            GraphViewTemplateWindowHelpers.SetLastUsedTemplatePref("d379eac073f73024780a8968d2259cef");
            onCreateAssetMethod.Invoke(vfxViewWindow.graphView, null);
            yield return null;
            var enumerator = GraphViewTemplateWindowOpening.CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
            vfxViewWindow.graphView.OnSave();

            // Create a new VFX using the simple loop template
            GraphViewTemplateWindowHelpers.SetLastUsedTemplatePref("a8d8823499ff50847aa460cb119c445d");
            onCreateAssetMethod.Invoke(vfxViewWindow.graphView, null);
            yield return null;

            enumerator = GraphViewTemplateWindowOpening.CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}
