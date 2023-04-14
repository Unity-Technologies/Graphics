#ifndef IMPOSTERSTART
    #define IMPOSTESTART

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

    struct ImposterData
    {
        float2 uv;
        float2 grid;
        float4 frame0;
        float4 frame1;
        float4 frame2;
        float3 vertex;
        bool hemiCheck;
    };

    struct Ray
    {
        float3 Origin;
        float3 Direction;
    };

    float4 ImposterBlendWeights(Texture2D tex, SamplerState ss, float2 frame0, float2 frame1, float2 frame2, float3 weights, float4 ddxy)
    {
        float4 samp0 = SAMPLE_TEXTURE2D_GRAD(tex, ss, frame0, ddxy.xy, ddxy.zw);
        float4 samp1 = SAMPLE_TEXTURE2D_GRAD(tex, ss, frame1, ddxy.xy, ddxy.zw);
        float4 samp2 = SAMPLE_TEXTURE2D_GRAD(tex, ss, frame2, ddxy.xy, ddxy.zw);

        float4 result = samp0*weights.x + samp1*weights.y + samp2*weights.z;
        
        return result;
    }

    //for hemisphere
    float3 OctaHemiEnc( float2 coord )
    {
        coord = float2( coord.x + coord.y, coord.x - coord.y ) * 0.5;
        float3 vec = float3( coord.x, 1.0 - dot( float2(1.0,1.0), abs(coord.xy) ), coord.y  );
        return vec;
    }

    //for sphere
    float3 OctaSphereEnc( float2 coord )
    {
        float3 vec = float3( coord.x, 1-dot(1,abs(coord)), coord.y );
        if ( vec.y < 0 )
        {
            float2 flip = vec.xz >= 0 ? float2(1,1) : float2(-1,-1);
            vec.xz = (1-abs(vec.zx)) * flip;
        }
        return vec;
    }

    float3 GridToVector( float2 coord )
    {
        float3 vec;
        if (_ImposterIsHalfSphere)
        {
            vec = OctaHemiEnc(coord);
        }
        else
        {
            vec = OctaSphereEnc(coord);
        }
        return vec;
    }

    //for hemisphere
    float2 VecToHemiOct( float3 vec )
    {
        vec.xz /= dot( 1.0, abs(vec) );
        return float2( vec.x + vec.z, vec.x - vec.z );
    }

    float2 VecToSphereOct( float3 vec )
    {
        vec.xz /= dot( 1,  abs(vec) );
        if ( vec.y <= 0 )
        {
            float2 flip = vec.xz >= 0 ? float2(1,1) : float2(-1,-1);
            vec.xz = (1-abs(vec.zx)) * flip;
        }
        return vec.xz;
    }
    //3D to 2D
    float2 VectorToGrid( float3 vec , bool hemiCheck)
    {
        float2 coord;

        if (hemiCheck)
        {
            vec.y = max(0.001,vec.y);
            vec = normalize(vec);
            coord = VecToHemiOct( vec );
        }
        else
        {
            coord = VecToSphereOct( vec );
        }
        return coord;
    }

    float4 TriangleInterpolate( float2 uv )
    {
        uv = frac(uv);

        float2 omuv = float2(1.0,1.0) - uv.xy;
        
        float4 res = float4(0,0,0,0);
        //frame 0
        res.x = min(omuv.x,omuv.y); 
        //frame 1
        res.y = abs( dot( uv, float2(1.0,-1.0) ) );
        //frame 2
        res.z = min(uv.x,uv.y); 
        //mask
        res.w = saturate(ceil(uv.x-uv.y));
        
        return res;
    }

    //frame and framecout, returns 
    float3 FrameXYToRay( float2 frame, float2 frameCountMinusOne )
    {
        //divide frame x y by framecount minus one to get 0-1
        float2 f = frame.xy / frameCountMinusOne;
        //bias and scale to -1 to 1
        f = (f-0.5)*2.0; 
        //convert to vector, either full sphere or hemi sphere
        float3 vec = GridToVector( f );
        vec = normalize(vec);
        return vec;
    }

    float3 ITBasis( float3 vec, float3 basedX, float3 basedY, float3 basedZ )
    {
        return float3( dot(basedX,vec), dot(basedY,vec), dot(basedZ,vec) );
    }
    
    float3 FrameTransform( float3 projRay, float3 frameRay, out float3 worldX, out float3 worldZ  )
    {
        //TODO something might be wrong here
        worldX = normalize( float3(-frameRay.z, 0, frameRay.x) );
        worldZ = normalize( cross(worldX, frameRay ) ); 
        
        projRay *= -1.0; 
        
        float3 local = normalize( ITBasis( projRay, worldX, frameRay, worldZ ) );
        return local;
    }

    float2 VirtualPlaneUV( float3 planeNormal, float3 planeX, float3 planeZ, float3 center, float2 uvScale, Ray rayLocal )
    {
        float normalDotOrigin = dot(planeNormal,rayLocal.Origin);
        float normalDotCenter = dot(planeNormal,center);
        float normalDotRay = dot(planeNormal,rayLocal.Direction);
        
        float planeDistance = normalDotOrigin-normalDotCenter;
        planeDistance *= -1.0;
        
        float intersect = planeDistance / normalDotRay;
        
        float3 intersection = ((rayLocal.Direction * intersect) + rayLocal.Origin) - center;
        
        float dx = dot(planeX,intersection);
        float dz = dot(planeZ,intersection);
        
        float2 uv = float2(0,0);
        
        if ( intersect > 0 )
        {
            uv = float2(dx,dz);
        }
        else
        {
            uv = float2(0,0);
        }
        
        uv /= uvScale;
        uv += float2(0.5,0.5);
        return uv;
    }

    float3 SpriteProjection( float3 pivotToCameraRayLocal, float frames, float2 size, float2 coord )
    {
        float3 gridVec = pivotToCameraRayLocal;
        
        //octahedron vector, pivot to camera
        float3 y = normalize(gridVec);
        
        float3 x = normalize( cross( y, float3(0.0, 1.0, 0.0) ) );
        float3 z = normalize( cross( x, y ) );
        //here we have a plane
        float2 uv = ((coord*frames)-0.5) * 2.0; //-1 to 1 
        //scale base on the uv 
        float3 newX = x * uv.x;
        float3 newZ = z * uv.y;
        
        float2 floatSize = size*0.5;//size = impostorSize.xx*2 so need to time .5

        //scale with imposter size
        newX *= floatSize.x;
        newZ *= floatSize.y;
        
        float3 res = newX + newZ;  
        
        return res;
    }

    void ImposterVertex(
        inout ImposterData imp,
        in float imposterFrames,
        in float imposterOffset,
        in float imposterSize,
        bool hemiCheck)
    {
        //incoming vertex, object space
        float3 vertex = imp.vertex;
        
        //camera in object space
        float3 objectSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
        float2 texcoord = imp.uv;

        float framesMinusOne = imposterFrames -1;
        
        //pivot to camera ray
        float3 pivotToCameraRay = normalize(objectSpaceCameraPos.xyz - imposterOffset);

        //scale uv to single frame
        texcoord = float2(texcoord.x,texcoord.y)*(1.0/ imposterFrames.x);
        
        //radius * 2 * unity scaling
        float2 size = imposterSize.xx * 2.0; // * objectScale.xx; //unity_BillboardSize.xy                 
        //Need to get this because the view change dramatics
        float3 projected = SpriteProjection(pivotToCameraRay, imposterFrames, size, texcoord.xy);

        //this creates the proper offset for vertices to camera facing billboard
        float3 vertexOffset = projected + imposterOffset;
        //subtract from camera pos 
        vertexOffset = normalize(objectSpaceCameraPos - vertexOffset);
        //then add the original projected world
        vertexOffset += projected;
        //remove position of vertex
        vertexOffset -= vertex.xyz;
        //add pivot
        vertexOffset += imposterOffset;

        //camera to projection vector
        float3 rayDirectionLocal = (projected + imposterOffset) - objectSpaceCameraPos;
        
        //projected position to camera ray
        // float3 projInterpolated = normalize( objectSpaceCameraPos - (projected + _ImposterOffset) ); 
        float3 projInterpolated = normalize(objectSpaceCameraPos - (projected + imposterOffset)); 
        
        Ray rayLocal;
        rayLocal.Origin = objectSpaceCameraPos - imposterOffset; 
        rayLocal.Direction = rayDirectionLocal; 
        //find which grid we are at: floor(View2DVec * FrameXY)/FramesXY
        float2 grid = VectorToGrid( pivotToCameraRay, hemiCheck);
        float2 gridRaw = grid;
        grid = saturate((grid+1.0)*0.5); //bias and scale to 0 to 1 
        grid *= framesMinusOne;//scale to fit the frames
        
        float2 gridFrac = frac(grid);
        
        float2 gridFloor = floor(grid);
        //how much blend between the 3 frames
        float4 weights = TriangleInterpolate( gridFrac ); 
        
        //3 nearest frames
        float2 frame0 = gridFloor;
        float2 frame1 = gridFloor + lerp(float2(0,1),float2(1,0),weights.w);
        float2 frame2 = gridFloor + float2(1,1);
        
        //convert frame coordinate to octahedron direction
        float3 frame0ray = FrameXYToRay(frame0, framesMinusOne.xx);
        float3 frame1ray = FrameXYToRay(frame1, framesMinusOne.xx);
        float3 frame2ray = FrameXYToRay(frame2, framesMinusOne.xx);
        
        float3 planeCenter = float3(0,0,0);
        
        float3 plane0x;
        float3 plane0normal = frame0ray;
        float3 plane0z;
        //get plane basis
        float3 frame0local = FrameTransform( projInterpolated, frame0ray, plane0x, plane0z );
        frame0local.xz = frame0local.xz/ imposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv0 = VirtualPlaneUV( plane0normal, plane0x, plane0z, planeCenter, size, rayLocal );
        vUv0 /= imposterFrames.xx;
        
        float3 plane1x; 
        float3 plane1normal = frame1ray;
        float3 plane1z;
        float3 frame1local = FrameTransform( projInterpolated, frame1ray, plane1x, plane1z);
        frame1local.xz = frame1local.xz/ imposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv1 = VirtualPlaneUV( plane1normal, plane1x, plane1z, planeCenter, size, rayLocal );
        vUv1 /= imposterFrames.xx;
        
        float3 plane2x;
        float3 plane2normal = frame2ray;
        float3 plane2z;
        float3 frame2local = FrameTransform( projInterpolated, frame2ray, plane2x, plane2z );
        frame2local.xz = frame2local.xz/ imposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv2 = VirtualPlaneUV( plane2normal, plane2x, plane2z, planeCenter, size, rayLocal );
        vUv2 /= imposterFrames.xx;
        
        //add offset here
        imp.vertex.xyz += vertexOffset;
        //overwrite others
        imp.uv = texcoord;
        imp.grid = grid;
        imp.frame0 = float4(vUv0.xy,frame0local.xz);
        imp.frame1 = float4(vUv1.xy,frame1local.xz);
        imp.frame2 = float4(vUv2.xy,frame2local.xz);
        imp.hemiCheck = hemiCheck;
    }

    void ImposterUV(in float2 grid, in float4 impFrame0, in float4 impFrame1, in float4 impFrame2,
        in float4 texelSize,
        in float imposterFrames, in texture2D depthTexture, in float imposterBorderClamp, in SamplerState ss,
        out float2 vp0uv, out float2 vp1uv, out float2 vp2uv, out float4 weights)
    {

        float2 fracGrid = frac(grid);

        weights = TriangleInterpolate(fracGrid);

        float2 gridSnap = floor(grid) / imposterFrames.xx;

        float2 frame0 = gridSnap;
        float2 frame1 = gridSnap + (lerp(float2(0, 1), float2(1, 0), weights.w) / imposterFrames.xx);
        float2 frame2 = gridSnap + (float2(1, 1) / imposterFrames.xx);

        vp0uv = frame0 + impFrame0.xy;
        vp1uv = frame1 + impFrame1.xy;
        vp2uv = frame2 + impFrame2.xy;

        //resolution of atlas (Square)
        float textureDims = texelSize.z;
        //fractional frame size, ex 2048/12 = 170.6
        float frameSize = textureDims / imposterFrames;
        //actual atlas resolution used, ex 170*12 = 2040
        float actualDims = floor(frameSize) * imposterFrames;
        //the scale factor to apply to UV coordinate, ex 2048/2040 = 0.99609375
        float scaleFactor = actualDims / textureDims;

        vp0uv *= scaleFactor;
        vp1uv *= scaleFactor;
        vp2uv *= scaleFactor;

        // clip out neighboring frames 
        float2 gridSize = 1.0 / imposterFrames.xx;
        gridSize *= texelSize.zw;
        gridSize *= texelSize.xy;
        float2 border = texelSize.xy * imposterBorderClamp;

        vp0uv = clamp(vp0uv, frame0 - border, frame0 + gridSize + border);
        vp1uv = clamp(vp1uv, frame1 - border, frame1 + gridSize + border);
        vp2uv = clamp(vp2uv, frame2 - border, frame2 + gridSize + border);

       // float4 ddxy = float4(ddx(imp.uv), ddy(imp.uv));

        //outAlbedo = ImposterBlendWeights(albedoMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
        //outNormal = ImposterBlendWeights(normalMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
    }

    void ImposterUV_Parallax(in float2 grid, in float4 impFrame0, in float4 impFrame1, in float4 impFrame2,
        in float4 texelSize,
        in float imposterFrames, in texture2D depthTexture, in float imposterBorderClamp, in SamplerState ss,
        out float2 vp0uv, out float2 vp1uv, out float2 vp2uv, out float4 weights)
    {

        float2 fracGrid = frac(grid);

        weights = TriangleInterpolate(fracGrid);

        float2 gridSnap = floor(grid) / imposterFrames.xx;

        float2 frame0 = gridSnap;
        float2 frame1 = gridSnap + (lerp(float2(0, 1), float2(1, 0), weights.w) / imposterFrames.xx);
        float2 frame2 = gridSnap + (float2(1, 1) / imposterFrames.xx);

        vp0uv = frame0 + impFrame0.xy;
        vp1uv = frame1 + impFrame1.xy;
        vp2uv = frame2 + impFrame2.xy;

        //resolution of atlas (Square)
        float textureDims = texelSize.z;
        //fractional frame size, ex 2048/12 = 170.6
        float frameSize = textureDims / imposterFrames;
        //actual atlas resolution used, ex 170*12 = 2040
        float actualDims = floor(frameSize) * imposterFrames;
        //the scale factor to apply to UV coordinate, ex 2048/2040 = 0.99609375
        float scaleFactor = actualDims / textureDims;

        vp0uv *= scaleFactor;
        vp1uv *= scaleFactor;
        vp2uv *= scaleFactor;


        //add parallax shift 
float depth0 = SAMPLE_TEXTURE2D(depthTexture, ss, vp0uv).a;
float depth1 = SAMPLE_TEXTURE2D(depthTexture, ss, vp1uv).a;
float depth2 = SAMPLE_TEXTURE2D(depthTexture, ss, vp2uv).a;
vp0uv -= impFrame0.zw * (depth0 - 0.5);
vp1uv -= impFrame1.zw * (depth1 - 0.5);
vp2uv -= impFrame2.zw * (depth2 - 0.5);



        // clip out neighboring frames 
        float2 gridSize = 1.0 / imposterFrames.xx;
        gridSize *= texelSize.zw;
        gridSize *= texelSize.xy;
        float2 border = texelSize.xy * imposterBorderClamp;

        vp0uv = clamp(vp0uv, frame0 - border, frame0 + gridSize + border);
        vp1uv = clamp(vp1uv, frame1 - border, frame1 + gridSize + border);
        vp2uv = clamp(vp2uv, frame2 - border, frame2 + gridSize + border);

        //float4 ddxy = float4(ddx(imp.uv), ddy(imp.uv));

        //outAlbedo = ImposterBlendWeights(albedoMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
        //outNormal = ImposterBlendWeights(normalMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
    }

#endif 
