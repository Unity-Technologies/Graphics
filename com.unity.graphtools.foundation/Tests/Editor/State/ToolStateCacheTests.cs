using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    [Serializable]
    class CacheStateComponent : StateComponent<CacheStateComponent.StateUpdater>
    {
        [SerializeField]
        int m_Value = 42;

        public class StateUpdater : BaseUpdater<CacheStateComponent>
        {
            public int Value
            {
                set => m_State.Value = value;
            }
        }

        public int Value
        {
            get => m_Value;
            private set => m_Value = value;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }
    }

    [TestFixture]
    public class ToolStateCacheTests
    {
        static string s_CacheDir = "Library/StateCache/GraphToolsFoundationTests/";

        static void AssertCacheDirIsEmptyOrNonExistent()
        {
            // Check that cache dir is empty or nonexistent.
            var di = new DirectoryInfo(s_CacheDir);
            if (di.Exists)
            {
                Assert.IsFalse(di.GetFiles().Any());
                Assert.IsFalse(di.GetDirectories().Any());
            }
        }

        [SetUp]
        public void SetUp()
        {
            var di = new DirectoryInfo(s_CacheDir);
            if (di.Exists)
            {
                foreach (var file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (var dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }

            AssertCacheDirIsEmptyOrNonExistent();
        }

        [Test]
        public void GetNonExistingStateWithNullDefaultReturnsNull()
        {
            var stateCache = new StateCache(s_CacheDir);
            var key = new Hash128(42, 42, 42, 42);
            var state = stateCache.GetState<CacheStateComponent>(key);
            Assert.IsNull(state);
        }

        [Test]
        public void GetStateCreatesFileAndRemoveStateRemovesFile()
        {
            var stateCache = new StateCache(s_CacheDir);
            var key = new Hash128(42, 42, 42, 42);
            var defaultState = new CacheStateComponent();
            var state = stateCache.GetState(key, () => defaultState);
            Assert.AreEqual(defaultState, state);

            stateCache.StoreState(key, state);
            stateCache.Flush();

            var di = new DirectoryInfo(s_CacheDir);
            Assert.IsTrue(di.Exists);
            var filePath = stateCache.GetFilePathForKey(key);
            Assert.IsTrue(File.Exists(filePath));

            stateCache.RemoveState(key);
            Assert.IsFalse(File.Exists(filePath));
        }
    }
}
