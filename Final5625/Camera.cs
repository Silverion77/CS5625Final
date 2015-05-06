using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Scenes;

namespace Chireiden
{
    public interface Camera
    {
        /// <summary>
        /// Returns the projection matrix of this camera.
        /// </summary>
        /// <returns></returns>
        Matrix4 getProjectionMatrix();

        /// <summary>
        /// Computes up and right vectors for this camera, in preparation for
        /// computing the view matrix.
        /// </summary>
        void computeFrame();

        /// <summary>
        /// Returns the viewing matrix of this camera. This function can assume
        /// that computeFrame has already been called.
        /// </summary>
        /// <returns></returns>
        Matrix4 getViewMatrix();

        /// <summary>
        /// Given a list of point lights, transforms their positions and stores
        /// their attributes into arrays that are ready to be passed to OpenGL.
        /// </summary>
        /// <param name="lights"></param>
        void transformPointLights(List<PointLight> lights);

        /// <summary>
        /// Assuming that transformPointLights has been previously called this frame,
        /// binds all of the light uniforms for the given shader program.
        /// </summary>
        void setPointLightUniforms(ShaderProgram program);

        /// <returns>position of camera in world space</returns>
        Vector3 getWorldSpacePos();

    }
}
