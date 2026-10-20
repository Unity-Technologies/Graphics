#if USING_PHYSICS2D
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// UUM-147033: a ShadowCaster2D whose casting source is a Collider2D must keep one bounding sphere
    /// whichever camera rendered last.
    ///
    /// Every camera shares the caster's shadow mesh, and ShadowShape2DProvider_Collider2D culls that mesh
    /// to the bounds of the camera being rendered. ShadowCaster2D.Update used to size the bounding sphere
    /// from the culled mesh, and IsLit tests lights against that sphere. So after a small camera (the
    /// Game view, or the Inspector preview camera) the sphere shrank, and the next view skipped the shadow
    /// for lights outside it. In the Editor this showed as shadows that flicker between views.
    /// </summary>
    [TestFixture]
    class ShadowCaster2DColliderCullingTests
    {
        const float k_Tolerance = 0.01f;

        GameObject m_GameObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        static Vector2[] Box(float centerX, float centerY)
        {
            return new[]
            {
                new Vector2(centerX - 1, centerY - 1), new Vector2(centerX + 1, centerY - 1),
                new Vector2(centerX + 1, centerY + 1), new Vector2(centerX - 1, centerY + 1)
            };
        }

        // Two separate boxes, like the far ends of a tilemap collider. Two shapes are needed, because the
        // provider culls whole shapes, so a single box is never trimmed.
        static PolygonCollider2D AddTwoBoxCollider(GameObject gameObject, float leftX, float rightX)
        {
            var collider = gameObject.AddComponent<PolygonCollider2D>();
            collider.pathCount = 2;
            collider.SetPath(0, Box(leftX, 0));
            collider.SetPath(1, Box(rightX, 0));
            return collider;
        }

        static ShadowCaster2D AddColliderCaster(GameObject gameObject, Component collider)
        {
            var caster = gameObject.AddComponent<ShadowCaster2D>();
            caster.shadowCastingSource = ShadowCaster2D.ShadowCastingSources.ShapeProvider;
            caster.shadowShape2DComponent = collider;
            caster.shadowShape2DProvider = new ShadowShape2DProvider_Collider2D();
            caster.m_ForceShadowMeshRebuild = true;
            caster.Update();
            return caster;
        }

        static void RenderWithCullingBounds(ShadowCaster2D caster, Bounds cullingBounds)
        {
            ShapeProviderUtility.CallOnBeforeRender(caster.shadowShape2DProvider, caster.shadowShape2DComponent, caster.m_ShadowMesh, cullingBounds, null);
            caster.Update();
        }

        static void AssertSameSphere(BoundingSphere expected, BoundingSphere actual)
        {
            Assert.That(actual.radius, Is.EqualTo(expected.radius).Within(k_Tolerance),
                $"The bounding sphere changed with the last camera's culling bounds (center {actual.position}, radius {actual.radius}; " +
                $"expected center {expected.position}, radius {expected.radius}). IsLit then rejects lights the other views still need.");
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(k_Tolerance),
                $"The bounding sphere center moved to {actual.position} (expected {expected.position}).");
        }

        static void AssertCovers(BoundingSphere sphere, Vector2 point)
        {
            var distance = Vector2.Distance(new Vector2(sphere.position.x, sphere.position.y), point);
            Assert.That(distance, Is.LessThanOrEqualTo(sphere.radius + k_Tolerance),
                $"The bounding sphere (center {sphere.position}, radius {sphere.radius}) does not cover {point}.");
        }

        [Test]
        public void ColliderShadowCaster_BoundingSphere_DoesNotDependOnLastCameraCullingBounds()
        {
            m_GameObject = new GameObject("UUM-147033 Caster");
            var collider = AddTwoBoxCollider(m_GameObject, -20, 20);
            var caster = AddColliderCaster(m_GameObject, collider);

            // A camera that sees the whole collider, like a zoomed-out Scene view.
            RenderWithCullingBounds(caster, new Bounds(Vector3.zero, new Vector3(100, 100, 1000)));
            var fullSphere = caster.boundingSphere;

            // A small camera that sees only the left box, like the Game view or the Inspector preview.
            RenderWithCullingBounds(caster, new Bounds(new Vector3(-20, 0, 0), new Vector3(4, 4, 1000)));
            AssertSameSphere(fullSphere, caster.boundingSphere);

            // The sphere covers both boxes. The radius is sqrt(21^2 + 1^2) plus the polygon skin radius.
            Assert.That(Vector2.Distance(fullSphere.position, Vector2.zero), Is.LessThan(k_Tolerance), $"Center is {fullSphere.position}.");
            Assert.That(fullSphere.radius, Is.GreaterThanOrEqualTo(21f));
        }

        [Test]
        public void ColliderShadowCaster_OffsetFromOrigin_BoundingSphereUnchangedWhenViewMissesCollider()
        {
            m_GameObject = new GameObject("UUM-147033 Offset Caster");
            var collider = AddTwoBoxCollider(m_GameObject, 100, 140);
            var caster = AddColliderCaster(m_GameObject, collider);

            // A view at the GameObject origin that sees none of the collider. The provider empties the mesh,
            // but the sphere must still cover both boxes.
            var missView = new Bounds(Vector3.zero, new Vector3(10, 10, 1000));
            RenderWithCullingBounds(caster, missView);
            var missSphere = caster.boundingSphere;
            Assert.That(Vector2.Distance(missSphere.position, new Vector2(120, 0)), Is.LessThan(k_Tolerance), $"Center is {missSphere.position}.");
            Assert.That(missSphere.radius, Is.GreaterThanOrEqualTo(21f));

            RenderWithCullingBounds(caster, new Bounds(new Vector3(120, 0, 0), new Vector3(100, 100, 1000)));
            AssertSameSphere(missSphere, caster.boundingSphere);

            RenderWithCullingBounds(caster, new Bounds(new Vector3(140, 0, 0), new Vector3(4, 4, 1000)));
            AssertSameSphere(missSphere, caster.boundingSphere);

            RenderWithCullingBounds(caster, missView);
            AssertSameSphere(missSphere, caster.boundingSphere);
        }

        [Test]
        public void ColliderShadowCaster_RotatedRigidbody_BoundingSphereCoversRotatedShapes()
        {
            m_GameObject = new GameObject("UUM-147033 Rotated Caster");
            m_GameObject.transform.SetPositionAndRotation(new Vector3(5, 5, 0), Quaternion.Euler(0, 0, 90));
            var body = m_GameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            var collider = AddTwoBoxCollider(m_GameObject, 10, 50);
            var caster = AddColliderCaster(m_GameObject, collider);

            RenderWithCullingBounds(caster, new Bounds(new Vector3(5, 35, 0), new Vector3(100, 100, 1000)));
            var fullSphere = caster.boundingSphere;

            // Rotating by 90 degrees about (5,5) moves the boxes at local x=10 and x=50 to world (5,15) and (5,55).
            foreach (var center in new[] { new Vector2(5, 15), new Vector2(5, 55) })
                foreach (var corner in Box(center.x, center.y))
                    AssertCovers(fullSphere, corner);

            // A small view on the lower box only.
            RenderWithCullingBounds(caster, new Bounds(new Vector3(5, 15, 0), new Vector3(4, 4, 1000)));
            AssertSameSphere(fullSphere, caster.boundingSphere);
        }

        [Test]
        public void ColliderShadowCaster_DisabledOrEmptyCollider_FallsBackWithoutExceptions()
        {
            m_GameObject = new GameObject("UUM-147033 Disabled Caster");
            var collider = AddTwoBoxCollider(m_GameObject, -20, 20);
            var caster = AddColliderCaster(m_GameObject, collider);
            var provider = caster.shadowShape2DProvider;
            var wholeView = new Bounds(Vector3.zero, new Vector3(100, 100, 1000));
            RenderWithCullingBounds(caster, wholeView);
            Assert.That(provider.TryGetWorldBounds(collider, out _), Is.True, "An enabled collider with shapes gives bounds.");

            collider.enabled = false;
            Assert.DoesNotThrow(() => RenderWithCullingBounds(caster, wholeView));
            Assert.That(provider.TryGetWorldBounds(collider, out _), Is.False, "A disabled collider gives no bounds.");

            // Re-enabled with no shapes, like a TilemapCollider2D merged into a CompositeCollider2D.
            collider.enabled = true;
            collider.pathCount = 0;
            Assert.DoesNotThrow(() => RenderWithCullingBounds(caster, wholeView));
            Assert.That(provider.TryGetWorldBounds(collider, out _), Is.False, "A collider with no shapes gives no bounds.");

            // Shapes come back: the bounds are read again, not reused from before.
            collider.pathCount = 1;
            collider.SetPath(0, Box(60, 0));
            Assert.DoesNotThrow(() => RenderWithCullingBounds(caster, new Bounds(new Vector3(60, 0, 0), new Vector3(100, 100, 1000))));
            Assert.That(provider.TryGetWorldBounds(collider, out var bounds), Is.True);
            Assert.That(Vector2.Distance(bounds.center, new Vector2(60, 0)), Is.LessThan(k_Tolerance), $"Bounds center is {bounds.center}.");
            Assert.That(Vector2.Distance(caster.boundingSphere.position, new Vector2(60, 0)), Is.LessThan(k_Tolerance));
        }

        [Test]
        public void ColliderShadowCaster_NullProviderOrComponent_DoesNotThrow()
        {
            m_GameObject = new GameObject("UUM-147033 Null Caster");
            var collider = AddTwoBoxCollider(m_GameObject, -20, 20);
            var caster = AddColliderCaster(m_GameObject, collider);

            caster.shadowShape2DComponent = null;
            Assert.DoesNotThrow(() => caster.Update(), "Null component");

            caster.shadowShape2DComponent = collider;
            caster.shadowShape2DProvider = null;
            Assert.DoesNotThrow(() => caster.Update(), "Null provider");

            caster.shadowShape2DComponent = null;
            Assert.DoesNotThrow(() => caster.Update(), "Null provider and component");
        }
    }
}
#endif
