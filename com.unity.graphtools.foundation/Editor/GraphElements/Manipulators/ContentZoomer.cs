using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Changes the <see cref="GraphView"/> zoom when the mouse wheel is rotated over its background.
    /// </summary>
    public class ContentZoomer : Manipulator
    {
        public static readonly float DefaultReferenceScale = 1.0f;
        public static readonly float DefaultMinScale = 0.25f;
        public static readonly float DefaultMaxScale = 1.0f;
        public static readonly float DefaultScaleStep = 0.15f;

        /// <summary>
        /// Scale that should be computed when scroll wheel offset is at zero.
        /// </summary>
        public float referenceScale { get; set; } = DefaultReferenceScale;

        public float minScale { get; set; } = DefaultMinScale;
        public float maxScale { get; set; } = DefaultMaxScale;

        /// <summary>
        /// Relative scale change when zooming in/out (e.g. For 15%, use 0.15).
        /// </summary>
        /// <remarks>
        /// Depending on the values of <c>minScale</c>, <c>maxScale</c> and <c>scaleStep</c>, it is not guaranteed that
        /// the first and last two scale steps will correspond exactly to the value specified in <c>scaleStep</c>.
        /// </remarks>
        public float scaleStep { get; set; } = DefaultScaleStep;

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        // Compute the parameters of our exponential model:
        // z(w) = (1 + s) ^ (w + a) + b
        // Where
        // z: calculated zoom level
        // w: accumulated wheel deltas (1 unit = 1 mouse notch)
        // s: zoom step
        //
        // The factors a and b are calculated in order to satisfy the conditions:
        // z(0) = referenceZoom
        // z(1) = referenceZoom * (1 + zoomStep)
        static float CalculateNewZoom(float currentZoom, float wheelDelta, float zoomStep, float referenceZoom, float minZoom, float maxZoom)
        {
            if (minZoom <= 0)
            {
                Debug.LogError($"The minimum zoom ({minZoom}) must be greater than zero.");
                return currentZoom;
            }
            if (referenceZoom < minZoom)
            {
                Debug.LogError($"The reference zoom ({referenceZoom}) must be greater than or equal to the minimum zoom ({minZoom}).");
                return currentZoom;
            }
            if (referenceZoom > maxZoom)
            {
                Debug.LogError($"The reference zoom ({referenceZoom}) must be less than or equal to the maximum zoom ({maxZoom}).");
                return currentZoom;
            }
            if (zoomStep < 0)
            {
                Debug.LogError($"The zoom step ({zoomStep}) must be greater than or equal to zero.");
                return currentZoom;
            }

            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            if (Mathf.Approximately(wheelDelta, 0))
            {
                return currentZoom;
            }

            // Calculate the factors of our model:
            double a = Math.Log(referenceZoom, 1 + zoomStep);
            double b = referenceZoom - Math.Pow(1 + zoomStep, a);

            // Convert zoom levels to scroll wheel values.
            double minWheel = Math.Log(minZoom - b, 1 + zoomStep) - a;
            double maxWheel = Math.Log(maxZoom - b, 1 + zoomStep) - a;
            double currentWheel = Math.Log(currentZoom - b, 1 + zoomStep) - a;

            // Except when the delta is zero, for each event, consider that the delta corresponds to a rotation by a
            // full notch. The scroll wheel abstraction system is buggy and incomplete: with a regular mouse, the
            // minimum wheel movement is 0.1 on OS X and 3 on Windows. We can't simply accumulate deltas like these, so
            // we accumulate integers only. This may be problematic with high resolution scroll wheels: many small
            // events will be fired. However, at this point, we have no way to differentiate a high resolution scroll
            // wheel delta from a non-accelerated scroll wheel delta of one notch on OS X.
            wheelDelta = Math.Sign(wheelDelta);
            currentWheel += wheelDelta;

            // Assimilate to the boundary when it is nearby.
            if (currentWheel > maxWheel - 0.5)
            {
                return maxZoom;
            }
            if (currentWheel < minWheel + 0.5)
            {
                return minZoom;
            }

            // Snap the wheel to the unit grid.
            currentWheel = Math.Round(currentWheel);

            // Do not assimilate again. Otherwise, points as far as 1.5 units away could be stuck to the boundary
            // because the wheel delta is either +1 or -1.

            // Calculate the corresponding zoom level.
            return (float)(Math.Pow(1 + zoomStep, currentWheel + a) + b);
        }

        /// <summary>
        /// Callback for the WheelEvent.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnWheel(WheelEvent evt)
        {
            var graphView = target as GraphView;
            if (graphView == null)
                return;

            IPanel panel = (evt.target as VisualElement)?.panel;
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            Vector3 position = graphView.ViewTransform.position;
            Vector3 scale = graphView.ViewTransform.scale;

            // TODO: augment the data to have the position as well, so we don't have to read in data from the target.
            // 0-1 ranged center relative to size
            Vector2 zoomCenter = target.ChangeCoordinatesTo(graphView.ContentViewContainer, evt.localMousePosition);
            float x = zoomCenter.x + graphView.ContentViewContainer.layout.x;
            float y = zoomCenter.y + graphView.ContentViewContainer.layout.y;

            position += Vector3.Scale(new Vector3(x, y, 0), scale);

            // Apply the new zoom.
            float zoom = CalculateNewZoom(scale.y, -evt.delta.y, scaleStep, referenceScale, minScale, maxScale);
            scale.x = zoom;
            scale.y = zoom;
            scale.z = 1;

            position -= Vector3.Scale(new Vector3(x, y, 0), scale);
            position.x = GraphViewStaticBridge.RoundToPixelGrid(position.x);
            position.y = GraphViewStaticBridge.RoundToPixelGrid(position.y);

            graphView.Dispatch(new ReframeGraphViewCommand(position, scale));

            evt.StopPropagation();
        }
    }
}
