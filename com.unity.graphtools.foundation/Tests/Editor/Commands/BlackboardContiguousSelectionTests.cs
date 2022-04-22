using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class BlackboardContiguousSelectionTests : BlackboardSharedTestClasses
    {
        struct Infos
        {
            public IVariableDeclarationModel out1;
            public IVariableDeclarationModel out2;
            public IVariableDeclarationModel var1;
            public IVariableDeclarationModel var2;
            public IVariableDeclarationModel var3;
            public IVariableDeclarationModel var4;
            public IVariableDeclarationModel var5;
            public IVariableDeclarationModel var6;
            public IVariableDeclarationModel var7;
            public IVariableDeclarationModel var8;

            public IGroupModel group1;
            public IGroupModel group2;
            public IGroupModel group3;
            public IGroupModel group5;
            public IGroupModel group6;
        }

        Infos m_Infos;


        [SetUp]
        public override void Setup()
        {
            base.Setup();

            /*
             * output section
             *  out1
             *  out2
             * variable section
             * - var1
             * - var2
             * - group1
             *  - var3
             *  - var4
             *  - group2
             *      - var5
             *  - group3
             *      - var6
             *  - group4
             *  - var7
             * var8
             */

        }

        IEnumerator Start()
        {
            yield return null;

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("out1",false,TypeHandle.Float,typeof(BlackboardOutputVariableDeclarationModel)));

            m_Infos.out1 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("out2",false,TypeHandle.Float,typeof(BlackboardOutputVariableDeclarationModel)));

            m_Infos.out2 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var1",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel)));

            m_Infos.var1 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var2",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel)));

            m_Infos.var2 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            var variableSection =
                m_GraphAsset.GraphModel.GetSectionModel(
                    Stencil.sections[(int)VariableType.Variable]);

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(variableSection,m_Infos.var2,"group1"));
            yield return null;

            m_Infos.group1 = (IGroupModel)variableSection.Items.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var3",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),m_Infos.group1));
            yield return null;

            m_Infos.var3 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var4",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),m_Infos.group1));
            yield return null;

            m_Infos.var4 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(m_Infos.group1,m_Infos.var4,"group2"));
            yield return null;

            m_Infos.group2 = (IGroupModel)m_Infos.group1.Items.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var5",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),m_Infos.group2));
            yield return null;

            m_Infos.var5 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(m_Infos.group1,null,"group3"));
            yield return null;

            m_Infos.group3 = (IGroupModel)m_Infos.group1.Items.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var6",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),m_Infos.group3));
            yield return null;

            m_Infos.var6 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

           m_BlackboardView.Dispatch(new BlackboardGroupCreateCommand(m_Infos.group1,m_Infos.group3,"group4"));
            yield return null;

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var7",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),m_Infos.group1));
            yield return null;

            m_Infos.var7 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            m_BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand("var8",false,TypeHandle.Float,typeof(BlackboardVariableDeclarationModel),variableSection));
            yield return null;

            m_Infos.var8 = m_GraphAsset.GraphModel.VariableDeclarations.Last();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection1()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.out1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.out2.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;
            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 2);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection2()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.out1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.var2.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 4);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection3()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.out1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.group1.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 5);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.group1));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection4()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.out1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.var3.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 5);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var3));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection5()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.out1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.var6.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 8);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var3));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var4));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var5));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var6));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection6()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.var1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.var8.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 8);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var3));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var4));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var5));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var6));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var7));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var8));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection7()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.var1));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.out1.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 3);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.out1));
        }

        [UnityTest]
        public IEnumerator TestContiguousSelection8()
        {
            yield return Start();

            m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace,m_Infos.var8));

            yield return null;

            m_BlackboardView.ExtendSelection(m_Infos.var1.GetView<BlackboardElement>(m_BlackboardView));

            yield return null;

            Assert.IsTrue(m_BlackboardView.GetSelection().Count == 8);
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var1));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var2));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var3));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var4));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var5));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var6));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var7));
            Assert.IsTrue(m_BlackboardView.GetSelection().Contains(m_Infos.var8));
        }
    }
}
