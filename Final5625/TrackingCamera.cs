using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Scenes;
using Chireiden.Scenes.Stages;

namespace Chireiden
{
    /// <summary>
    /// A camera that is passed a transformable object on construction.
    /// Its lookAt position then stays fixed on that node's position,
    /// while its eye position is always some configurable distance behind it.
    /// The eye position swings around the target following the mouse.
    /// </summary>
    public class TrackingCamera : Camera
    {
        public float MouseSensitivity = 0.01f;
        public float distanceBehind = 5;
        public float MouseWheelSensitivity = 0.5f;

        float pitch = 0;
        float yaw = 0;

        float fovy;
        float aspectRatio;
        float nearClip;
        float farClip;

        float rumbleMag;
        const float RUMBLE_DISTANCE_MAX_THRESHOLD = 10000;
        const float RUMBLE_DISTANCE_MIN_THRESHOLD = 100;
        const int RUMBLE_MIN_TIME = 40;
        const int RUMBLE_MAX_TIME = 75;
        int rumbleTime = 0;
        int rumbleMaxTime = RUMBLE_MAX_TIME;

        Vector3 forward;
        Vector3 right;
        Vector3 up = Vector3.UnitZ;

        Matrix4 projectionMatrix;
        Matrix4 viewMatrix;

        MobileObject target;

        int num_lights;
        Vector3[] light_eyePosition;
        float[] light_falloffDistance;
        float[] light_energy;
        Vector3[] light_color;

        Vector3 worldSpacePos;

        Stage stage;

        public Vector3 WorldPosition { get { return worldSpacePos; } }

        bool frozen;

        public TrackingCamera(MobileObject target, float fovy, float aspectRatio, float nearClip, float farClip)
        {
            this.fovy = fovy;
            this.aspectRatio = aspectRatio;
            this.nearClip = nearClip;
            this.farClip = farClip;
            this.target = target;

            light_eyePosition = new Vector3[ShaderProgram.MAX_LIGHTS];
            light_falloffDistance = new float[ShaderProgram.MAX_LIGHTS];
            light_energy = new float[ShaderProgram.MAX_LIGHTS];
            light_color = new Vector3[ShaderProgram.MAX_LIGHTS];

            this.projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(fovy, aspectRatio, nearClip, farClip);
            computeFrame();

            frozen = false;
        }

        public void setStage(Stage s)
        {
            stage = s;
        }

        public Matrix4 getProjectionMatrix()
        {
            return projectionMatrix;
        }

        /// <summary>
        /// Computes the forward and right vectors for the current camera position and lookAt,
        /// and uses these to compute the new view matrix.
        /// </summary>
        public void computeFrame()
        {
            Vector3 lookAt = target.camFocusPosition();

            // The default offset is "behind" the target position in world space terms
            Vector3 offset = new Vector3(0, -1, 0);

            // First rotate by the pitch angle (around global up axis)
            Quaternion pitchRotation = Quaternion.FromAxisAngle(up, pitch + getRumbleOffset(10000));
            offset = Vector3.Transform(offset, pitchRotation);

            // Now compute rightward vector -- distance from camera to lookAt = negation of offset
            Vector3 right = Vector3.Cross(-offset, up);
            // Rotate by yaw angle (around right axis)
            Quaternion yawRotation = Quaternion.FromAxisAngle(right, yaw + getRumbleOffset(0));
            offset = Vector3.Transform(offset, yawRotation);

            this.right = right;
            this.forward = -offset;

            Vector3 rotatedOffset = Vector3.Multiply(forward, distanceBehind);
            worldSpacePos = lookAt - rotatedOffset;

            // Add an over-the-shoulder offset
            worldSpacePos = worldSpacePos + target.camRightOffset() * right;
            lookAt = lookAt + target.camRightOffset() * right;

            // Also, let's make the camera not clip through the world
            if (stage != null)
            {
                if (!stage.worldLocInBounds(worldSpacePos) || worldSpacePos.Z < 0)
                {
                    // If we're not in bounds, we need to do a projection
                    Vector3 worldSpaceCorrected = stage.computeCollisionTime(lookAt, worldSpacePos);
                    worldSpaceCorrected = 1.1f * worldSpaceCorrected - 0.1f * worldSpacePos;
                    if (!worldSpaceCorrected.Equals(lookAt))
                        worldSpacePos = worldSpaceCorrected;
                }
            }

            viewMatrix = Matrix4.LookAt(worldSpacePos, lookAt, up);

            rumbleTime--;
        }

