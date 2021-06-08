using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class HDRPShowLightCluster : MonoBehaviour
{
    [SerializeField]
    Camera cam;

    const int clusterSize = 8;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        int rows = DivRoundUp((int)cam.pixelWidth, 32);
        int cols = DivRoundUp((int)cam.pixelHeight, 32);

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                // Gizmos.DrawFrustum();
            }
        }
    }

#if UNITY_EDITOR
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForMyScript(HDRPShowLightCluster comp, GizmoType gizmoType)
    {
	    var cam = comp.cam;
        DrawFrustum(cam);
    }
#endif

    static readonly Color widthColor = new Color(1, 0, 0, 0.4f);
    static readonly Color heightColor = new Color(0, 1, 0, 0.1f);
    static readonly Color depthColor = new Color(0, 0, 1, 0.4f);

    static void DrawFrustum ( Camera cam ) {

        int rows = DivRoundUp((int)cam.pixelWidth, 32);
        int cols = DivRoundUp((int)cam.pixelHeight, 32);
        var t = cam.transform;

        Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
        Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes( cam ); //get planes from matrix
        Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

        for ( int i = 0; i < 4; i++ ) {
            nearCorners[i] = Plane3Intersect( camPlanes[4], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //near corners on the created projection matrix
            farCorners[i] = Plane3Intersect( camPlanes[5], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //far corners on the created projection matrix
        }

        // Outside lines
        float camNearPlane = cam.nearClipPlane;
        float camFarPlane = cam.farClipPlane;
        float zCount = 64;
        float s0 = camNearPlane, s1 = (camFarPlane - camNearPlane) / zCount;
        for (int z = 0; z < zCount; z++)
        {
            cam.nearClipPlane = s0;
            cam.farClipPlane = cam.nearClipPlane + s1;

            DrawFrustum2(cam);

            s0 += s1;
        }
        cam.nearClipPlane = camNearPlane;
        cam.farClipPlane = camFarPlane;

        // zCount lines
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                float nw = Vector3.Distance(nearCorners[0], nearCorners[1]);
                float fw = Vector3.Distance(farCorners[0], farCorners[1]);
                float nh = Vector3.Distance(nearCorners[1], nearCorners[2]);
                float fh = Vector3.Distance(farCorners[1], farCorners[2]);

                // for ( int i = 0; i < 4; i++ )
                {
                    Vector3 n = nearCorners[0] + t.right * (x / (float)rows) * nw + t.up * (y / (float)cols) * nh;
                    Vector3 f = farCorners[0] + t.right * (x / (float)rows) * fw + t.up * (y / (float)cols) * fh;

                    Debug.DrawLine( n, f, heightColor, 0, true ); //sides of the created projection matrix
                }
            }
        }
    }

    static void DrawFrustum2(Camera cam)
    {
        Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
        Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes( cam ); //get planes from matrix
        Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

        for ( int i = 0; i < 4; i++ ) {
            nearCorners[i] = Plane3Intersect( camPlanes[4], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //near corners on the created projection matrix
            farCorners[i] = Plane3Intersect( camPlanes[5], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //far corners on the created projection matrix
        }

        for ( int i = 0; i < 4; i++ ) {
            Debug.DrawLine( nearCorners[i], nearCorners[( i + 1 ) % 4], widthColor, 0, true ); //near corners on the created projection matrix
            Debug.DrawLine( farCorners[i], farCorners[( i + 1 ) % 4], depthColor, 0, true ); //far corners on the created projection matrix
            Debug.DrawLine( nearCorners[i], farCorners[i], heightColor, 0, true ); //sides of the created projection matrix
        }
    }
 
    static Vector3 Plane3Intersect ( Plane p1, Plane p2, Plane p3 ) { //get the intersection point of 3 planes
        return ( ( -p1.distance * Vector3.Cross( p2.normal, p3.normal ) ) +
                ( -p2.distance * Vector3.Cross( p3.normal, p1.normal ) ) +
                ( -p3.distance * Vector3.Cross( p1.normal, p2.normal ) ) ) /
            ( Vector3.Dot( p1.normal, Vector3.Cross( p2.normal, p3.normal ) ) );
    }

    static int DivRoundUp(int x, int y) => (x + y - 1) / y;
}
