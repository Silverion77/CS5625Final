using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    /// <summary>
    /// A camera that is passed a transformable object on construction.
    /// Its lookAt position then stays fixed on that node's position,
    /// while its eye position is always some configurable distance behind it.
    /// The eye position swings around the target following the mouse.
    /// </summary>
    class TrackingCamera
    {
        public float MoveSpeed = 0.2f;
        public float MouseSensitivity = 0.01f;
        public float distanceBehind = 5;

        Vector3 position;
        Vector3 orientation;
        Vector3 offset;

        float fovy;
        float aspectRatio;
        float nearClip;
        float farClip;

        Matrix4 projectionMatrix;

        TransformableObject target;

        public TrackingCamera(TransformableObject target, float fovy, float aspectRatio, float nearClip, float farClip)
        {
            offset = new Vector3();
            position = new Vector3();
            orientation = new Vector3((float)Math.PI, 0f, 0f);
            this.fovy = fovy;
            this.aspectRatio = aspectRatio;
            this.nearClip = nearClip;
            this.farClip = farClip;
            this.target = target;

            this.projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fovy, aspectRatio, nearClip, farClip);
        }

        public Matrix4 getProjectionMatrix()
        {
            return projectionMatrix;
        }

        // Courtesy of http://neokabuto.blogspot.com/2014/01/opentk-tutorial-5-basic-camera.html
        public Matrix4 getViewMatrix()
        {
            Vector3 lookAt = target.worldPosition;

            offset.X = (float)(Math.Sin((float)orientation.X) * Math.Cos((float)orientation.Y));
            offset.Y = (float)Math.Sin((float)orientation.Y);
            offset.Z = (float)(Math.Cos((float)orientation.X) * Math.Cos((float)orientation.Y));
            offset = Vector3.Multiply(offset, distanceBehind);

            return Matrix4.LookAt(lookAt - offset, lookAt, Vector3.UnitY);
        }

        // Courtesy of http://neokabuto.blogspot.com/2014/01/opentk-tutorial-5-basic-camera.html
        public void AddRotation(float x, float y)
        {
            x = x * MouseSensitivity;
            y = y * MouseSensitivity;

            orientation.X = (orientation.X + x) % ((float)Math.PI * 2.0f);
            orientation.Y = Math.Max(Math.Min(orientation.Y + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }
    }
}
