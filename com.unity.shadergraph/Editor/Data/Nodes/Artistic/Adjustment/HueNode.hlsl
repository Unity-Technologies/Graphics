real3 Unity_Hue(real3 IN, real offset, float offsetFactor)
{
    // RGB to HSV
    real4 K = real4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    real4 P = lerp(real4(IN.bg, K.wz), real4(IN.gb, K.xy), step(IN.b, IN.g));
    real4 Q = lerp(real4(P.xyw, IN.r), real4(IN.r, P.yzx), step(P.x, IN.r));
    real D = Q.x - min(Q.w, Q.y);
    real E = 1e-10;
    real3 hsv = real3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
    real hue = hsv.x + offset * offsetFactor;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;
    // HSV to RGB
    real4 K2 = real4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    real3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    return hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}
