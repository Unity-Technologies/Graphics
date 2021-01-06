// Part of this code has been given by Christoph Peter in the Demo code of the "Improved Moment Shadow Maps for Translucent Occluders, Soft Shadows and Single Scattering" paper
//
// To the extent possible under law, the author(s) have dedicated all copyright and
// related and neighboring rights to this software to the public domain worldwide.
// This software is distributed without any warranty.
//
// You should have received a copy of the CC0 Public Domain Dedication along with
// this software. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
// Link: http://jcgt.org/published/0006/01/03/

// Given that the default texture2d type matches a float4, we beed to manually declare this texture for the improved moment shadow algorithm
Texture2D<uint4> _SummedAreaTableInputInt;

float2 ComputeFixedPrecision(float3x2 kernelSizeParameter, float2 shadowmapSize)
{
    float2 maxKernelSize = ceil(1.0f+2.0f * kernelSizeParameter._21_22 * shadowmapSize);
    float fixedPres = 4294967295.0f/(maxKernelSize.x*maxKernelSize.y);
    return float2(fixedPres, 1.0 / fixedPres);
}

// Soft IMS
float4 ComputeIntegerRectangleAverage_uint4(float4 searchRegion, float2 shadowTextureSize, float invFixedPrecision)
{
    // Pixel position of the range
    int2 topPixel = int2(searchRegion.xy * shadowTextureSize.xy) - 1;
    int2 bottomPixel = int2(searchRegion.zw * shadowTextureSize.xy) - 1;
    uint2 reactangleSize = bottomPixel - topPixel;

    // Evaluate the integral
    uint4 integral = uint4(0,0,0,0);
    integral += _SummedAreaTableInputInt.Load(int3(topPixel.x, topPixel.y,0));
    integral += _SummedAreaTableInputInt.Load(int3(bottomPixel.x, bottomPixel.y,0));
    integral -= _SummedAreaTableInputInt.Load(int3(topPixel.x, bottomPixel.y,0));
    integral -= _SummedAreaTableInputInt.Load(int3(bottomPixel.x, topPixel.y,0));
    return float4(integral) * invFixedPrecision / float(reactangleSize.x * reactangleSize.y);
}

float2 GetRoots(float3 Coefficients)
{
    float Scaling=1.0f/Coefficients[2];
    float p=Coefficients[1]*Scaling;
    float q=Coefficients[0]*Scaling;
    float D=p*p*0.25f-q;
    float r=sqrt(D);
    return float2(-0.5f*p-r,-0.5f*p+r);
}

void Compute4MomentAverageBlockerDepth(out float OutAverageBlockerDepth, out float OutBlockerSearchShadowIntensity, float depthBias, float4 BlockerSearchBiased4Moments,float FragmentDepth)
{
    // Use short-hands for the many formulae to come
    float4 b=BlockerSearchBiased4Moments;
    float3 z;
    z[0]=FragmentDepth - depthBias;

    // Compute a Cholesky factorization of the Hankel matrix B storing only non-
    // trivial entries or related products
    float L21D11=mad(-b[0],b[1],b[2]);
    float D11=mad(-b[0],b[0], b[1]);
    float SquaredDepthVariance=mad(-b[1],b[1], b[3]);
    float D22D11=dot(float2(SquaredDepthVariance,-L21D11),float2(D11,L21D11));
    float InvD11=1.0f/D11;
    float L21=L21D11*InvD11;

    // Obtain a scaled inverse image of bz=(1,z[0],z[0]*z[0])^T
    float3 c=float3(1.0f,z[0],z[0]*z[0]);
    // Forward substitution to solve L*c1=bz
    c[1]-=b.x;
    c[2]-=b.y+L21*c[1];
    // Scaling to solve D*c2=c1
    c[1]*=InvD11;
    c[2]*=D11/D22D11;
    // Backward substitution to solve L^T*c3=c2
    c[1]-=L21*c[2];
    c[0]-=dot(c.yz,b.xy);
    // Solve the quadratic equation c[0]+c[1]*z+c[2]*z^2 to obtain solutions
    // z[1] and z[2]
    z.yz=GetRoots(c);
    // Compute weights of the Dirac-deltas at the roots
    float3 Weight;
    Weight[0]=(z[1]*z[2]-b[0]*(z[1]+z[2])+b[1])/((z[0]-z[1])*(z[0]-z[2]));
    Weight[1]=(z[0]*z[2]-b[0]*(z[0]+z[2])+b[1])/((z[2]-z[1])*(z[0]-z[1]));
    Weight[2]=1.0f-Weight[0]-Weight[1];
    // Compute the shadow intensity and the unnormalized average depth of occluders
    float AverageBlockerDepthIntegral=((z[1]<z[0])?(Weight[1]*z[1]):0.0f)+((z[2]<z[0])?(Weight[2]*z[2]):0.0f);
    float BlockerSearchShadowIntensity=((z[1]<z[0])?Weight[1]:0.0f)+((z[2]<z[0])?Weight[2]:0.0f);
    const float FullyLitBias=1.0e-3f;
    OutAverageBlockerDepth=clamp((FullyLitBias*z[0]+AverageBlockerDepthIntegral)/(FullyLitBias+BlockerSearchShadowIntensity),-1.0f,1.0f);
    OutBlockerSearchShadowIntensity=(BlockerSearchShadowIntensity>0.99f)?1.0f:(-1.0f);
}

