using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
  [TestFixture]
  sealed class DebugDisplaySettingsResetTests
  {
      bool m_OriginalStreamingTextureDiscardUnusedMips;

      [SetUp]
      public void SetUp()
      {
          m_OriginalStreamingTextureDiscardUnusedMips = Texture.streamingTextureDiscardUnusedMips;
      }

      [TearDown]
      public void TearDown()
      {
          Texture.streamingTextureDiscardUnusedMips = m_OriginalStreamingTextureDiscardUnusedMips;
      }

      [Test]
      public void GivenNewInstance_WhenGetResetCalled_ThenResetActionIsNotNull()
      {
          // ARRANGE
          var settings = new DebugDisplaySettings();

          // ACT
          var resetAction = ((IDebugData)settings).GetReset();

          // ASSERT
          Assert.That(resetAction, Is.Not.Null, "reset action must be initialized in the constructor");
      }

      [Test]
      public void GivenModifiedDebugData_WhenResetActionInvoked_ThenDebugMipIsResetToDefault()
      {
          // ARRANGE
          var settings = new DebugDisplaySettings();
          settings.data.fullscreenDebugMip = 1.0f;
          var resetAction = ((IDebugData)settings).GetReset();

          // ACT
          resetAction.Invoke();

          // ASSERT
          Assert.That(settings.data.fullscreenDebugMip, Is.EqualTo(0.0f),
              "fullscreenDebugMip should be reset to its default value of 0.0f");
      }

      [Test]
      public void GivenResetActionInvokedOnce_WhenInvokedAgain_ThenNoExceptionIsThrown()
      {
          // ARRANGE
          var settings = new DebugDisplaySettings();
          var resetAction = ((IDebugData)settings).GetReset();
          resetAction.Invoke();

          // ACT / ASSERT
          Assert.That(() => resetAction.Invoke(), Throws.Nothing,
              "reset action must be safe to invoke multiple times");
      }

      [Test]
      public void GivenStreamingTextureDiscardUnusedMipsIsTrue_WhenResetActionInvoked_ThenStreamingTextureDiscardUnusedMipsIsFalse()
      {
          // ARRANGE
          Texture.streamingTextureDiscardUnusedMips = true;
          var settings = new DebugDisplaySettings();
          var resetAction = ((IDebugData)settings).GetReset();

          // ACT
          resetAction.Invoke();

          // ASSERT
          Assert.That(Texture.streamingTextureDiscardUnusedMips, Is.False,
              "streamingTextureDiscardUnusedMips must be false after the reset action is invoked");
      }

      [Test]
      public void GivenFreshInstance_WhenGetResetCalledMultipleTimes_ThenSameActionIsReturned()
      {
          // ARRANGE
          var settings = new DebugDisplaySettings();

          // ACT
          var first = ((IDebugData)settings).GetReset();
          var second = ((IDebugData)settings).GetReset();

          // ASSERT
          Assert.That(first, Is.SameAs(second),
              "GetReset() must return the same action instance on every call");
      }
  }
}
