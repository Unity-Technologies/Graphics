#ifndef SDF_DEPTHOFFIELD_INCLUDED
#define SDF_DEPTHOFFIELD_INCLUDED

#include "UnityShaderVariables.cginc"


int lensRes;
float lensDis;
float lensSiz;
float focalDis;
float3 BackgroundColor;
float4 iResolution;

float asphere(in float3 ro, in float3 rd, in float3 sp, in float sr)
{
    // geometric solution
    float sr2 = sr*sr;
    float3 e0 = sp - ro;
    float e1 = dot(e0,rd);
    float r2 = dot(e0,e0) - e1*e1;
    if (r2 > sr2) return 1000.0;
    float e2 = sqrt(sr2 - r2);
    return e1-e2;
}


float map(in float3 ro, in float3 rd)
{
    return min(asphere(ro,rd,float3(0.0,0.0,0.0), 1.5),
               min(asphere(ro,rd,float3(-2,0.0,0.0),1.0),
                   min(asphere(ro,rd,float3(0.0,-2,0.0),1.0),
                       min(asphere(ro,rd,float3(1.15,1.15,1.15),1.0),
                           min(asphere(ro,rd,float3(0.0,0.0,-2),1.0),
                              asphere(ro,rd,float3(3.,3.,3.),0.2))))));
}


float3 ascene(in float3 ro, in float3 rd){
    float t = map(ro,rd);
    float3 col = float3(0, 0, 0);
    if (t==1000.0)
    {
        col = BackgroundColor;
    }
    else
    {
        float3 loc = t*rd+ro;
        loc = loc*0.5;
        col =  float3(clamp(loc.x,0.0,1.0),clamp(loc.y,0.0,1.0),clamp(loc.z,0.0,1.0));
    }
    return col;
}

float4 DepthOfField(float4 position : SV_POSITION, float2 uv : TEXCOORD0)  : SV_Target
{
    const int ssaa = 1;


    float2 fragCoord = float2(uv.x * iResolution.x, uv.y * iResolution.y);
    //fragcoord is the center of the pixel
    float2 sensorLoc = fragCoord.xy / iResolution.x; //sets x limits from 0-1 y at same scale, center at (0.5,0.?)
    sensorLoc = float2(0.5, 0.5*(iResolution.y/iResolution.x)) - sensorLoc; //reverse sensor and center on (0,0)

    float3 Z = float3(0.0,0.0,1.0); //useful later could be hardcoded later instead

    float3 cameraDir = -_WorldSpaceCameraPos; //this will and should be normalized
    cameraDir = normalize(cameraDir); //normalize

    float3 cameraX = cross(cameraDir,Z); //right dir for camera
    cameraX = normalize(cameraX); //normalize

    float3 cameraY = cross(cameraX,cameraDir); //up dir for camera
    cameraY = normalize(cameraY); //normlize

    float3 colorTotal = float3(0.0,0.0,0.0);//for each pixel reset the accumulated color
    float colorCount = 0.0;
    float lensResF = float(lensRes); //for comparing to float later
    float focal = 1.0+lensDis/focalDis; //brings the image to focus at focalDis from the cameraPos
    float ssaaF = float(ssaa); // for using later to save a cast.
    float sscale = 1.0/(iResolution.x); // size of a pixel
    float sstep = 1.0/ssaaF;
    float sstart = sstep/2.0-0.5;
    float lstep = 1.0/lensResF;
    float lstart = lstep/2.0-0.5;

    for (float sx = sstart; sx < 0.5; sx += sstep) //SSAA x direction
    {
        for (float sy = sstart; sy < 0.5; sy += sstep) //SSAA y direction
        {
            float2 ss = float2(sx,sy)*sscale; //sub pixel offset for SSAA
            float3 sensorRel = cameraX*(sensorLoc.x+ss.x) + cameraY*(sensorLoc.y+ss.y); //position on sensor relative to center of sensor. Used once
            float3 sensorPos = _WorldSpaceCameraPos - lensDis*cameraDir + sensorRel; //3d position of ray1 origin on sensor

            for (float lx = lstart; lx < 0.5; lx+=lstep)
            {
                for (float ly = lstart; ly < 0.5; ly+=lstep)
                {
                    float2 lensCoord = float2(lx,ly); //fragCoord analog for lens array. lens is square
                    float2 lensLoc = (lensCoord)*lensSiz; //location on 2d lens plane

                    if (length(lensLoc)<(lensSiz/2.0)) //trim lens to circle
                    {
                        float3 lensRel = cameraX*(lensLoc.x) + cameraY*(lensLoc.y); //position on lens relative to lens center. Used twice
                        float3 lensPos = _WorldSpaceCameraPos + lensRel; // 3d position of ray1 end and ray2 origin on lens
                        float3 rayDir1 = lensPos - sensorPos; //direction of ray from sensor to lens
                        float3 rayDir2 = rayDir1 - focal*(lensRel); //direction of ray afer being focused by lens
                        rayDir2 = normalize(rayDir2); //normalize after focus
                        float3 color = ascene(lensPos,rayDir2); //scene returns a color
                        colorTotal = colorTotal+color; //sum colors over all  points from lens
                        colorCount += 1.0; //total number of colors added.
                    }
                }
            }
        }
    }

    return float4(colorTotal/colorCount, 0.0); //slight post-processing
}

#endif