void EstimatePenumbraSize(out float2 OutKernelSize,out float OutDepthBias, float OccluderDepth, float FragmentDepth, float3x2 LightParameter, float3x2 KernelSizeParameter, float MaxDepthBias)
{
    float2 Numerator=LightParameter._11_12*(FragmentDepth-OccluderDepth);
    float2 Denominator=LightParameter._21_22*OccluderDepth+LightParameter._31_32;
    OutKernelSize=max(KernelSizeParameter._11_12,min(KernelSizeParameter._21_22,Numerator/Denominator));
    OutDepthBias=MaxDepthBias*clamp(OutKernelSize.x*KernelSizeParameter._31,KernelSizeParameter._32,1.0f);
}

float4 ComputeRectangleAverage_uint4(float2 LeftTop,float2 RightBottom, float4 TextureSize, float2 FixedPrecision)
{
    // The summed area table is essentially off by one because the top left texel
    // already holds the integral over the top left texel. Compensate for that.
    LeftTop-=TextureSize.zw;
    RightBottom-=TextureSize.zw;
    // Round the texture coordinates to integer texels
    float2 LeftTopPixel=LeftTop*TextureSize.xy;
    float2 RightBottomPixel=RightBottom*TextureSize.xy;
    float2 LeftTopPixelFloor=floor(LeftTopPixel);
    float2 RightBottomPixelFloor=floor(RightBottomPixel);
    int2 iLeftTop=int2(LeftTopPixelFloor);
    int2 iRightBottom=int2(RightBottomPixelFloor);
    float2 LeftTopFactor=float2(1.0f,1.0f)-(LeftTopPixel-LeftTopPixelFloor);
    float2 RightBottomFactor=RightBottomPixel-RightBottomPixelFloor;
    // Sample the summed area table at all relevant locations. The first two indices
    // determine whether we are at the left top or right bottom of the rectangle,
    // the latter two determine the pixel offset.
    int x, y;
    uint4 Samples[2][2][2][2];
    [unroll] for(x=0;x!=2;++x){
        int TexelX=(x==0)?iLeftTop.x:iRightBottom.x;
        [unroll] for(y=0;y!=2;++y){
            int TexelY=(y==0)?iLeftTop.y:iRightBottom.y;
            [unroll] for(int z=0;z!=2;++z){
                [unroll] for(int w=0;w!=2;++w){
                    Samples[x][y][z][w] = _SummedAreaTableInputInt.Load(int3(TexelX+z,TexelY+w,0));
                }
            }
        }
    }
    // Compute integrals for various rectangles
    float4 pCornerIntegral[2][2];
    [unroll] for(x=0;x!=2;++x){
        [unroll] for(y=0;y!=2;++y){
            pCornerIntegral[x][y]=float4(Samples[x][y][0][0]+Samples[x][y][1][1]-Samples[x][y][1][0]-Samples[x][y][0][1]);
        }
    }
    float4 pEdgeIntegral[4]={
        // Right edge
        float4(Samples[1][0][0][1]+Samples[1][1][1][0]-Samples[1][0][1][1]-Samples[1][1][0][0]),
        // Top edge
        float4(Samples[0][0][1][0]+Samples[1][0][0][1]-Samples[0][0][1][1]-Samples[1][0][0][0]),
        // Left edge
        float4(Samples[0][0][0][1]+Samples[0][1][1][0]-Samples[0][0][1][1]-Samples[0][1][0][0]),
        // Bottom edge
        float4(Samples[0][1][1][0]+Samples[1][1][0][1]-Samples[0][1][1][1]-Samples[1][1][0][0])
    };
    float4 CenterIntegral=float4(Samples[0][0][1][1]+Samples[1][1][0][0]-Samples[0][1][1][0]-Samples[1][0][0][1]);
    // Compute the integral over the given rectangle
    float4 Integral=CenterIntegral;
    Integral+=pCornerIntegral[0][0]*(LeftTopFactor.x*LeftTopFactor.y);
    Integral+=pCornerIntegral[0][1]*(LeftTopFactor.x*RightBottomFactor.y);
    Integral+=pCornerIntegral[1][0]*(RightBottomFactor.x*LeftTopFactor.y);
    Integral+=pCornerIntegral[1][1]*(RightBottomFactor.x*RightBottomFactor.y);
    Integral+=pEdgeIntegral[0]*RightBottomFactor.x;
    Integral+=pEdgeIntegral[1]*LeftTopFactor.y;
    Integral+=pEdgeIntegral[2]*LeftTopFactor.x;
    Integral+=pEdgeIntegral[3]*RightBottomFactor.y;
    // Get from a non-normalized integral to moments
    float2 Size=RightBottomPixel-LeftTopPixel;
    return Integral*FixedPrecision.y/(Size.x*Size.y);
}

