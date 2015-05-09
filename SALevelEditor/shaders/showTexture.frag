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

in vec2 geom_position;
in vec2 geom_texCoord;

uniform sampler2DRect texture;

out vec4 out_frag_color;

float distFromEdge(vec2 point) {
	float fractX = fract(geom_position.x);
	float distX = min(fractX, abs(1 - fractX));
	float fractY = fract(geom_position.y);
	float distY = min(fractY, abs(1 - fractY));
	return min(distX, distY);
}

void main() {
	float dist = distFromEdge(geom_position);
	if (dist < 0.05) {
		out_frag_color = vec4(0.5, 0.5, 0.5, 1);
	}
	else {
		out_frag_color = texture2DRect(texture, floor(geom_texCoord) + 0.5);
	}
}