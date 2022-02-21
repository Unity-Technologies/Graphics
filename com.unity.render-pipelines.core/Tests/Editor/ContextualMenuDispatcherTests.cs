using NUnit.Framework;
using System;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    #region Components
    class Apple : MonoBehaviour
    {

    }

    [RequireComponent(typeof(Apple))]
    class AdditionalApple : MonoBehaviour, IAdditionalData
    {
    }

    class Banana : MonoBehaviour
    {
    }
    [RequireComponent(typeof(Banana))]
    class AdditionalBanana : MonoBehaviour, IAdditionalData
    {
    }

    [RequireComponent(typeof(Apple))]
    [RequireComponent(typeof(Banana))]
    class FruitBasket : MonoBehaviour, IAdditionalData
    {
    }
    #endregion

    class ContextualMenuDispatcherTests
    {
        [Test]
        public void RemoveComponent()
        {
            var gameObject = new GameObject();

            gameObject.AddComponent<AdditionalBanana>();
            var banana = gameObject.AddComponent<Banana>();
            Assert.True(banana != null);

            gameObject.AddComponent<AdditionalApple>();
            var apple = gameObject.AddComponent<Apple>();
            Assert.True(apple != null);

            ContextualMenuDispatcher.RemoveComponent(banana);
            Assert.True(gameObject.GetComponent<AdditionalBanana>() == null, "AdditionalComponent has not been removed");
            Assert.True(gameObject.GetComponent<AdditionalApple>() != null, "AdditionalComponent has been removed, but it shouldn't");

            ContextualMenuDispatcher.RemoveComponent(apple);
            Assert.True(gameObject.GetComponent<AdditionalApple>() == null, "AdditionalComponent has not been removed");

            GameObject.DestroyImmediate(gameObject);
        }

        [Test]
        public void RemoveAdditionalComponent()
        {
            var gameObject = new GameObject();

            var additionalData = gameObject.AddComponent<FruitBasket>();
            gameObject.AddComponent<Banana>();
            gameObject.AddComponent<Apple>();

            using (ListPool<Type>.Get(out var componentsToRemove))
            {
                if (!ContextualMenuDispatcher.TryGetComponentsToRemove(additionalData, componentsToRemove, out var error))
                    throw error;

                Assert.AreEqual(2, componentsToRemove.Count);

                ContextualMenuDispatcher.RemoveAdditionalDataComponent(additionalData, componentsToRemove);
            }

            var banana = gameObject.GetComponent<Banana>();
            var apple = gameObject.GetComponent<Apple>();

            Assert.True(banana == null);
            Assert.True(apple == null);

            GameObject.DestroyImmediate(gameObject);
        }
    }
}