void Convert4MomentOptimizedToCanonical(out float4 OutBiased4Moments,float4 OptimizedMoments0,float MomentBias=6.0e-5f){
    OutBiased4Moments.xz=mul(OptimizedMoments0.xz-0.5f,float2x2(-1.0f/3.0f,-0.75f,sqrt(3.0f),0.75f*sqrt(3.0f)));
    OutBiased4Moments.yw=mul(OptimizedMoments0.yw,float2x2(0.125f,-0.125f,1.0f,1.0f));
    OutBiased4Moments=lerp(OutBiased4Moments,float4(0.0f,0.628f,0.0f,0.628f),MomentBias);
}

void Compute4MomentUnboundedShadowIntensity(out float OutShadowIntensity,
    float4 Biased4Moments,float FragmentDepth,float DepthBias)
{
    // Use short-hands for the many formulae to come
    float4 b=Biased4Moments;
    float3 z;
    z[0]=FragmentDepth-DepthBias;

    // Compute a Cholesky factorization of the Hankel matrix B storing only non-
    // trivial entries or related products
    float L21D11=mad(-b[0],b[1],b[2]);
    float D11=mad(-b[0],b[0], b[1]);
    float SquaredDepthVariance=mad(-b[1],b[1], b[3]);
    float D22D11=dot(float2(SquaredDepthVariance,-L21D11),float2(D11,L21D11));
    float InvD11=1.0f/D11;
    float L21=L21D11*InvD11;
    float D22=D22D11*InvD11;
    float InvD22=1.0f/D22;

    // Obtain a scaled inverse image of bz=(1,z[0],z[0]*z[0])^T
    float3 c=float3(1.0f,z[0],z[0]*z[0]);
    // Forward substitution to solve L*c1=bz
    c[1]-=b.x;
    c[2]-=b.y+L21*c[1];
    // Scaling to solve D*c2=c1
    c[1]*=InvD11;
    c[2]*=InvD22;
    // Backward substitution to solve L^T*c3=c2
    c[1]-=L21*c[2];
    c[0]-=dot(c.yz,b.xy);
    // Solve the quadratic equation c[0]+c[1]*z+c[2]*z^2 to obtain solutions
    // z[1] and z[2]
    float InvC2=1.0f/c[2];
    float p=c[1]*InvC2;
    float q=c[0]*InvC2;
    float D=(p*p*0.25f)-q;
    float r=sqrt(D);
    z[1]=-p*0.5f-r;
    z[2]=-p*0.5f+r;
    // Compute the shadow intensity by summing the appropriate weights
    float4 Switch=
        (z[2]<z[0])?float4(z[1],z[0],1.0f,1.0f):(
        (z[1]<z[0])?float4(z[0],z[1],0.0f,1.0f):
        float4(0.0f,0.0f,0.0f,0.0f));
    float Quotient=(Switch[0]*z[2]-b[0]*(Switch[0]+z[2])+b[1])/((z[2]-Switch[1])*(z[0]-z[1]));
    OutShadowIntensity=Switch[2]+Switch[3]*Quotient;
    OutShadowIntensity=saturate(OutShadowIntensity);
}

