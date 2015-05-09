using System;
using System.Collections.Generic;

using Chireiden;

using OpenTK;

namespace SALevelEditor
{
    public class OrthoCamera : Camera
    {
        Vector3 position;
        float aspectRatio;
        float nearClip;
        float farClip;

        float zoomSensitivity = 0.5f;
        float moveSpeed = 3f;
        float height = 1;

        public float Height { get { return height; } }
        public float Width { get { return aspectRatio * height; } }

        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;

        public OrthoCamera(Vector3 pos, float aspectRatio, float nearClip, float farClip)
        {
            position = pos;
            this.nearClip = nearClip;
            this.farClip = farClip;
            this.aspectRatio = aspectRatio;
        }

        public Matrix4 getProjectionMatrix()
        {
            return projectionMatrix;
        }

        public float getFarPlane()
        {
            return farClip;

        }

        public float getNearPlane()
        {
            return nearClip;
        }

        public void computeFrame()
        {
            projectionMatrix = Matrix4.CreateOrthographic(Width, Height, nearClip, farClip);
            Vector3 lookAt = position - Vector3.UnitZ;
            viewMatrix = Matrix4.LookAt(position, lookAt, Vector3.UnitY);
        }

        public Vector3 getClickedLoc(double x, double y)
        {
            Vector4 screenPos = new Vector4((float)x, (float)y, -1, 1);
            Matrix4 inverseProj = projectionMatrix.Inverted();
            Matrix4 inverseView = viewMatrix.Inverted();
            Vector4 eyePos = Vector4.Transform(screenPos, inverseProj);
            Vector4 worldPos = Vector4.Transform(eyePos, inverseView);
            return worldPos.Xyz;
        }

        public void move(float dx, float dy, double time)
        {
            position.X += dx * moveSpeed * Width / 20 * (float)time;
            position.Y += dy * moveSpeed * Height / 20 * (float)time;
        }

        public void elevate(float dz, double time)
        {
            height += dz * zoomSensitivity;
            height = Math.Max(Math.Min(height, 100), 1);
        }

        public Matrix4 getViewMatrix()
        {
            return viewMatrix;
        }

        public void transformPointLights(List<Chireiden.Scenes.PointLight> lights)
        {
            // Lights are irrelevant when we're looking at the map
        }

        public void setPointLightUniforms(ShaderProgram program)
        {
            // Same
        }

        public Vector3 getWorldSpacePos()
        {
            return position;
        }
    }
}
