#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System.Collections;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class VFXTimelineTest
    {
        [UnityTest]
        public IEnumerator Verify_Playable_Migration()
        {
            //This asset contains old activation tracks & clip
            var kSourceAsset = "Assets/AllTests/Editor/Tests/VFXOldTimelineActivation.playable_";

            var expectedEnterName = new [] {"shoot", "once"};
            var expectedExitName = new [] { "ceasefire", "" };

            var timelineAsset = VFXTestCommon.CopyTemporaryTimeline(kSourceAsset);
            yield return null;
            Assert.IsNotNull(timelineAsset);

            //Skip the really first track (marker)
            var allTracks = timelineAsset.GetOutputTracks().Skip(1).ToArray();
            Assert.AreEqual(8, allTracks.Length);

            foreach (var track in allTracks)
            {
                Assert.IsInstanceOf<VisualEffectControlTrack>(track);
                Assert.IsNotEmpty(track.GetClips());

                foreach (var clip in track.GetClips())
                {
                    var vfxClip = clip.asset as VisualEffectControlClip;
                    Assert.IsNotNull(vfxClip);
                    Assert.IsFalse(vfxClip.scrubbing);
                    Assert.AreEqual(1, vfxClip.clipEvents.Count);

                    var clipEvent = vfxClip.clipEvents.First();
                    Assert.Contains((string)clipEvent.enter.name, expectedEnterName);
                    Assert.Contains((string)clipEvent.exit.name, expectedExitName);

                    Assert.AreEqual(1, clipEvent.enter.eventAttributes.content.Length);

                    var attribute = clipEvent.enter.eventAttributes.content.First() as EventAttributeColor;
                    Assert.IsNotNull(attribute);

                    Assert.AreEqual("color", (string)attribute.id);
                    var sqrLengthToWhite = (UnityEngine.Vector3.one - attribute.value).sqrMagnitude;
                    var sqrLengthToBlack = (UnityEngine.Vector3.one - attribute.value).sqrMagnitude;
                    Assert.Greater(sqrLengthToWhite, 0.01f);
                    Assert.Greater(sqrLengthToBlack, 0.01f);
                }
            }
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }
}
#endif