float3x2 ComputeDirectionalLightSoftShadowParameters(float4x4 ViewToProjectionSpace, float LightSourceAngle)
{
    // Compute the bounding box dimensions of the view frustum of the shadow map
    float3 FrustumExtents=float3(
        2.0f*(ViewToProjectionSpace._11*ViewToProjectionSpace._44-ViewToProjectionSpace._14*ViewToProjectionSpace._41)/
             (ViewToProjectionSpace._11*ViewToProjectionSpace._11-ViewToProjectionSpace._41*ViewToProjectionSpace._41),
        2.0f*(ViewToProjectionSpace._22*ViewToProjectionSpace._44-ViewToProjectionSpace._24*ViewToProjectionSpace._42)/
             (ViewToProjectionSpace._22*ViewToProjectionSpace._22-ViewToProjectionSpace._42*ViewToProjectionSpace._42),
        dot(float4(ViewToProjectionSpace._43,-ViewToProjectionSpace._43,-ViewToProjectionSpace._33,ViewToProjectionSpace._33),float4(ViewToProjectionSpace._33,ViewToProjectionSpace._43,ViewToProjectionSpace._34,ViewToProjectionSpace._44))/
            (ViewToProjectionSpace._33*(ViewToProjectionSpace._33-ViewToProjectionSpace._43))
    );
    // Compute a factor that turns a depth difference into a kernel size as texture
    // coordinate
    float3x2 OutLightParameter;
    OutLightParameter._11=0.5f*tan(0.5f*LightSourceAngle)*FrustumExtents.z/FrustumExtents.x;
    OutLightParameter._12=0.5f*tan(0.5f*LightSourceAngle)*FrustumExtents.z/FrustumExtents.y;
    // The denominator is constant one
    OutLightParameter._21_22=float2(0.0f,0.0f);
    OutLightParameter._31_32=float2(1.0f,1.0f);
    return OutLightParameter;
}

// Minimal kernel size for our system
#define MIN_KERNEL_SIZE 1.0

float3x2 ComputeKernelSizePameter(float kernelSize, float2 texResolution)
{
    float3x2 result;
    result._11 = MIN_KERNEL_SIZE/ texResolution.x;
    result._12 = MIN_KERNEL_SIZE/ texResolution.y;
    result._21 = kernelSize * 0.5f / texResolution.x;
    result._22 = kernelSize * 0.5f / texResolution.y;
    result._31 = texResolution.x / max(0.5f * kernelSize - 0.5f, 0.5f);
    result._32 = 0.1f;
    return result;
}

