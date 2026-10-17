#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float4 _CellCount;
sampler2D _FalloffLut;
float4 _Lights[4];
float _LightCount;
float _Ambient;

// Each light is (x pixels, y pixels, horizontal radius in pixels, intensity); the ellipse is half as tall.
float LightAt(float2 pixel)
{
    float light = _Ambient;
    for (int k = 0; k < 4; k++)
    {
        if (k < _LightCount)
        {
            float2 offset = (pixel - _Lights[k].xy) / max(_Lights[k].z, 1.0);
            offset.y *= 2.0;
            float falloff = tex2Dlod(_FalloffLut, float4(length(offset), 0.5, 0.0, 0.0)).r;
            light = max(light, falloff * _Lights[k].w);
        }
    }
    return light;
}

// Fifteen bilinear taps between pixel pairs average a 6x10 cell (AsciiSettings.CellWidth/CellHeight).
fixed4 FragDownsample(v2f_img i) : SV_Target
{
    float2 origin = floor(i.uv * _CellCount.xy) * float2(6.0, 10.0);
    float3 sum = 0.0;
    for (int y = 0; y < 5; y++)
    {
        for (int x = 0; x < 3; x++)
        {
            float2 pixel = origin + float2(1.0 + 2.0 * x, 1.0 + 2.0 * y);
            sum += tex2Dlod(_MainTex, float4(pixel * _MainTex_TexelSize.xy, 0.0, 0.0)).rgb;
        }
    }
    return fixed4(sum / 15.0 * LightAt(origin + float2(3.0, 5.0)), 1.0);
}
