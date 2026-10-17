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

        Pass
        {
            Name "BloomAcross"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBloomAcross
            #pragma target 3.0
            #include "Bloom.cginc"
            ENDCG
        }

        Pass
        {
            Name "BloomDown"
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment FragBloomDown
            #pragma target 3.0
            #include "Bloom.cginc"
            ENDCG
        }
    }
    Fallback Off
}
