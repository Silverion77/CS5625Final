/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 140
#extension GL_ARB_texture_rectangle : enable

#define MAX_MORPHS 20

in vec3 vert_position;
in vec3 vert_normal;
in vec2 vert_texCoord;
in vec4 vert_tangent;
in vec4 vert_boneIDs;
in vec4 vert_boneWeights;
in float vert_morphStart;
in float vert_morphCount;

uniform mat4 modelViewMatrix;
uniform mat4 projectionMatrix;
uniform mat3 normalMatrix;

uniform float morph_weights[MAX_MORPHS];

uniform sampler2DRect morph_displacements;

uniform sampler2DRect bone_matrices;
uniform int bone_textureWidth;
uniform int bone_textureHeight;

out vec3 geom_position;
out vec3 geom_normal;
out vec2 geom_texCoord;
out vec3 geom_tangent;
out vec3 geom_bitangent;

mat4 getBoneXform(int boneIndex) {
	int startPixelX = boneIndex * 4;

	mat4 result = mat4(0.0);

	float x = startPixelX + 0.5;
	result[0] = texture2DRect(bone_matrices, vec2(x, 0.5));
	result[1] = texture2DRect(bone_matrices, vec2(x+1, 0.5));
	result[2] = texture2DRect(bone_matrices, vec2(x+2, 0.5));
	result[3] = texture2DRect(bone_matrices, vec2(x+3, 0.5));
	return result;
}

float getMorphWeight(float i) {
	return morph_weights[int(i)];
}

vec4 getMorphDisplacementInfo(float x) {
	return texture2DRect(morph_displacements, vec2(x + 0.5, 0.5));
}

void setBS() {
	gl_Position = projectionMatrix *
			(modelViewMatrix * vec4(0,0,0,1));	

	vec3 N = normalize(vert_normal);
	vec3 T = normalize(vert_tangent.xyz);
	vec3 B = normalize(cross(N, T) * vert_tangent.w);
	geom_normal = normalize(modelViewMatrix * vec4(N,0)).xyz;	
	geom_tangent = normalize(modelViewMatrix * vec4(T,0)).xyz;
	geom_bitangent = normalize(modelViewMatrix * vec4(B,0)).xyz;

	geom_position = (modelViewMatrix * vec4(0,0,0,1)).xyz;	
	geom_texCoord = vert_texCoord;
}

void main()
{
	vec3 position = vert_position;

	// TODO: If I can figure out how to get blend shapes in here, it will go here
	// Loop over all applicable morphs
	
	for (int i = 0; i < vert_morphCount; i++) {
		// Get the actual index of the morph in this vertex's block
		float index = vert_morphStart + i;
		// Look up the displacement
		vec4 disp_info = getMorphDisplacementInfo(index);
		float morphID = disp_info.w;
		vec3 displace = disp_info.xyz;
		// Get the weight of this morph
		float weight = getMorphWeight(morphID);
		// Adjust position accordingly
		position += weight * displace;
	}

	vec3 N = normalize(vert_normal);
	vec3 T = normalize(vert_tangent.xyz);
	vec3 B = normalize(cross(N, T) * vert_tangent.w);

	// Compute blended vertex positions from skeleton
	vec3 P = position;
	position = vec3(0);
	vec3 tangent = vec3(0);
	vec3 bitangent = vec3(0);
	vec3 normal = vec3(0);

	float totalWeight = 0;
	
	for (int i = 0; i < 4; i++) {
		int boneID = int(vert_boneIDs[i] + 0.1);
		float boneWeight = vert_boneWeights[i];
		if (boneID >= 0 && boneWeight > 0) {
			mat4 boneTransform = getBoneXform(boneID);

			totalWeight += boneWeight;

			position += boneWeight * (boneTransform * vec4(P, 1)).xyz;
			normal += boneWeight * (boneTransform * vec4(N, 0)).xyz;
			tangent += boneWeight * (boneTransform * vec4(T, 0)).xyz;
			bitangent += boneWeight * (boneTransform * vec4(B, 0)).xyz;
		}
	}

	position /= totalWeight;
	tangent /= totalWeight;
	normal /= totalWeight;
	bitangent /= totalWeight;

	tangent = normalize(tangent);
	bitangent = normalize(bitangent);
	normal = normalize(cross(tangent, bitangent) * vert_tangent.w);

	gl_Position = projectionMatrix *
			(modelViewMatrix * vec4(position,1));

	geom_position = (modelViewMatrix * vec4(position,1)).xyz;
	geom_texCoord = vert_texCoord;

	geom_normal = normalize(normalMatrix * normal);	
	geom_tangent = normalize(modelViewMatrix * vec4(tangent,0)).xyz;
	geom_bitangent = normalize(modelViewMatrix * vec4(bitangent,0)).xyz;
}
