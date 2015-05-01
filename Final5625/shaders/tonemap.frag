#version 140

uniform sampler2D colorBuffer;
uniform float exposure = 1.0; //computed as middle grey / average(log(luminance))
uniform float L_white = 2.0;

out vec4 FragColor;

void main()
{
	vec3 Lw = texelFetch(colorBuffer, ivec2(gl_FragCoord.xy), 0).rgb;
	vec3 L = exposure * Lw;

	FragColor = vec4(L * (1.0 + L/(L_white*L_white)) / (1 + L), 1);
}