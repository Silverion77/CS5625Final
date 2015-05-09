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

uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;
uniform vec2 dimensions;

out vec2 geom_position;
out vec2 geom_texCoord;

void main()
{
	geom_position = vert_position.xy * dimensions;
	vec3 position = vert_position * vec3(dimensions, 1);
	gl_Position = projectionMatrix * viewMatrix * vec4(position,1);
	geom_texCoord = vert_texCoord * dimensions;
}
