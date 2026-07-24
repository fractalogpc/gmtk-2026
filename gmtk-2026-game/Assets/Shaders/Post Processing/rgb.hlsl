#ifndef SIMPLE_RGB_INCLUDED
#define SIMPLE_RGB_INCLUDED

void rgb_float(float3 c, out float3 rgb)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    rgb = c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
#endif