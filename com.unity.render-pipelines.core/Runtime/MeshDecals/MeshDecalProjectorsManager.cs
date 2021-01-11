using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Rendering.MeshDecal
{
    [ExecuteAlways]
    public class MeshDecalProjectorsManager : MonoBehaviour
    {
        static MeshDecalProjectorsManager m_instance;
        public static MeshDecalProjectorsManager instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = FindObjectOfType<MeshDecalProjectorsManager>();

                if (m_instance == null)
                    m_instance = new GameObject("Decal Projectors Manager").AddComponent<MeshDecalProjectorsManager>();

                return m_instance;
            }
        }

        public int atlasTextureSize = 4096;
        public Texture2D atlasTexture;

        [ContextMenu("Clear")]
        public void Clear()
        {
            Destroy(atlasTexture);

            atlasTexture = new Texture2D(atlasTextureSize, atlasTextureSize, TextureFormat.RGBA32, true, true);
            atlasTexture.name = "Decals Atlas";
            atlasTexture.hideFlags = HideFlags.DontSave;
        }

        List<MeshDecalProjector> projectors = new List<MeshDecalProjector>();
        bool projectorsListChanged = false;

        public static void RegisterDecalProjector(MeshDecalProjector projector)
        {
            if (!instance.projectors.Contains(projector))
                instance.projectors.Add(projector);
            instance.projectorsListChanged = true;
        }
        public static void UnregisterDecalProjector(MeshDecalProjector projector)
        {
            if (instance.projectors.Contains(projector))
            {
                instance.projectors.Remove(projector);
                instance.projectorsListChanged = true;
            }
        }

        List<MeshDecalProjector> projectorsInView = new List<MeshDecalProjector>();
        Dictionary<Camera, Plane[]> cameraFrustums = new Dictionary<Camera, Plane[]>();

        private void OnEnable()
        {
            Camera.onPreRender += UpdateDecalsInView;

            atlasTexture = new Texture2D(atlasTextureSize, atlasTextureSize, TextureFormat.RGBA32, true, true);
            atlasTexture.name = "Decals Atlas";
            atlasTexture.hideFlags = HideFlags.DontSave;
        }

        void UpdateDecalsInView(Camera cam)
        {
            // FilterDecalsInCamera(cam);

            if (projectorsListChanged)
            {
                projectorsInView = projectors;
                projectorsListChanged = false;

                UpdateAtlas();
                UpdateProjectors();
            }
        }

        void FilterDecalsInCamera(Camera cam)
        {
            bool newCamera = false;

            if (!cameraFrustums.ContainsKey(cam))
            {
                cameraFrustums.Add(cam, new Plane[6]);
                newCamera = true;
            }

            if (cam.transform.hasChanged || newCamera)
            {
                var frustumPoints = new Vector3[8];
                var invProjectionMatrix = cam.projectionMatrix.inverse;
                frustumPoints[0] = invProjectionMatrix * new Vector4(-1, -1, -1, 1);
                frustumPoints[1] = invProjectionMatrix * new Vector4(-1, -1, 1, 1);
                frustumPoints[2] = invProjectionMatrix * new Vector4(-1, 1, -1, 1);
                frustumPoints[3] = invProjectionMatrix * new Vector4(-1, 1, 1, 1);
                frustumPoints[4] = invProjectionMatrix * new Vector4(1, -1, -1, 1);
                frustumPoints[5] = invProjectionMatrix * new Vector4(1, -1, 1, 1);
                frustumPoints[6] = invProjectionMatrix * new Vector4(1, 1, -1, 1);
                frustumPoints[7] = invProjectionMatrix * new Vector4(1, 1, 1, 1);

                var centerPoint = invProjectionMatrix * new Vector4(0, 0, 0, 1);

                var frustumPlanes = cameraFrustums[cam];

                frustumPlanes[0] = new Plane(frustumPoints[0], frustumPoints[1], frustumPoints[2]);
                frustumPlanes[0] = new Plane(frustumPoints[4], frustumPoints[5], frustumPoints[6]);
                frustumPlanes[0] = new Plane(frustumPoints[0], frustumPoints[1], frustumPoints[4]);
                frustumPlanes[0] = new Plane(frustumPoints[2], frustumPoints[3], frustumPoints[6]);
                frustumPlanes[0] = new Plane(frustumPoints[0], frustumPoints[2], frustumPoints[4]);
                frustumPlanes[0] = new Plane(frustumPoints[1], frustumPoints[3], frustumPoints[5]);

                for (int i = 0; i < 6; ++i)
                    if (frustumPlanes[i].GetDistanceToPoint(centerPoint) > 0) frustumPlanes[i].normal = -frustumPlanes[i].normal;

                cameraFrustums[cam] = frustumPlanes;
            }

            projectorsInView = projectors.Where(p => OverlapsFrustum(p, cam)).ToList();

            // Debug.Log($"Projectors in view of {cam.name} : {projectorsInView.Count} .");
        }

        bool OverlapsFrustum(MeshDecalProjector projector, Camera cam)
        {
            bool overlaps = true;
            int i = 0;

            var frustumPlanes = cameraFrustums[cam];

            while (overlaps && i < 6)
            {
                overlaps &= frustumPlanes[i].GetDistanceToPoint(projector.transform.position) < projector.radius;
                ++i;
            }

            return overlaps;
        }

        enum TextureSlot
        {
            Albedo,
            Normal
        }

        List<TextureData> textureDatas = new List<TextureData>();

        struct TextureData
        {
            public Texture2D texture;
            public MeshDecalProjector projector;
            public TextureSlot textureSlot;
            public Vector4 posSize;
        }

        void UpdateAtlas()
        {
            textureDatas.Clear();

            foreach (var p in projectorsInView)
            {
                if (p.albedo != null) textureDatas.Add(new TextureData()
                {
                    texture = p.albedo,
                    projector = p,
                    textureSlot = TextureSlot.Albedo
                });
                if (p.normal != null) textureDatas.Add(new TextureData()
                {
                    texture = p.normal,
                    projector = p,
                    textureSlot = TextureSlot.Normal
                });
            }

            var rects = atlasTexture.PackTextures(textureDatas.Select(d => d.texture).ToArray(), 0, atlasTextureSize, false);

            TextureData t;
            for (int i = 0; i < rects.Length; ++i)
            {
                t = textureDatas[i];
                t.posSize = new Vector4(rects[i].x, rects[i].y, rects[i].width, rects[i].height);
                textureDatas[i] = t;
            }

            atlasTexture.Apply();
        }

        void UpdateProjectors()
        {
            foreach (var p in projectorsInView)
                p.SetAtlasData(atlasTexture);

            foreach (var t in textureDatas)
            {
                string propertyName = "";
                if (t.textureSlot == TextureSlot.Albedo) propertyName = "_AlbedoPosSize";
                if (t.textureSlot == TextureSlot.Normal) propertyName = "_NormalPosSize";

                t.projector.SetAtlasData(propertyName, t.posSize);
            }
        }
    }
}
