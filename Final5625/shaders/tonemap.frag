#version 430

uniform sampler2D colorBuffer;
uniform sampler2D logLuminance;
uniform float middleGrey = 0.18;
uniform float L_white = 2.0;

out vec4 FragColor;

void main()
{
	float avgLuminance = exp(texelFetch(logLuminance, ivec2(0,0), textureQueryLevels(logLuminance)-1).r);
	float exposure = middleGrey / avgLuminance;

	vec3 Lw = texelFetch(colorBuffer, ivec2(gl_FragCoord.xy), 0).rgb;
	vec3 L = exposure * Lw;
	FragColor = vec4(L * (1.0 + L/(L_white*L_white)) / (1 + L), 1);
}