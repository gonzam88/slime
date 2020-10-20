Shader "Custom/SlimeParticle" {
	Properties {
		_Color ("Main Color", Color) = (1,0.5,0.5,1)
	}

	SubShader {
		Pass {
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Blend SrcAlpha one

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0

		fixed4 _Color;

		struct Particle{
			float3 position;
			float3 velocity;
			float life;
		};
		
		struct PS_INPUT{
			float4 position : SV_POSITION;
			float4 color : COLOR;
			float life : LIFE;
		};
		// particles' data
		StructuredBuffer<Particle> particleBuffer;
		

		PS_INPUT vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			PS_INPUT o = (PS_INPUT)0;

			// Color
			// float life = particleBuffer[instance_id].life;
			// float lerpVal = life * 0.25f;
			//o.color = fixed4(_Color.r - lerpVal+0.1, lerpVal+0.1, 1.0f, lerpVal);
			o.color.rgb = _Color;
			o.color.a = 1.0;
			//o.color = _Color;
			// Position
			o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].position, 1.0f));

			return o;
		}

		float4 frag(PS_INPUT i) : COLOR
		{
			return i.color;
		}


		ENDCG
		}
	}
	FallBack Off
}