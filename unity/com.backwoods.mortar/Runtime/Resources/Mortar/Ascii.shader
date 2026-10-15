Shader "Hidden/Mortar/Ascii"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Downsample"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragDownsample
            #pragma target 3.0
            #include "Downsample.cginc"
            ENDCG
        }

        Pass
        {
            Name "Glyph"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragGlyph
            #pragma target 3.0
            #include "Glyph.cginc"
            ENDCG
        }
    }
    Fallback Off
}
