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
        public float MouseSensitivity = 0.01f;
        public float distanceBehind = 5;

        float pitch = 0;
        float yaw = 0;

        float fovy;
        float aspectRatio;
        float nearClip;
        float farClip;

        Vector3 forward;
        Vector3 right;

        Matrix4 projectionMatrix;

        MobileObject target;

        public TrackingCamera(MobileObject target, float fovy, float aspectRatio, float nearClip, float farClip)
        {
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

        /// <summary>
        /// Computes the forward and right vectors for the current camera position and lookAt.
        /// </summary>
        public void computeFrame()
        {
            Vector3 lookAt = target.worldPosition;

            // The default offset is (-1, 0, 0) to the target position
            Vector3 offset = new Vector3(-1, 0, 0);

            // First rotate by the pitch angle (around global up axis)
            Quaternion pitchRotation = Quaternion.FromAxisAngle(Vector3.UnitY, pitch);
            offset = Vector3.Transform(offset, pitchRotation);

            // Now compute rightward vector -- distance from camera to lookAt = negation of offset
            Vector3 right = Vector3.Cross(-offset, Vector3.UnitY);
            // Rotate by yaw angle (around right axis)
            Quaternion yawRotation = Quaternion.FromAxisAngle(right, yaw);
            offset = Vector3.Transform(offset, yawRotation);

            this.right = right;
            this.forward = -offset;
        }

        public Matrix4 getViewMatrix()
        {
            // Assumes that computeFrame() has already been called this timestep,
            // so this is really quite simple
            Vector3 offset = Vector3.Multiply(forward, distanceBehind);

            Matrix4 lookAtMat = Matrix4.LookAt(target.worldPosition - offset, target.worldPosition, Vector3.UnitY);

            return lookAtMat;
        }

        // Courtesy of http://neokabuto.blogspot.com/2014/01/opentk-tutorial-5-basic-camera.html
        /// <summary>
        /// Given the change in the mouse position, rotates the camera to track with the mouse movement.
        /// </summary>
        /// <param name="x">Distance moved by mouse in screen X direction</param>
        /// <param name="y">Distance moved by mouse in screen Y direction</param>
        public void addRotation(float x, float y)
        {
            x = -x * MouseSensitivity;
            y = -y * MouseSensitivity;

            pitch = (pitch + x) % ((float)Math.PI * 2.0f);
            yaw = Math.Max(Math.Min(yaw + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
        }

        /// <summary>
        /// Given a direction in screen space (with X right, Y up, Z into the screen),
        /// transforms that into a movement direction for the tracked object in world space.
        /// </summary>
        /// <param name="x">Right direction in screen space; translates to rightward movement</param>
        /// <param name="y">Up direction in screen space; translates to forward movement</param>
        /// <returns></returns>
        public Vector3 getMovementVector(float x, float y)
        {
            Vector3 offset = Vector3.Zero;

            // Project the directions down to the ground plane
            Vector3 rightParallelY = Vector3.Dot(right, Vector3.UnitY) * Vector3.UnitY;
            Vector3 forwardParallelY = Vector3.Dot(forward, Vector3.UnitY) * Vector3.UnitY;
            Vector3 rightProj = Vector3.Normalize(right - rightParallelY);
            Vector3 forwardProj = Vector3.Normalize(forward - forwardParallelY);

            offset = offset + x * rightProj;
            offset = offset + y * forwardProj;
            offset.Normalize();
            return offset;
        }
    }
}
