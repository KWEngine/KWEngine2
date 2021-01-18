#version 430
in		vec2 vTexture;

uniform vec4        uTintColor;
uniform sampler2D   uTextureDiffuse;

out             vec4 color;
out             vec4 bloom;
 
void main()
{
	color = ((texture(uTextureDiffuse, vTexture)) * vec4(uTintColor.xyz, 1.0)) * uTintColor.w;
	
	bloom.x = color.x;
	bloom.y = color.y;
	bloom.z = color.z;
	bloom.w = color.w * 0.1;

}