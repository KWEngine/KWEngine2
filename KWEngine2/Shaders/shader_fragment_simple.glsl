#version 430

uniform vec4 uBaseColor;

out vec4 color;
out vec4 bloom;

void main()
{
	color = uBaseColor;
	bloom = vec4(0.0);
}