#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _CellTex;
sampler2D _MaskTex;
sampler2D _GlyphAtlas;
sampler2D _RampLut;
sampler2D _EdgeLut;
float4 _CellCount;
float _Background;
float _Normalize;
float _Saturation;
float _DarkThreshold;
float _StrokeScale;
float _StrokeSpeed;
float _FigureLift;
float _FigureFloor;
sampler2D _BloomTex;
float _BloomStrength;

static const float2 CellPixels = float2(6.0, 10.0);
static const float Slots = 16.0;
static const float FirstEdgeSlot = 12.0;
static const float3 Luma = float3(0.299, 0.587, 0.114);

float4 Fetch(sampler2D lookup, float2 uv)
{
    return tex2Dlod(lookup, float4(uv, 0.0, 0.0));
}

float2 CellUv(float2 cell)
{
    return (cell + 0.5) / _CellCount.xy;
}

float CellLuma(float2 cell)
{
    return dot(Fetch(_CellTex, CellUv(cell)).rgb, Luma);
}

float Slot(float encoded)
{
    return round(encoded * (Slots - 1.0));
}

float Hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float bottom = lerp(Hash(i), Hash(i + float2(1.0, 0.0)), u.x);
    float top = lerp(Hash(i + float2(0.0, 1.0)), Hash(i + float2(1.0, 1.0)), u.x);
    return lerp(bottom, top, u.y);
}

float StrokeSlot(float2 cell)
{
    // Prefer - / \ over | so travelling lean reads as wind, not contour bands.
    float2 wind = float2(1.0, 0.35);
    float phase = dot(cell * _StrokeScale, wind) - _Time.y * _StrokeSpeed;
    float gust = 0.5 + 0.5 * sin(phase * 6.2831853);
    float ripples = Noise(cell * _StrokeScale * 2.0 + float2(_Time.y * _StrokeSpeed * 0.4, 0.0));
    float lean = saturate(gust * 0.65 + ripples * 0.25 + Hash(cell) * 0.2);
    float band = lean * 3.0;
    if (band < 1.0)
    {
        return FirstEdgeSlot;
    }
    if (band < 2.0)
    {
        return FirstEdgeSlot + 1.0;
    }
    return FirstEdgeSlot + 3.0;
}

float ChooseSlot(float2 cell, float luma)
{
    float4 mask = Fetch(_MaskTex, CellUv(cell));
    if (mask.b > 0.5 && luma > 0.0)
    {
        luma = saturate(max(luma * _FigureLift, _FigureFloor));
    }
    if (luma < _DarkThreshold)
    {
        return 0.0;
    }
    if (mask.r > 0.5)
    {
        return StrokeSlot(cell);
    }
    float gx = (CellLuma(cell + float2(1.0, 0.0)) - CellLuma(cell - float2(1.0, 0.0))) * 0.5;
    float gy = (CellLuma(cell + float2(0.0, 1.0)) - CellLuma(cell - float2(0.0, 1.0))) * 0.5;
    float edge = Slot(Fetch(_EdgeLut, float2(gx, gy) * 0.5 + 0.5).r);
    return edge > 0.0 ? edge : Slot(Fetch(_RampLut, float2(luma, 0.5)).r);
}

float3 Tint(float3 color)
{
    float peak = max(max(color.r, color.g), max(color.b, 0.0001));
    float3 tint = lerp(color, color / peak, _Normalize);
    return lerp(dot(tint, Luma).xxx, tint, _Saturation);
}

float Ink(float slot, float2 local)
{
    float2 uv = float2((slot * CellPixels.x + local.x + 0.5) / (Slots * CellPixels.x), (local.y + 0.5) / CellPixels.y);
    return Fetch(_GlyphAtlas, uv).a;
}

fixed4 FragGlyph(v2f_img i) : SV_Target
{
    float2 uv = i.uv;
#if UNITY_UV_STARTS_AT_TOP
    if (_MainTex_TexelSize.y < 0.0)
    {
        uv.y = 1.0 - uv.y;
    }
#endif
    float2 pixel = floor(uv * abs(_MainTex_TexelSize.zw));
    float2 cell = floor(pixel / CellPixels);
    float2 cellUv = CellUv(cell);
    float3 color = saturate(Fetch(_CellTex, cellUv).rgb + Fetch(_BloomTex, cellUv).rgb * _BloomStrength);
    float slot = ChooseSlot(cell, dot(color, Luma));
    float ink = Ink(slot, pixel - cell * CellPixels);
    return fixed4(lerp(_Background.xxx, Tint(color), ink), 1.0);
}
