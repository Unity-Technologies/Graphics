#ifndef XRA_IMPOSTERCOMMON
    #define XRA_IMPOSTERCOMMON

    TEXTURE2D(_ImposterAlbedoMap);
    float4 _ImposterAlbedoMap_TexelSize;
    TEXTURE2D(_ImposterWorldNormalDepthMap);

    float _ImposterFrames;
    float _ImposterSize;
    float3 _ImposterOffset;
    float _ImposterIsHalfSphere;
    float _ImposterBorderClamp;

    struct ImposterData
    {
        float2 uv;
        float2 grid;
        float4 frame0;
        float4 frame1;
        float4 frame2;
        float4 vertex;
    };

    struct Ray
    {
        float3 Origin;
        float3 Direction;
    };

    float3 NormalizePerPixelNormal (float3 n)
    {
        #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
            return n;
        #else
            return normalize(n);
        #endif
    }

    float3 PerPixelWorldNormal(float4 i_tex, float4 tangentToWorld[3])
    {
        #ifdef _NORMALMAP
            float3 tangent = tangentToWorld[0].xyz;
            float3 binormal = tangentToWorld[1].xyz;
            float3 normal = tangentToWorld[2].xyz;

            #if UNITY_TANGENT_ORTHONORMALIZE
                normal = NormalizePerPixelNormal(normal);

                // ortho-normalize Tangent
                tangent = normalize (tangent - normal * dot(tangent, normal));

                // recalculate Binormal
                float3 newB = cross(normal, tangent);
                binormal = newB * sign (dot (newB, binormal));
            #endif

            float3 normalTangent = NormalInTangentSpace(i_tex);
            float3 normalWorld = NormalizePerPixelNormal(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z); // @TODO: see if we can squeeze this normalize on SM2.0 as well
        #else
            float3 normalWorld = normalize(tangentToWorld[2].xyz);
        #endif
        return normalWorld;
    }

    float4 BakeNormalsDepth( TEXTURE2D bumpMap, float2 uv, float depth, float4 tangentToWorld[3] )
    {
        float4 tex = tex2D( bumpMap, uv );
        
        float3 worldNormal = PerPixelWorldNormal(tex, tangentToWorld);
        
        return float4( worldNormal.xyz*0.5+0.5, 1-depth );
    }

    float4 ImposterBlendWeights( TEXTURE2D tex, float2 uv, float2 frame0, float2 frame1, float2 frame2, float4 weights, float2 ddxy )
    {    
        float4 samp0 = tex2Dgrad( tex, frame0, ddxy.x, ddxy.y );
        float4 samp1 = tex2Dgrad( tex, frame1, ddxy.x, ddxy.y );
        float4 samp2 = tex2Dgrad( tex, frame2, ddxy.x, ddxy.y );

        //float4 samp0 = tex2Dlod( tex, float4(frame0,0,0) );
        //float4 samp1 = tex2Dlod( tex, float4(frame1,0,0) );
        //float4 samp2 = tex2Dlod( tex, float4(frame2,0,0) );

        float4 result = samp0*weights.x + samp1*weights.y + samp2*weights.z;
        
        return result;
    }

    float Isolate( float c, float w, float x )
    {
        return smoothstep(c-w,c,x)-smoothstep(c,c+w,x);
    }

    float SphereMask( float2 p1, float2 p2, float r, float h )
    {
        float d = distance(p1,p2);
        return 1-smoothstep(d,r,h);
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

        float2 uv = ((coord*frames)-0.5) * 2.0; //-1 to 1 

        float3 newX = x * uv.x;
        float3 newZ = z * uv.y;
        
        float2 floatSize = size*0.5;
        
        newX *= floatSize.x;
        newZ *= floatSize.y;
        
        float3 res = newX + newZ;  
        
        return res;
    }

    void ImposterVertex( inout ImposterData imp )
    {
        //incoming vertex, object space
        float4 vertex = imp.vertex;
        
        //camera in object space
        float3 objectSpaceCameraPos = mul( unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz,1) ).xyz;
        float2 texcoord = imp.uv;
        float4x4 objectToWorld = unity_ObjectToWorld;
        float4x4 worldToObject = unity_WorldToObject;

        float3 imposterPivotOffset = _ImposterOffset.xyz;
        float framesMinusOne = _ImposterFrames-1;
        
        float3 objectScale = float3(length(float3(objectToWorld[0].x, objectToWorld[1].x, objectToWorld[2].x)),
        length(float3(objectToWorld[0].y, objectToWorld[1].y, objectToWorld[2].y)),
        length(float3(objectToWorld[0].z, objectToWorld[1].z, objectToWorld[2].z)));
        
        //pivot to camera ray
        float3 pivotToCameraRay = normalize(objectSpaceCameraPos.xyz-imposterPivotOffset.xyz);

        //scale uv to single frame
        texcoord = float2(texcoord.x,texcoord.y)*(1.0/_ImposterFrames.x);  
        
        //radius * 2 * unity scaling
        float2 size = _ImposterSize.xx * 2.0; // * objectScale.xx; //unity_BillboardSize.xy                 
        
        float3 projected = SpriteProjection( pivotToCameraRay, _ImposterFrames, size, texcoord.xy );

        //this creates the proper offset for vertices to camera facing billboard
        float3 vertexOffset = projected + imposterPivotOffset;
        //subtract from camera pos 
        vertexOffset = normalize(objectSpaceCameraPos-vertexOffset);
        //then add the original projected world
        vertexOffset += projected;
        //remove position of vertex
        vertexOffset -= vertex.xyz;
        //add pivot
        vertexOffset += imposterPivotOffset;

        //camera to projection vector
        float3 rayDirectionLocal = (imposterPivotOffset + projected) - objectSpaceCameraPos;
        
        //projected position to camera ray
        float3 projInterpolated = normalize( objectSpaceCameraPos - (projected + imposterPivotOffset) ); 
        
        Ray rayLocal;
        rayLocal.Origin = objectSpaceCameraPos-imposterPivotOffset; 
        rayLocal.Direction = rayDirectionLocal; 
        
        float2 grid = VectorToGrid( pivotToCameraRay );
        float2 gridRaw = grid;
        grid = saturate((grid+1.0)*0.5); //bias and scale to 0 to 1 
        grid *= framesMinusOne;
        
        float2 gridFrac = frac(grid);
        
        float2 gridFloor = floor(grid);
        
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

    void ImposterSample( in ImposterData imp, out float4 baseTex, out float4 worldNormal )//, out float depth )
    {
        float2 fracGrid = frac(imp.grid);
        
        float4 weights = TriangleInterpolate( fracGrid );
        
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
        
        //clamp out neighboring frames TODO maybe discard instead?
        float2 gridSize = 1.0/_ImposterFrames.xx;
        gridSize *= _ImposterAlbedoMap_TexelSize.zw;
        gridSize *= _ImposterAlbedoMap_TexelSize.xy;
        float2 border = _ImposterAlbedoMap_TexelSize.xy*_ImposterBorderClamp;
        
        //vp0uv = clamp(vp0uv,frame0+border,frame0+gridSize-border);
        //vp1uv = clamp(vp1uv,frame1+border,frame1+gridSize-border);
        //vp2uv = clamp(vp2uv,frame2+border,frame2+gridSize-border);
        
        // for parallax modify
        float4 n0 = tex2Dlod( _ImposterWorldNormalDepthMap, float4(vp0uv, 0, 1 ) );
        float4 n1 = tex2Dlod( _ImposterWorldNormalDepthMap, float4(vp1uv, 0, 1 ) );
        float4 n2 = tex2Dlod( _ImposterWorldNormalDepthMap, float4(vp2uv, 0, 1 ) );
        
        float n0s = 0.5-n0.a;    
        float n1s = 0.5-n1.a;
        float n2s = 0.5-n2.a;
        
        float2 n0p = imp.frame0.zw * n0s;
        float2 n1p = imp.frame1.zw * n1s;
        float2 n2p = imp.frame2.zw * n2s;
        
        //add parallax shift 
        vp0uv += n0p;
        vp1uv += n1p;
        vp2uv += n2p;
        
        //clamp out neighboring frames TODO maybe discard instead?
        vp0uv = clamp(vp0uv,frame0+border,frame0+gridSize-border);
        vp1uv = clamp(vp1uv,frame1+border,frame1+gridSize-border);
        vp2uv = clamp(vp2uv,frame2+border,frame2+gridSize-border);
        
        float2 ddxy = float2( ddx(imp.uv.x), ddy(imp.uv.y) );
        
        worldNormal = ImposterBlendWeights( _ImposterWorldNormalDepthMap, imp.uv, vp0uv, vp1uv, vp2uv, weights, ddxy );
        baseTex = ImposterBlendWeights( _ImposterAlbedoMap, imp.uv, vp0uv, vp1uv, vp2uv, weights, ddxy );
        
        //pixel depth offset
        //float pdo = 1-baseTex.a;
        //float3 objectScale = float3(length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
        //                        length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
        //                        length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
        //float2 size = _ImposterSize.xx * 2.0;// * objectScale.xx;  
        //float3 viewWorld = mul( UNITY_MATRIX_VP, float4(0,0,1,0) ).xyz;
        //pdo *= size * abs(dot(normalize(imp.viewDirWorld.xyz),viewWorld));
        //depth = pdo;
    }

#endif //XRA_IMPOSTERCOMMON