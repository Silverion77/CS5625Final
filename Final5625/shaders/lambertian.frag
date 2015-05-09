/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 140

#define MAX_LIGHTS 40

in vec3 geom_position;
in vec3 geom_normal;
in vec2 geom_texCoord;

uniform vec4 mat_diffuseColor;
uniform bool mat_hasTexture;
uniform sampler2D texture;
uniform vec2 texScale;

uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform float light_falloffDistance[MAX_LIGHTS];
uniform float light_energy[MAX_LIGHTS];
uniform vec3 light_color[MAX_LIGHTS];

out vec4 out_frag_color;

void main()
{
	vec4 diffuse = mat_diffuseColor;
	vec4 tex = vec4(0, 0, 0, 1);
	if (mat_hasTexture) {
		tex = texture2D(texture, geom_texCoord / texScale);
		diffuse = diffuse * tex;
	}

	vec4 result = vec4(0,0,0,diffuse.a);
	vec3 n = normalize(geom_normal);

	for (int i=0; i<light_count; i++) {

		vec3 l = light_eyePosition[i] - geom_position;
		float r_squared = dot(l, l);
		l = normalize(l);

		// The attenuation model used here is from http://wiki.blender.org/index.php/Doc:2.6/Manual/Lighting/Lights/Light_Attenuation
		float d2 = light_falloffDistance[i] * light_falloffDistance[i];
		float intensity = light_energy[i] * (d2 / (d2 + r_squared));

		float dotProd = max(dot(n,l), 0);
		result.xyz += diffuse.xyz * dotProd * light_color[i] * intensity;
	}
	
	out_frag_color = result;
}