float4x4 GetProjectionMatrix(HDShadowData sd)
{
    float4x4 proj;
    proj = 0.0;
    proj._m00 = sd.proj[0];
    proj._m11 = sd.proj[1];
    proj._m22 = sd.proj[2];
    proj._m23 = sd.proj[3];
    proj._m33 = 1.0;
    return  proj;
}

#define MOMENT_BIAS 6e-7

float SampleShadow_IMS(HDShadowData sd, float3 tcs, float depthBias, float kernelSize, float lightAngle, float maxDepthBias)
{
    // Depth value in the [-1, 1]
    float pixelShadowmapDepth = (1.0 - tcs.z) * 2.0f - 1.0f;

    // Compute the kernel size parameters (this should be done on the scripting side and injected, but for the moment we do not have
    // enough constant buffer params). The code for this was retro-engineered from the demo that the paper gives.
    float3x2 kernelSizeParameter = ComputeKernelSizePameter(kernelSize, _CascadeShadowAtlasSize.xy);

    // Compute the inverse fixed precision value (that allows us to move from uint precision to float)
    float2 fixedPrecision = ComputeFixedPrecision(kernelSizeParameter, _CascadeShadowAtlasSize.xy);

    // The blocker search rectangle is in texCoord space [minShadowmapUV, maxShadowmapUV]
    // XY matches the top left kernel position in texCoords
    // ZW matches the bottom right kernel position in texCoords
    float4 searchRegion = float4(0.0, 0.0, 0.0, 0.0);
    searchRegion.xy = saturate(tcs.xy - kernelSizeParameter._21_22 - 0.5f * _CascadeShadowAtlasSize.zw);
    searchRegion.zw = saturate(tcs.xy + kernelSizeParameter._21_22 + 0.5f * _CascadeShadowAtlasSize.zw);

    // Compute the integral in the target rectangle
    float4 averageOptimizedMoment = ComputeIntegerRectangleAverage_uint4(searchRegion, _CascadeShadowAtlasSize.xy, fixedPrecision.y);

    // Convert the moment to canonical
    Convert4MomentOptimizedToCanonical(averageOptimizedMoment, averageOptimizedMoment, MOMENT_BIAS);

    // Compute the average depth and the shadow intensity
    float averageBlockerDepth = 0.0f;
    float blockerSearchShadowIntensity = 0.0f;
    Compute4MomentAverageBlockerDepth(averageBlockerDepth, blockerSearchShadowIntensity, depthBias, averageOptimizedMoment, pixelShadowmapDepth);

    // Compute the view projection matrix
    float4x4 viewToProjection = GetProjectionMatrix(sd);

    // Let's compute the light parameters
    float3x2 lightParameters = ComputeDirectionalLightSoftShadowParameters(viewToProjection, lightAngle);

    // Estimate the size of the penumbra for this pixel
    float2 penumbraKernelSize = float2(0.0, 0.0);
    float estimatedDepthBias = 0.0f;
    EstimatePenumbraSize(penumbraKernelSize, estimatedDepthBias, averageBlockerDepth, pixelShadowmapDepth, lightParameters, kernelSizeParameter, maxDepthBias);

    // Estimate the region of the filter with the penumbra estimation
    float4 filterRegion = float4(0.0, 0.0, 0.0, 0.0);
    filterRegion.xy = saturate(tcs.xy - penumbraKernelSize);
    filterRegion.zw = saturate(tcs.xy + penumbraKernelSize);

    float4 optimizedMoment = ComputeRectangleAverage_uint4(filterRegion.xy, filterRegion.zw, _CascadeShadowAtlasSize, fixedPrecision.y);

    // Optimized to regular moments
    float4 biasedMoment = float4(0.0, 0.0, 0.0, 0.0);
    Convert4MomentOptimizedToCanonical(biasedMoment, optimizedMoment, MOMENT_BIAS);

    // Compute the shadow intensity
    float shadowIntensity = 0.0f;
    Compute4MomentUnboundedShadowIntensity(shadowIntensity, biasedMoment, pixelShadowmapDepth, estimatedDepthBias);

    // Inverse it and we are done
    return 1.0 - shadowIntensity;
}
