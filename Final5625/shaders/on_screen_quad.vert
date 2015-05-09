/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 140

in vec3 vert_position;
in vec2 vert_texCoord;

uniform vec4 locationAndDimensions;

out vec2 geom_texCoord;

void main()
{
	vec3 location = vec3(locationAndDimensions.xy, 0);
	vec3 dimensions = vec3(locationAndDimensions.zw, 0);

	vec3 position = location + (dimensions * vert_position);

	gl_Position = vec4(position,1);
	geom_texCoord = vert_texCoord;
}