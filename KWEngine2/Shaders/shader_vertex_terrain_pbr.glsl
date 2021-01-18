#version 430

in		vec3 aPosition;
in		vec2 aTexture;
in		vec3 aNormal;
in		vec3 aTangent;
in		vec3 aBiTangent;

out		vec3 vPosition;
out		vec3 vNormal;
out		vec2 vTexture;
out		vec2 vTextureNoRepetitions;
out		mat3 vTBN;
out		vec4 vShadowCoord;
out		vec4 vShadowCoord2;

uniform mat4 uMVP;
uniform mat4 uNormalMatrix;
uniform mat4 uModelMatrix;
uniform mat4 uMVPShadowMap;
uniform mat4 uMVPShadowMap2;
uniform vec2 uTextureTransform;

void main()
{
	vec4 totalLocalPos = vec4(0.0);
	vec4 totalNormal = vec4(0.0);
	vec4 totalTangent = vec4(0.0);
	vec4 totalBiTangent = vec4(0.0);
	
	
	totalLocalPos = vec4(aPosition, 1.0);
	totalNormal = vec4(aNormal, 0.0);
	totalTangent = vec4(aTangent, 0.0);
	totalBiTangent = vec4(aBiTangent, 0.0);
	

	
	// normal mapping:
	vNormal = normalize((uNormalMatrix * totalNormal).xyz);
	vec3 vTangent = normalize((uNormalMatrix * totalTangent).xyz);
	vec3 vBiTangent = normalize((uNormalMatrix * totalBiTangent).xyz);

	// Other pass-throughs:
	vTexture = aTexture * uTextureTransform;
	vTextureNoRepetitions = aTexture;
	vPosition = (uModelMatrix * totalLocalPos).xyz;
	vShadowCoord = uMVPShadowMap * totalLocalPos;
	vShadowCoord2 = uMVPShadowMap2 * totalLocalPos;
	vTBN = mat3(vTangent.xyz, vBiTangent.xyz, vNormal.xyz);
	
	gl_Position = uMVP * totalLocalPos;
}