using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    class ArmatureBone
    {
        // Displacement from parent bone in the rest pose.
        Matrix4 restPoseMatrix;

        // Displacements from parent bone in animated poses.
        Matrix4 poseTranslation;
        Matrix4 poseRotation;

        public ArmatureBone(Matrix4 bindPoseTransform)
        {
            restPoseMatrix = bindPoseTransform;
            poseTranslation = Matrix4.Identity;
            poseRotation = Matrix4.Identity;
        }
    }
}
