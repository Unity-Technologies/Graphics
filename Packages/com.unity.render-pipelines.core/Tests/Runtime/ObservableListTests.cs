using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class ObservableListTests
    {
        [Test]
        public void ObservableList_CompletelyCleared()
        {
            var observableList = new ObservableList<int>();

            for(int i = 0; i < 5; i++)
                observableList.Add(i);

            observableList.Clear();
            Assert.AreEqual(0, observableList.Count,
                $"{observableList.Count} elements remaining after ObservableList.Clear(), expected 0");
        }
    }
}
