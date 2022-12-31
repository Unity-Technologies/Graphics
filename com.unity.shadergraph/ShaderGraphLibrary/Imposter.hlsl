#ifndef XRA_IMPOSTERCOMMON
    #define XRA_IMPOSTERCOMMON

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

    struct ImposterData
    {
        float2 uv;
        float2 grid;
        float4 frame0;
        float4 frame1;
        float4 frame2;
        float3 vertex;
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
    float2 VectorToGrid( float3 vec )
    {
        float2 coord;

        if (_ImposterIsHalfSphere)
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

    void ImposterVertex(inout ImposterData imp)
    {
        //incoming vertex, object space
        float3 vertex = imp.vertex;
        
        //camera in object space
        float3 objectSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
        float2 texcoord = imp.uv;

        float framesMinusOne = _ImposterFrames-1;
        
        //pivot to camera ray
        float3 pivotToCameraRay = normalize(objectSpaceCameraPos.xyz - _ImposterOffset);

        //scale uv to single frame
        texcoord = float2(texcoord.x,texcoord.y)*(1.0/_ImposterFrames.x);  
        
        //radius * 2 * unity scaling
        float2 size = _ImposterSize.xx * 2.0; // * objectScale.xx; //unity_BillboardSize.xy                 
        //Need to get this because the view change dramatics
        float3 projected = SpriteProjection(pivotToCameraRay, _ImposterFrames, size, texcoord.xy);

        //this creates the proper offset for vertices to camera facing billboard
        float3 vertexOffset = projected + _ImposterOffset;
        //subtract from camera pos //WHY?
        vertexOffset = normalize(objectSpaceCameraPos - vertexOffset);
        //then add the original projected world
        vertexOffset += projected;
        //remove position of vertex
        vertexOffset -= vertex.xyz;
        //add pivot
        vertexOffset += _ImposterOffset;

        //camera to projection vector
        float3 rayDirectionLocal = (projected + _ImposterOffset) - objectSpaceCameraPos;
        
        //projected position to camera ray
        // float3 projInterpolated = normalize( objectSpaceCameraPos - (projected + _ImposterOffset) ); 
        float3 projInterpolated = normalize(objectSpaceCameraPos - (projected + _ImposterOffset)); 
        
        Ray rayLocal;
        rayLocal.Origin = objectSpaceCameraPos - _ImposterOffset; 
        rayLocal.Direction = rayDirectionLocal; 
        //find which grid we are at: floor(View2DVec * FrameXY)/FramesXY
        float2 grid = VectorToGrid( pivotToCameraRay );
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
        frame0local.xz = frame0local.xz/_ImposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv0 = VirtualPlaneUV( plane0normal, plane0x, plane0z, planeCenter, size, rayLocal );
        vUv0 /= _ImposterFrames.xx;   
        
        float3 plane1x; 
        float3 plane1normal = frame1ray;
        float3 plane1z;
        float3 frame1local = FrameTransform( projInterpolated, frame1ray, plane1x, plane1z);
        frame1local.xz = frame1local.xz/_ImposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv1 = VirtualPlaneUV( plane1normal, plane1x, plane1z, planeCenter, size, rayLocal );
        vUv1 /= _ImposterFrames.xx;
        
        float3 plane2x;
        float3 plane2normal = frame2ray;
        float3 plane2z;
        float3 frame2local = FrameTransform( projInterpolated, frame2ray, plane2x, plane2z );
        frame2local.xz = frame2local.xz/_ImposterFrames.xx; //for displacement
        
        //virtual plane UV coordinates
        float2 vUv2 = VirtualPlaneUV( plane2normal, plane2x, plane2z, planeCenter, size, rayLocal );
        vUv2 /= _ImposterFrames.xx;
        
        //add offset here
        imp.vertex.xyz += vertexOffset;
        //overwrite others
        imp.uv = texcoord;
        imp.grid = grid;
        imp.frame0 = float4(vUv0.xy,frame0local.xz);
        imp.frame1 = float4(vUv1.xy,frame1local.xz);
        imp.frame2 = float4(vUv2.xy,frame2local.xz);
    }

    void ImposterSample(in ImposterData imp, in SamplerState ss, out float4 outAlbedo, out float4 outNormal)//, out float depth )
    {
        float2 fracGrid = frac(imp.grid);
        
        float4 weights = TriangleInterpolate(fracGrid);
        
        float2 gridSnap = floor(imp.grid) / _ImposterFrames.xx;
        
        float2 frame0 = gridSnap;
        float2 frame1 = gridSnap + (lerp(float2(0,1),float2(1,0),weights.w)/_ImposterFrames.xx);
        float2 frame2 = gridSnap + (float2(1,1)/_ImposterFrames.xx);
        
        float2 vp0uv = frame0 + imp.frame0.xy;
        float2 vp1uv = frame1 + imp.frame1.xy; 
        float2 vp2uv = frame2 + imp.frame2.xy;
        
        //resolution of atlas (Square)
        float textureDims = _ImposterAlbedoMap_TexelSize.z;
        //fractional frame size, ex 2048/12 = 170.6
        float frameSize = textureDims/_ImposterFrames; 
        //actual atlas resolution used, ex 170*12 = 2040
        float actualDims = floor(frameSize) * _ImposterFrames; 
        //the scale factor to apply to UV coordinate, ex 2048/2040 = 0.99609375
        float scaleFactor = actualDims / textureDims;
        
        vp0uv *= scaleFactor;
        vp1uv *= scaleFactor;
        vp2uv *= scaleFactor;
        
        //add parallax shift 
        float depth0 = SAMPLE_TEXTURE2D(_ImposterNormalMap, ss, vp0uv).a;
        float depth1 = SAMPLE_TEXTURE2D(_ImposterNormalMap, ss, vp1uv).a;
        float depth2 = SAMPLE_TEXTURE2D(_ImposterNormalMap, ss, vp2uv).a;

        vp0uv -= imp.frame0.zw * (depth0 - 0.5);
        vp1uv -= imp.frame1.zw * (depth1 - 0.5);
        vp2uv -= imp.frame2.zw * (depth2 - 0.5);

        // clip out neighboring frames 
        float2 gridSize = 1.0/_ImposterFrames.xx;
        gridSize *= _ImposterAlbedoMap_TexelSize.zw;
        gridSize *= _ImposterAlbedoMap_TexelSize.xy;
        float2 border = _ImposterAlbedoMap_TexelSize.xy * _ImposterBorderClamp;

        vp0uv = clamp(vp0uv, frame0 - border, frame0 + gridSize + border);
        vp1uv = clamp(vp1uv, frame1 - border, frame1 + gridSize + border);
        vp2uv = clamp(vp2uv, frame2 - border, frame2 + gridSize + border);

        float4 ddxy = float4(ddx(imp.uv), ddy(imp.uv));
        
        outAlbedo = ImposterBlendWeights(_ImposterAlbedoMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
        outNormal = ImposterBlendWeights(_ImposterNormalMap, ss, vp0uv, vp1uv, vp2uv, weights.xyz, ddxy);
    }

    void Vertex_half(in float3 inPositionOS, in float2 inUV, out float3 outPositionOS, out float4 outUVGrid, out float4 outPlane0, out float4 outPlane1, out float4 outPlane2)
    {
        ImposterData imp;
        imp.vertex = inPositionOS;
        imp.uv = inUV.xy;
        
        ImposterVertex(imp);
        
        //IMP results  
        //v2f
        outPositionOS = imp.vertex;
        
        //surface
        outUVGrid.xy = imp.uv;
        outUVGrid.zw = imp.grid;
        outPlane0 = imp.frame0;
        outPlane1 = imp.frame1;
        outPlane2 = imp.frame2;
    }

    void Fragment_half(in float3 inNormalWS, in float3 inTangentWorld, in float3 inBiTangentWS, in float4 inUVGrid, in float4 inPlane0, in float4 inPlane1, in float4 inPlane2,in SamplerState ss, out float3 outColor, out float3 outNormal, out float outAlpha)
    {
       // SamplerState ss = MAIN_SAMPLERSTATE_CLAMP;

        ImposterData imp;
        imp.uv = inUVGrid.xy;
        imp.grid = inUVGrid.zw;
        imp.frame0 = inPlane0; 
        imp.frame1 = inPlane1;
        imp.frame2 = inPlane2;
        
        half4 albedoMapSample;
        half4 normalMapSample;
        ImposterSample(imp, ss, albedoMapSample, normalMapSample);

        outColor = albedoMapSample.rgb;
        outNormal = normalMapSample.xyz * 2 - 1;
        outAlpha = albedoMapSample.a;
    }

#endif //XRA_IMPOSTERCOMMON
