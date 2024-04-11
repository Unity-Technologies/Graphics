using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A helper function for interpolating AnimationCurves together. In general, curves can not be directly blended
    /// because they will have keypoints at different places. InterpAnimationCurve traverses through the keypoints.
    /// If both curves have a keypoint at the same time, they keypoints are trivially lerped together. However
    /// if one curve has a keypoint at a time that is missing in the other curve (which is the most common case),
    /// InterpAnimationCurve calculates a synthetic keypoint at that time based on value and derivative, and interpolates
    /// the resulting keys.
    /// Note that this function should only be called by internal rendering code. It creates a small pool of animation
    /// curves and reuses them to avoid creating garbage. The number of curves needed is quite small, since curves only need
    /// to be used when interpolating multiple volumes together with different curve parameters. The underlying interp
    /// function isn't allowed to fail, so in the case where we run out of memory we fall back to returning a single keyframe.
    /// </summary>
    ///
    /// <example>
    /// <code>
    /// {
    ///     AnimationCurve curve0 = new AnimationCurve();
    ///     curve0.AddKey(new Keyframe(0.0f, 3.0f));
    ///     curve0.AddKey(new Keyframe(4.0f, 2.0f));
    ///
    ///     AnimationCurve curve1 = new AnimationCurve();
    ///     curve1.AddKey(new Keyframe(0.0f, 0.0f));
    ///     curve1.AddKey(new Keyframe(2.0f, 1.0f));
    ///     curve1.AddKey(new Keyframe(4.0f, 4.0f));
    ///
    ///     float t = 0.5f;
    ///     KeyframeUtility.InterpAnimationCurve(curve0, curve1, t);
    ///
    ///     // curve0 now stores the resulting interpolated curve
    /// }
    /// </code>
    /// </example>
    public class KeyframeUtility
    {
        /// <summary>
        /// Helper function to remove all control points for an animation curve. Since animation curves are reused in a pool,
        /// this function clears existing keys so the curve is ready for reuse.
        /// </summary>
        /// <param name="curve">The curve to reset.</param>
        static public void ResetAnimationCurve(AnimationCurve curve)
        {
            curve.ClearKeys();
        }

        static private Keyframe LerpSingleKeyframe(Keyframe lhs, Keyframe rhs, float t)
        {
            var ret = new Keyframe();

            ret.time = Mathf.Lerp(lhs.time, rhs.time, t);
            ret.value = Mathf.Lerp(lhs.value, rhs.value, t);
            ret.inTangent = Mathf.Lerp(lhs.inTangent, rhs.inTangent, t);
            ret.outTangent = Mathf.Lerp(lhs.outTangent, rhs.outTangent, t);
            ret.inWeight = Mathf.Lerp(lhs.inWeight, rhs.inWeight, t);
            ret.outWeight = Mathf.Lerp(lhs.outWeight, rhs.outWeight, t);

            // it's not possible to lerp the weightedMode, so use the lhs mode.
            ret.weightedMode = lhs.weightedMode;

            // Note: ret.tangentMode is deprecated, so we will use  the value from the constructor
            return ret;
        }

        /// In an animation curve, the inTangent and outTangent don't match the edge of the curve. For example,
        /// the first key might have inTangent=3.0f but the actual incoming tangent is 0.0 because the curve is
        /// clamped outside the time domain. So this helper fetches a key, but zeroes out the inTangent of the first
        /// key and the outTangent of the last key.
        static private Keyframe GetKeyframeAndClampEdge([DisallowNull] NativeArray<Keyframe> keys, int index)
        {
            var lastKeyIndex = keys.Length - 1;
            if (index < 0 || index > lastKeyIndex)
            {
                Debug.LogWarning("Invalid index in GetKeyframeAndClampEdge. This is likely a bug.");
                return new Keyframe();
            }

            var currKey = keys[index];
            if (index == 0)
            {
                currKey.inTangent = 0.0f;
            }
            if (index == lastKeyIndex)
            {
                currKey.outTangent = 0.0f;
            }
            return currKey;
        }

        /// Fetch a key from the keys list. If index<0, then expand the first key backwards to startTime. If index>=keys.length,
        /// then extend the last key to endTime. Keys must be a valid array with at least one element.
        static private Keyframe FetchKeyFromIndexClampEdge([DisallowNull] NativeArray<Keyframe> keys, int index, float segmentStartTime, float segmentEndTime)
        {
            float startTime = Mathf.Min(segmentStartTime, keys[0].time);
            float endTime = Mathf.Max(segmentEndTime, keys[keys.Length - 1].time);

            float startValue = keys[0].value;
            float endValue = keys[keys.Length - 1].value;

            // In practice, we are lerping animcurves for post processing curves that are always clamping at the begining and the end,
            // so we are not implementing the other wrap modes like Loop, PingPong, etc.
            Keyframe ret;
            if (index < 0)
            {
                // when you are at a time either before the curve start time the value is clamped to the start time and the input tangent is ignored.
                ret = new Keyframe(startTime, startValue, 0.0f, 0.0f);
            }
            else if (index >= keys.Length)
            {
                // if we are after the end of the curve, there slope is always zero just like before the start of a curve
                var lastKey = keys[keys.Length - 1];
                ret = new Keyframe(endTime, endValue, 0.0f, 0.0f);
            }
            else
            {
                // only remaining case is that we have a proper index
                ret = GetKeyframeAndClampEdge(keys, index);
            }
            return ret;
        }


        /// Given a desiredTime, interpoloate between two keys to find the value and derivative. This function assumes that lhsKey.time <= desiredTime <= rhsKey.time,
        /// but will return a reasonable float value if that's not the case.
        static private void EvalCurveSegmentAndDeriv(out float dstValue, out float dstDeriv, Keyframe lhsKey, Keyframe rhsKey, float desiredTime)
        {
            // This is the same epsilon used internally
            const float epsilon = 0.0001f;

            float currTime = Mathf.Clamp(desiredTime, lhsKey.time, rhsKey.time);

            // (lhsKey.time <= rhsKey.time) should always be true. But theoretically, if garbage values get passed in, the value would
            // be clamped here to epsilon, and we would still end up with a reasonable value for dx.
            float dx = Mathf.Max(rhsKey.time - lhsKey.time, epsilon);
            float dy = rhsKey.value - lhsKey.value;
            float length = 1.0f / dx;
            float lengthSqr = length * length;

            float m1 = lhsKey.outTangent;
            float m2 = rhsKey.inTangent;
            float d1 = m1 * dx;
            float d2 = m2 * dx;

            // Note: The coeffecients are calculated to match what the editor does internally. These coeffeceients expect a
            // t in the range of [0,dx]. We could change the function to accept a range between [0,1], but then this logic would
            // be different from internal editor logic which could cause subtle bugs later.

            float c0 = (d1 + d2 - dy - dy) * lengthSqr * length;
            float c1 = (dy + dy + dy - d1 - d1 - d2) * lengthSqr;
            float c2 = m1;
            float c3 = lhsKey.value;

            float t = Mathf.Clamp(currTime - lhsKey.time, 0.0f, dx);

            dstValue = (t * (t * (t * c0 + c1) + c2)) + c3;
            dstDeriv = (t * (3.0f * t * c0 + 2.0f * c1)) + c2;
        }

        /// lhsIndex and rhsIndex are the indices in the keys array. The lhsIndex/rhsIndex may be -1, in which it creates a synthetic first key
        /// at startTime, or beyond the length of the array, in which case it creates a synthetic key at endTime.
        static private Keyframe EvalKeyAtTime([DisallowNull] NativeArray<Keyframe> keys, int lhsIndex, int rhsIndex, float startTime, float endTime, float currTime)
        {
            var lhsKey = KeyframeUtility.FetchKeyFromIndexClampEdge(keys, lhsIndex, startTime, endTime);
            var rhsKey = KeyframeUtility.FetchKeyFromIndexClampEdge(keys, rhsIndex, startTime, endTime);

            float currValue;
            float currDeriv;
            KeyframeUtility.EvalCurveSegmentAndDeriv(out currValue, out currDeriv, lhsKey, rhsKey, currTime);

            return new Keyframe(currTime, currValue, currDeriv, currDeriv);
        }


        /// <summary>
        /// Interpolates two AnimationCurves. Since both curves likely have control points at different places
        /// in the curve, this method will create a new curve from the union of times between both curves. However, to avoid creating
        /// garbage, this function will always replace the keys of lhsAndResultCurve with the final result, and return lhsAndResultCurve.
        /// </summary>
        /// <param name="lhsAndResultCurve">The start value. Additionaly, this instance will be reused and returned as the result.</param>
        /// <param name="rhsCurve">The end value.</param>
        /// <param name="t">The interpolation factor in range [0,1].</param>
        static public void InterpAnimationCurve(ref AnimationCurve lhsAndResultCurve, [DisallowNull] AnimationCurve rhsCurve, float t)
        {
            if (t <= 0.0f || rhsCurve.length == 0)
            {
                // no op. lhsAndResultCurve is already the result
            }
            else if (t >= 1.0f || lhsAndResultCurve.length == 0)
            {
                // In this case the obvious solution would be to return the rhsCurve. BUT (!) the lhsCurve and rhsCurve are different. This function is
                // called by:
                //      stateParam.Interp(stateParam, toParam, interpFactor);
                //
                // stateParam (lhsCurve) is a temporary in/out parameter, but toParam (rhsCurve) might point to the original component, so it's unsafe to
                // change that data. Thus, we need to copy the keys from the rhsCurve to the lhsCurve instead of returning rhsCurve.
                lhsAndResultCurve.CopyFrom(rhsCurve);
            }
            else
            {
                // Note: If we reached this code, we are guaranteed that both lhsCurve and rhsCurve are valid with at least 1 key

                // create a native array for the temp keys to avoid GC
                var lhsCurveKeys = new NativeArray<Keyframe>(lhsAndResultCurve.length, Allocator.Temp);
                var rhsCurveKeys = new NativeArray<Keyframe>(rhsCurve.length, Allocator.Temp);

                for (int i = 0; i < lhsAndResultCurve.length; i++)
                {
                    lhsCurveKeys[i] = lhsAndResultCurve[i];
                }

                for (int i = 0; i < rhsCurve.length; i++)
                {
                    rhsCurveKeys[i] = rhsCurve[i];
                }

                float startTime = Mathf.Min(lhsCurveKeys[0].time, rhsCurveKeys[0].time);
                float endTime = Mathf.Max(lhsCurveKeys[lhsAndResultCurve.length - 1].time, rhsCurveKeys[rhsCurve.length - 1].time);

                // we don't know how many keys the resulting curve will have (because we will compact keys that are at the exact
                // same time), but in most cases we will need the worst case number of keys. So allocate the worst case.
                int maxNumKeys = lhsAndResultCurve.length + rhsCurve.length;
                int currNumKeys = 0;
                var dstKeys = new NativeArray<Keyframe>(maxNumKeys, Allocator.Temp);

                int lhsKeyCurr = 0;
                int rhsKeyCurr = 0;

                while (lhsKeyCurr < lhsCurveKeys.Length || rhsKeyCurr < rhsCurveKeys.Length)
                {
                    // the index is considered invalid once it goes off the end of the array
                    bool lhsValid = lhsKeyCurr < lhsCurveKeys.Length;
                    bool rhsValid = rhsKeyCurr < rhsCurveKeys.Length;

                    // it's actually impossible for lhsKey/rhsKey to be uninitialized, but have to
                    // add initialize here to prevent compiler erros
                    var lhsKey = new Keyframe();
                    var rhsKey = new Keyframe();
                    if (lhsValid && rhsValid)
                    {
                        lhsKey = GetKeyframeAndClampEdge(lhsCurveKeys, lhsKeyCurr);
                        rhsKey = GetKeyframeAndClampEdge(rhsCurveKeys, rhsKeyCurr);

                        if (lhsKey.time == rhsKey.time)
                        {
                            lhsKeyCurr++;
                            rhsKeyCurr++;
                        }
                        else if (lhsKey.time < rhsKey.time)
                        {
                            // in this case:
                            //     rhsKey[curr-1].time <= lhsKey.time <= rhsKey[curr].time
                            // so interpolate rhsKey at the lhsKey.time.
                            rhsKey = KeyframeUtility.EvalKeyAtTime(rhsCurveKeys, rhsKeyCurr - 1, rhsKeyCurr, startTime, endTime, lhsKey.time);
                            lhsKeyCurr++;
                        }
                        else
                        {
                            // only case left is (lhsKey.time > rhsKey.time)
                            Assert.IsTrue(lhsKey.time > rhsKey.time);

                            // this is the reverse of the lhs key case
                            //     lhsKey[curr-1].time <= rhsKey.time <= lhsKey[curr].time
                            // so interpolate lhsKey at the rhsKey.time.
                            lhsKey = KeyframeUtility.EvalKeyAtTime(lhsCurveKeys, lhsKeyCurr - 1, lhsKeyCurr, startTime, endTime, rhsKey.time);
                            rhsKeyCurr++;
                        }
                    }
                    else if (lhsValid)
                    {
                        // we are still processing lhsKeys, but we are out of rhsKeys, so increment lhs and evaluate rhs
                        lhsKey = GetKeyframeAndClampEdge(lhsCurveKeys, lhsKeyCurr);

                        // rhs will be evaluated between the last rhs key and the extrapolated rhs key at the end time
                        rhsKey = KeyframeUtility.EvalKeyAtTime(rhsCurveKeys, rhsKeyCurr - 1, rhsKeyCurr, startTime, endTime, lhsKey.time);

                        lhsKeyCurr++;
                    }
                    else
                    {
                        // either lhsValid is True, rhsValid is True, or they are both True. So to miss the first two cases,
                        // right here rhsValid must be true.
                        Assert.IsTrue(rhsValid);

                        // we still have rhsKeys to lerp, but we are out of lhsKeys, to increment rhs and evaluate lhs
                        rhsKey = GetKeyframeAndClampEdge(rhsCurveKeys, rhsKeyCurr);

                        // lhs will be evaluated between the last lhs key and the extrapolated lhs key at the end time
                        lhsKey = KeyframeUtility.EvalKeyAtTime(lhsCurveKeys, lhsKeyCurr - 1, lhsKeyCurr, startTime, endTime, rhsKey.time);

                        rhsKeyCurr++;
                    }

                    var dstKey = KeyframeUtility.LerpSingleKeyframe(lhsKey, rhsKey, t);
                    dstKeys[currNumKeys] = dstKey;
                    currNumKeys++;
                }

                // Replace the keys in lhsAndResultCurve with our interpolated curve.
                KeyframeUtility.ResetAnimationCurve(lhsAndResultCurve);
                for (int i = 0; i < currNumKeys; i++)
                {
                    lhsAndResultCurve.AddKey(dstKeys[i]);
                }

                dstKeys.Dispose();
            }
        }
    }
}
