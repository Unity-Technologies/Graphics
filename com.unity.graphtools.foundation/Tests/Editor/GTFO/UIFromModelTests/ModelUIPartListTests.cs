using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class ModelUIPartListTests
    {
        class DummyPart : IModelUIPart
        {
            public DummyPart(string name)
            {
                PartName = name;
            }

            public string PartName { get; set; }
            public ModelUIPartList PartList => null;
            public VisualElement Root => null;
            public void BuildUI(VisualElement parent)
            {
                throw new NotImplementedException();
            }

            public void PostBuildUI()
            {
                throw new NotImplementedException();
            }

            public void UpdateFromModel()
            {
                throw new NotImplementedException();
            }

            public void OwnerAddedToView()
            {
            }

            public void OwnerRemovedFromView()
            {
            }
        }

        static void AssertSequentialPartList(ModelUIPartList partList, int expectedCount)
        {
            Assert.AreEqual(expectedCount, partList.Parts.Count);

            int i = 1;
            foreach (var p in partList.Parts)
            {
                Assert.AreEqual(i.ToString(), p.PartName);
                i++;
            }
        }

        [Test]
        public void AppendsAtEnd()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            AssertSequentialPartList(partList, 3);
        }

        [Test]
        public void AppendDiscardsNull()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(null);
            partList.AppendPart(new DummyPart("2"));

            AssertSequentialPartList(partList, 2);
        }

        [Test]
        public void GetAppendedPartReturnsPart()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            for (var i = 1; i < 4; i++)
            {
                var part = partList.GetPart(i.ToString());
                Assert.IsNotNull(part);
                Assert.AreEqual(i.ToString(), part.PartName);
            }
        }

        [Test]
        public void GetUnknownPartReturnsNull()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            var part = partList.GetPart("not in list");
            Assert.IsNull(part);
        }

        [Test]
        public void InsertBeforeWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("4"));

            partList.InsertPartBefore("4", new DummyPart("3"));

            AssertSequentialPartList(partList, 4);
        }

        [Test]
        public void InsertBeforeFirstWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));
            partList.AppendPart(new DummyPart("4"));

            partList.InsertPartBefore("2", new DummyPart("1"));

            AssertSequentialPartList(partList, 4);
        }

        [Test]
        public void InsertBeforeNotFoundDoesNothingAndThrows()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => partList.InsertPartBefore("not found", new DummyPart("5"))
            );

            AssertSequentialPartList(partList, 3);
        }

        [Test]
        public void InsertAfterWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("3"));
            partList.AppendPart(new DummyPart("4"));

            partList.InsertPartAfter("1", new DummyPart("2"));

            AssertSequentialPartList(partList, 4);
        }

        [Test]
        public void InsertAfterLastWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            partList.InsertPartAfter("3", new DummyPart("4"));

            AssertSequentialPartList(partList, 4);
        }

        [Test]
        public void InsertAfterNotFoundDoesNothingAndThrows()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => partList.InsertPartBefore("not found", new DummyPart("5"))
            );

            AssertSequentialPartList(partList, 3);
        }

        [Test]
        public void ReplaceWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("5"));
            partList.AppendPart(new DummyPart("3"));

            partList.ReplacePart("5", new DummyPart("2"));

            AssertSequentialPartList(partList, 3);
        }

        [Test]
        public void ReplaceNotFoundDoesNothingAndThrows()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => partList.ReplacePart("not found", new DummyPart("5"))
            );

            AssertSequentialPartList(partList, 3);
        }

        [Test]
        public void RemoveWorks()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("5"));
            partList.AppendPart(new DummyPart("2"));

            partList.RemovePart("5");

            AssertSequentialPartList(partList, 2);
        }

        [Test]
        public void RemoveNotFoundDoesNothingAndThrows()
        {
            ModelUIPartList partList = new ModelUIPartList();
            partList.AppendPart(new DummyPart("1"));
            partList.AppendPart(new DummyPart("2"));
            partList.AppendPart(new DummyPart("3"));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => partList.RemovePart("not found")
            );

            AssertSequentialPartList(partList, 3);
        }
    }
}
