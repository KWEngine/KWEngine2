#version 450

in		vec3 aPosition;
in		vec2 aTexture;
in		vec2 aTexture2;
in		vec3 aNormal;
in		vec3 aTangent;
in		vec3 aBiTangent;
in		ivec3 aBoneIds;
in		vec3 aBoneWeights;

out		vec3 vPosition;
out		vec3 vNormal;
out		vec2 vTexture;
out		vec2 vTexture2;
out		mat3 vTBN;
out		vec4 vShadowCoord[3];

uniform vec2 uTextureTransform;
uniform int uUseAnimations;
uniform mat4 uBoneTransforms[96];

uniform mat4 uMVP;
uniform mat4 uNormalMatrix;
uniform mat4 uModelMatrix;
uniform mat4 uMVPShadowMap[3];

uniform int uLightCount;

const mat4 biasMatrix = mat4(
	0.5, 0.0, 0.0, 0.0,
    0.0, 0.5, 0.0, 0.0,
	0.0, 0.0, 0.5, 0.0,
	0.5, 0.5, 0.5, 1.0
);

void main()
{
	vec4 totalLocalPos = vec4(0.0);
	vec4 totalNormal = vec4(0.0);
	vec4 totalTangent = vec4(0.0);
	vec4 totalBiTangent = vec4(0.0);
	
	if(uUseAnimations > 0)
	{	
		for(int i = 0; i < 3; i++)
		{
			totalLocalPos += aBoneWeights[i] * uBoneTransforms[aBoneIds[i]] * vec4(aPosition, 1.0);
			totalNormal  += aBoneWeights[i] * uBoneTransforms[aBoneIds[i]] * vec4(aNormal, 0.0);
			totalTangent += aBoneWeights[i] * uBoneTransforms[aBoneIds[i]] * vec4(aTangent, 0.0);
			totalBiTangent  += aBoneWeights[i] * uBoneTransforms[aBoneIds[i]] * vec4(aBiTangent, 0.0);
		}
	}
	else
	{
		totalLocalPos = vec4(aPosition, 1.0);
		totalNormal = vec4(aNormal, 0.0);
		totalTangent = vec4(aTangent, 0.0);
		totalBiTangent = vec4(aBiTangent, 0.0);
	}

	
	// normal mapping:
	vNormal = normalize((uNormalMatrix * totalNormal).xyz);
	vec3 vTangent = normalize((uNormalMatrix * totalTangent).xyz);
	vec3 vBiTangent = normalize((uNormalMatrix * totalBiTangent).xyz);

	// Other pass-throughs:
	vTexture = aTexture * uTextureTransform;
	vTexture2 = aTexture2 * uTextureTransform;
	vPosition = (uModelMatrix * totalLocalPos).xyz;
	for(int i = 0; i < 3; i++)
	{
		vShadowCoord[i] = (biasMatrix * uMVPShadowMap[i] * uModelMatrix) * totalLocalPos;
	}
	
	vTBN = mat3(vTangent.xyz, vBiTangent.xyz, vNormal.xyz);
	
	gl_Position = uMVP * totalLocalPos;
}