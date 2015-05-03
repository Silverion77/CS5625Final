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
    public class ArmatureBone
    {
        // Displacement from parent bone in the rest pose.
        Matrix4 restPoseMatrix;

        // Displacements from parent bone in animated poses.
        Matrix4 poseTranslation;
        Matrix4 poseRotation;

        public ArmatureBone Parent { get; set; }
        public List<ArmatureBone> Children { get; set; }
        public string Name { get; set; }

        public ArmatureBone(Matrix4 bindPoseTransform, string name)
        {
            restPoseMatrix = bindPoseTransform;
            poseTranslation = Matrix4.Identity;
            poseRotation = Matrix4.Identity;

            Children = new List<ArmatureBone>();
            Parent = null;
            Name = name;
        }

        public void addChild(ArmatureBone child)
        {
            if (child != null)
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        public void removeChild(ArmatureBone child)
        {
            if (child != null && Children.Remove(child))
            {
                child.Parent = null;
            }
        }

        void print(int level)
        {
            string spaces = new string(' ', level * 2);
            Console.WriteLine("{0}{1}", spaces, Name);
            foreach (ArmatureBone b in Children)
            {
                b.print(level + 1);
            }
        }

        public void printBoneTree()
        {
            print(0);
        }
    }
}
