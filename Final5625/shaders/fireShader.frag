#version 330

in vec2 in_TexCoord;

// uniform
uniform sampler2D un_NoiseTexture;
uniform sampler2D un_FireTexture;
uniform float un_Time;
uniform vec3 un_ScrollSpeeds;

void main()
{
	float y = mod(in_TexCoord.y + un_Time * un_ScrollSpeeds.x, 1);
	vec4 set1 = texture(un_NoiseTexture, vec2(in_TexCoord.x, y));
	y = mod(in_TexCoord.y * 2 + un_Time * un_ScrollSpeeds.y, 1);
	vec4 set2 = texture(un_NoiseTexture, vec2(mod(in_TexCoord.x * 2, 1), y ));
	y = mod(in_TexCoord.y * 3 + un_Time * un_ScrollSpeeds.z, 1);
	vec4 set3 = texture(un_NoiseTexture, vec2(mod(in_TexCoord.x * 3, 1), y));
	float avgX = (set1.x + set2.x + set3.x) / 3;
	float avgY = (set1.y + set2.y + set3.y) / 3;
	gl_FragColor = texture(un_FireTexture, vec2(mod(avgX, 1), mod(avgY, 1)));
}
