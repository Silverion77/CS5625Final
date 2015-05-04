#version 140

uniform sampler2D tex;
uniform int mipmapLevel;
uniform float bloomWeight;

out vec4 FragColor;

//Radius of the gaussian filter. Standard deviation will be half this value.
uniform int width = 15;

void main()
{
	float totalWeight = 0;
	FragColor = vec4(0,0,0,0);
	for(int i = -width; i <= width; i++){
		float weight = exp(-1.0 * i*i / (0.5 * width * width)); 
		vec3 texel = textureLod(tex, vec2(gl_FragCoord.xy + vec2(0, i) * pow(2.0, float(mipmapLevel))) / textureSize(tex, 0), mipmapLevel).rgb;
		FragColor.rgb += max(texel, 0.0) * weight;
		totalWeight += weight;
	}
	FragColor.rgb *= bloomWeight / totalWeight;
}