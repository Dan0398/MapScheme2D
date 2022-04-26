Shader "Custom/Grid Shader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
	    _LineColor ("Line Color", Color) = (0.6,0.6,0.6,1)
        _AxisColor ("Axis Color", Color) = (1,0,0,1)
        _WidthLineColor ("Width Line Color", Color) = (0,0,0,1)
	    _CellSize("Cell Size In Pixels", Range(0,200)) = 50
	    _OutlinePercent("Outline In %", Range(0,100)) = 1
        _CenterOffset ("Center Offset", Vector) = (0,0,0,0)
        _RRR("R", Range(-1,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _CellSize;
            float _OutlinePercent;
            float4 _MainColor;
            float4 _LineColor;
            float4 _AxisColor;
            float4 _WidthLineColor;
            float4 _CenterOffset;
            float _RRR;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz - _CenterOffset.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed XAxis = abs(i.worldPos.x) < 0.5*_CellSize*_OutlinePercent/100? 1:0;
                fixed YAxis = abs(i.worldPos.y) < 0.5*_CellSize*_OutlinePercent/100? 1:0;
                fixed AxisMask = XAxis || YAxis;

                fixed OutlineScale = _CellSize* (1-_OutlinePercent/100) *0.5f;
                /*
                fixed2 WorldRepeated_Big = abs((i.worldPos.xy/5)%(_CellSize))/5 + _CellSize*(0.5,0.5);
                fixed XWidthMask = (WorldRepeated_Big.x > OutlineScale && WorldRepeated_Big.x < _CellSize - OutlineScale)? 1 : 0;
                fixed YWidthMask = (WorldRepeated_Big.y > OutlineScale && WorldRepeated_Big.y < _CellSize - OutlineScale)? 1 : 0;
                fixed WidthMask = XWidthMask|| YWidthMask;
                WidthMask *= (1-AxisMask);
                */
                
                fixed2 WorldRepeated = abs((i.worldPos.xy+ _CellSize*(0.5,0.5))%_CellSize);
                fixed XMask = (WorldRepeated.x > OutlineScale && WorldRepeated.x < _CellSize - OutlineScale)? 1 : 0;
	            fixed YMask = (WorldRepeated.y > OutlineScale && WorldRepeated.y < _CellSize - OutlineScale)? 1 : 0;
	            fixed BaseMask = XMask || YMask;
                //BaseMask *= (1-WidthMask);
                BaseMask *= (1-AxisMask);
                
                fixed ReadyMask = AxisMask + BaseMask;//+ WidthMask

                fixed4 Result = _MainColor * (1-ReadyMask);
                Result += _LineColor * BaseMask;
                Result += _AxisColor * AxisMask;
                //Result += _WidthLineColor * WidthMask;
                
                return Result;
            }
            ENDCG
        }
    }
}
