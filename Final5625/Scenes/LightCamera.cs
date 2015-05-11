using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    class LightCamera : Camera
    {

        float fovy;
        float aspectRatio;
        float nearClip;
        float farClip;

        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;
        Vector3 worldSpacePos;

        public LightCamera(float fovy, float aspectRatio, float nearClip, float farClip)
        {
            this.fovy = fovy;
            this.aspectRatio = aspectRatio;
            this.nearClip = nearClip;
            this.farClip = farClip;
            this.projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fovy, aspectRatio, nearClip, farClip);
            this.viewMatrix = Matrix4.Identity;
            this.worldSpacePos = Vector3.Zero;
        }

        public void computeFrame()
        {

        }

        public void transformPointLights(List<PointLight> lights)
        {

        }


        public Matrix4 getProjectionMatrix()
        {
            return projectionMatrix;
        }

        public Matrix4 getViewMatrix()
        {
            return viewMatrix;
        }

        public Vector3 getWorldSpacePos()
        {
            return worldSpacePos;
        }

        public float getNearPlane()
        {
            return nearClip;
        }

        public float getFarPlane()
        {
            return farClip;
        }

        public void update(PointLight light, Matrix4 viewMatrix)
        {
            worldSpacePos = light.worldPosition;
            this.viewMatrix = viewMatrix;
        }

        public void setPointLightUniforms(ShaderProgram program)
        {
        }

    }
}