        public void startRumble(float distance)
        {

            // lerp distance between 1 for close and 0 for far
            if (distance > RUMBLE_DISTANCE_MAX_THRESHOLD) {
                return;
            }
            if (distance < RUMBLE_DISTANCE_MIN_THRESHOLD) {
                distance = RUMBLE_DISTANCE_MIN_THRESHOLD;
            }
            float magnitude = (RUMBLE_DISTANCE_MAX_THRESHOLD - distance)/((float)((RUMBLE_DISTANCE_MAX_THRESHOLD - RUMBLE_DISTANCE_MIN_THRESHOLD)));
            magnitude = (float)Math.Max(Math.Min(magnitude, 1.0), 0.0);

            int newRumbleTime = RUMBLE_MIN_TIME + (int)(magnitude * (RUMBLE_MAX_TIME - RUMBLE_MIN_TIME));

            if (newRumbleTime > rumbleTime)
            {
                rumbleMaxTime = newRumbleTime;
                rumbleTime = newRumbleTime;
                rumbleMag = magnitude;
            }

        }

        // Given a seed, computes a perlin noise rumble offset appropriate
        // for the recency of the last explosion
        public float getRumbleOffset(float t) 
        {
            if (rumbleTime > 0) 
            {
                float x = rumbleTime/((float)rumbleMaxTime);
                // compute some non-zero offset damped by rumbletime and scaled by magnitude
                return (3*x*x - 2*x * x*x) * Utils.perlineNoise1D((t + rumbleTime)*.1f, .5f, 6) * rumbleMag * .15f;
            }
            // otherwise, too much time since last explosion , clamp to 0
            return 0.0f;
        }

        public Vector3 cameraForwardDir()
        {
            return (20 * forward + target.camRightOffset() * right).Normalized();
        }

        public Matrix4 getViewMatrix()
        {
            return viewMatrix;
        }

        public void transformPointLights(List<PointLight> lights)
        {
            num_lights = Math.Min(lights.Count, ShaderProgram.MAX_LIGHTS);
            int i = 0;
            foreach (PointLight light in lights)
            {
                if (i >= ShaderProgram.MAX_LIGHTS) break;
                Vector3 worldPos = light.worldPosition;
                Vector3 eyeSpace = Vector4.Transform(new Vector4(worldPos, 1), viewMatrix).Xyz;
                light_eyePosition[i] = eyeSpace;
                light_falloffDistance[i] = light.FalloffDistance;
                light_energy[i] = light.Energy;
                light_color[i] = light.Color;
                i++;
            }
        }

        public void setPointLightUniforms(ShaderProgram program)
        {
            program.setUniformInt1("light_count", num_lights);
            program.setUniformVec3Array("light_eyePosition", light_eyePosition);
            program.setUniformFloatArray("light_falloffDistance", light_falloffDistance);
            program.setUniformFloatArray("light_energy", light_energy);
            program.setUniformVec3Array("light_color", light_color);
        }

        public void copyRotation(TrackingCamera other)
        {
            this.pitch = other.pitch;
            this.yaw = other.yaw;
        }

        /// <summary>
        /// Toggles mouselook rotation of the camera on and off. If off, the camera will
        /// remain in its current offset, but will continue to track with the target object.
        /// </summary>
        /// <param name="freeze"></param>
        public void toggleCameraFrozen()
        {
            frozen = !frozen;
        }

        // Courtesy of http://neokabuto.blogspot.com/2014/01/opentk-tutorial-5-basic-camera.html
        /// <summary>
        /// Given the change in the mouse position, rotates the camera to track with the mouse movement.
        /// </summary>
        /// <param name="x">Distance moved by mouse in screen X direction</param>
        /// <param name="y">Distance moved by mouse in screen Y direction</param>
        public void addRotation(float x, float y)
        {
            if (frozen) return;
            x = -x * MouseSensitivity;
            y = -y * MouseSensitivity;

            pitch = (pitch + x) % ((float)Math.PI * 2.0f);
            yaw = Math.Max(Math.Min(yaw + y, (float)Math.PI / 3.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f);
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
            Vector3 rightParallelUp = Vector3.Dot(right, up) * up;
            Vector3 forwardParallelUp = Vector3.Dot(forward, up) * up;
            Vector3 rightProj = Vector3.Normalize(right - rightParallelUp);
            Vector3 forwardProj = Vector3.Normalize(forward - forwardParallelUp);

            offset = offset + x * rightProj;
            offset = offset + y * forwardProj;

            if (offset.Equals(Vector3.Zero)) return offset;

            offset.Normalize();
            return offset;
        }

        public void zoom(float amount)
        {
            distanceBehind += amount * MouseWheelSensitivity;
            distanceBehind = Math.Max(Math.Min(distanceBehind, 20), 1);
        }

        public Vector3 getWorldSpacePos()
        {
            return WorldPosition;
        }

        public float getNearPlane()
        {
            return nearClip;
        }

        public float getFarPlane()
        {
            return farClip;
        }
    }
}
