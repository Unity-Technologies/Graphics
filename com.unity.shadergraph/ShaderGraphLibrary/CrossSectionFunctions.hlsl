//Packages/com.unity.shadergraph/ShaderGraphLibrary/CrossSectionGlobalCuttingPlane.hlsl
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/CrossSectionVariables.hlsl"

#ifndef CROSS_SECTION_FUNCTIONS_INCLUDED
#define CROSS_SECTION_FUNCTIONS_INCLUDED

// TODO fix inclusions between shadergraph and hdrp
float3 IntersectRayPlane_xs(float3 rayOrigin, float3 rayDirection, float3 planeOrigin, float3 planeNormal)
{
    float dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

void GlobalCuttingPlane_float(out float3 planePosition, out float3 planeTangent, out float3 planeBitangent, out float3 planeNormal,
                              out float cutMainStyle,
                              out bool cutStyleIsTransparent, out bool cutStyleIsHollow, out bool cutStyleIsFilled, out bool cutStyleRequiresCustomShading)
{
    planePosition = _UnityAEC_ClipPlanePosition;
    planeTangent = _UnityAEC_ClipPlaneTangent;
    planeBitangent = _UnityAEC_ClipPlaneBitangent;
    planeNormal = _UnityAEC_ClipPlaneNormal;
    cutMainStyle = _UnityAEC_CutMainStyle;
    // TODO fix inclusions between shadergraph and hdrp and include the .cs.hlsl from CrossSectionDefinitions.cs
    cutStyleIsTransparent = (cutMainStyle == 0);
    cutStyleIsHollow = (cutMainStyle == 1) || (cutMainStyle == 2);
    cutStyleIsFilled = (cutMainStyle == 3);
    cutStyleRequiresCustomShading = (cutMainStyle > 1);
}

// Note:
//
// To offer a "filled cross section" style, we're using backfaces to trick shading at the cutting shape (eg plane) position
// even though the fragment is that actually generate the shading invocation could be far from it (it just wasn't rejected by
// the cutter, and the fragment perspective projection of a backface falls into a pixel that also corresponds to the perspective
// projection on screen of a fragment of the cutting shape itself.
//
// See HashPatternInPlane_float, that's why we manually do a perspective
// ray intersection with the cutter - here a plane - to generate cutting-plane-relative UVs.
//
// Problem with already double-sided enabled shader graphs:
//
// If the original shader graph had back faces enabled, a legitimately visible back faced-generated fragment will be "reshaded"
// by our alternate cross section style, whatever style we chose for the exposed cross section.
//
// A solution to this issue is:
//
// For shader graph materials with such backface enabled and thus potentially attached to materials assigned to geometry that could
// show backfaces, use a different cross section shader configuration, that allow clipping but don't allow any other style than to
// pass through whatever wasn't clipped (ie show the original back face). This also makes sense from the point of view of a material
// having such a double-sided rendering option enabled: it means it is used for thin objects represented by simple polygons (like folliage)
// instead of the mesh representing the limits of a closed hull / volume.
//
// The following node function allows to exposed what was enabled before the cross section subgraph splicing (modification) of the
// original shadergraph, based on the state of the master node that we saved on a property (input) port of our cross section subgraph.
//
// ! Note also that for another shader graph using the same cross section subgraph module to behave differently, use configuration
// signals outside the subgraph (at the scope of the enclosing graph, ie the original shader graph) and connect them to input ports
// on the cross section subgraph module and branch accordingly inside (remember that any modification inside the cross section subgraph
// are shared for all shader graph using the same subgraph). An alternative is to use another (different subgraph guid) cross section
// subgraph for the material that needs a different behavior.
//
void BackFaceWasOriginallyEnabled_float(float doubleSidedSavedState,
                                        out bool doubleSidedWasEnabled, out bool backFaceUsedFlippedNormals, out bool backFaceUsedMirroredNormals)
{
    // TODOTODO: Fix code sharing (especially HLSL including generated from HDRP and duped files!) between shadergraph and HDRP
    if (doubleSidedSavedState <= 0)
    {
        doubleSidedWasEnabled = false;
        backFaceUsedFlippedNormals = false;
        backFaceUsedMirroredNormals = false;
    }
    else
    {
        doubleSidedWasEnabled = true;
        backFaceUsedFlippedNormals = (backFaceUsedFlippedNormals == 2.0);
        backFaceUsedMirroredNormals = (doubleSidedSavedState == 3.0);
    }
}

// See comments above BackFaceWasOriginallyEnabled_float:
// These are suggested signals / behavior for combining the original state of the double sided mode that the master node had, with a
// desired style for the cross section
void BackFacePassingCutterBehaviorControl_float(bool fixArtifactsFromMeshesNotClosed, bool doubleSidedWasEnabled, bool crossSectionStyleIsTransparent,
                                                out bool disableCrossSectionShadingOverride)
{
    // fixArtifactsFromMeshesNotClosed is like saying "Even if the original shader graph material doesn't use double-sided mode, my mesh might
    // have a malformed winding or is note even close and thus can show some backfaces."
    // (Like explained above, normally such faces wouldn't even have been rasterized but we allowed it to show filled cross sections)
    // -> This will produce the cross section appearance when it shouldn't, and thus the only
    // cross section style available for such materials is simply to be transparent
    //
    // Likewise, if doubleSidedWasEnabled, then the material already expects to render backfaces, thus we may presume that the mesh will show
    // some backfaces - ie will have some back faces that can be visible / non occluded from other front faces.
    //
    // So in those two situations, we need to use a cutting behavior like case (a) below:
    disableCrossSectionShadingOverride = (doubleSidedWasEnabled || fixArtifactsFromMeshesNotClosed || crossSectionStyleIsTransparent);

    //
    // Otherwise we have more appearance choices to show an "under a rejected front fragment because of the cutter test failing":
    //
    // a) nothing added: ie transparent style, if the original material had back faces enabled, they will show, otherwise, anything else behind will show
    //    (This is crossSectionStyleIsTransparent)
    //
    // b) cross section cut filled flush with the shape of the cutter (as if a mesh defines a volume)
    // c) cross section cut showing a reshaded hollow interior
    // d) cross section cut showing a hollow interior but with the back faces using the original material
    //
    // In case b) we should override material properties especially normals, and UVs should be generated according to the cutting shape.
    // Cases c) and d) are easier to handle.
}

void HashPatternInPlane_float(float3 fragPosition,
                              float3 planePosition, float3 planeTangent, float3 planeBitangent, float3 planeNormal,
                              float scale, float3 viewWS, float3 cameraPos,
                              out float amplitude, out float2 planeSpacePos)
{
    // Orthogonal projection of the backfacing fragment on the plane: if the front is rejected, this will show.
    // This is creates an hollowed out appearance. (Caveat with the normal used though)
    //float3 tanDirVec = (fragPosition - planePosition);
    //tanDirVec = tanDirVec - dot(tanDirVec, planeNormal)*planeNormal;
    //tanDirVec *= scale;

    // Perspective projection of the backfacing fragment on the plane: for a filled volume appearance of the cut.
    //float3 raydir = normalize(GetPrimaryCameraPosition()-fragPosition);
    //float3 inter = IntersectRayPlane_xs(GetPrimaryCameraPosition(), raydir, planePosition, planeNormal);
    // TODOTODO: GetPrimaryCameraPosition() doesn't work here why, see code gen and includes.
    // SHADEROPTIONS_CAMERA_RELATIVE_RENDERING should be defined at the point where ShaderVariablesFunctions.hlsl
    // is included

    // but we already have that from the vertex stage of course:
    // float3 WorldSpaceViewDirection = _WorldSpaceCameraPos.xyz - mul(GetObjectToWorldMatrix(), float4(v.vertex.xyz, 1.0)).xyz;
    // use it:
    float3 raydir = normalize(viewWS);
    float3 inter = IntersectRayPlane_xs(cameraPos, raydir, planePosition, planeNormal);
    float3 tanDirVec = (inter - planePosition)*scale;
    planeSpacePos = float2(dot(tanDirVec, planeTangent), dot(tanDirVec, planeBitangent));
    amplitude = planeSpacePos.x - 2.0 * floor(planeSpacePos.x / 2.0);
}

void HashHollowPatternInPlane_float(float3 fragPosition,
                                    float3 planePosition, float3 planeTangent, float3 planeBitangent, float3 planeNormal,
                                    float scale,
                                    out float amplitude, out float2 planeSpacePos)
{
    // Orthogonal projection of the backfacing fragment on the plane: if the front is rejected, this will show.
    // This is creates an hollowed out appearance. (Caveat with the normal used though)
    float3 tanDirVec = (fragPosition - planePosition);
    tanDirVec = tanDirVec - dot(tanDirVec, planeNormal)*planeNormal;
    tanDirVec *= scale;
    planeSpacePos = float2(dot(tanDirVec, planeTangent), dot(tanDirVec, planeBitangent));
    amplitude = planeSpacePos.x - 2.0 * floor(planeSpacePos.x / 2.0);
}

void MultiPortBranch2_float(float predicate,
                           float Atrue, float Afalse,
                           float2 Btrue, float2 Bfalse,
                           out float A,
                           out float2 B)
{
    A = Afalse;
    B = Bfalse;
    if (predicate == 1.0)
    {
        A = Atrue;
        B = Btrue;
    }
}


void MultiPortBranch_float(float predicate,
                           float3 Atrue, float3 Afalse,
                           float3 Btrue, float3 Bfalse,
                           float3 Ctrue, float3 Cfalse,
                           float3 Dtrue, float3 Dfalse,
                           float3 Etrue, float3 Efalse,
                           float3 Ftrue, float3 Ffalse,
                           float3 Gtrue, float3 Gfalse,
                           float3 Htrue, float3 Hfalse,
                           float3 Itrue, float3 Ifalse,
                           float3 Jtrue, float3 Jfalse,
                           float3 Ktrue, float3 Kfalse,
                           out float3 A,
                           out float3 B,
                           out float3 C,
                           out float3 D,
                           out float3 E,
                           out float3 F,
                           out float3 G, 
                           out float3 H, 
                           out float3 I, 
                           out float3 J, 
                           out float3 K)
{
    A = Afalse;
    B = Bfalse;
    C = Cfalse;
    D = Dfalse;
    E = Efalse;
    F = Ffalse;
    G = Gfalse;
    H = Hfalse;
    I = Ifalse;
    J = Jfalse;
    K = Kfalse;
    if (predicate == 1.0)
    {
        A = Atrue;
        B = Btrue;
        C = Ctrue;
        D = Dtrue;
        E = Etrue;
        F = Ftrue;
        G = Gtrue;
        H = Htrue;
        I = Itrue;
        J = Jtrue;
        K = Ktrue;
    }
}

#endif // #define CROSS_SECTION_FUNCTIONS_INCLUDED