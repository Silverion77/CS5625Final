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
    /// <summary>
    /// A tree of bones that defines a skeleton which can be animated.
    /// </summary>
    public class ArmatureBone
    {
        // Displacement from local origin to parent bone's origin in the rest pose.
        Matrix4 restTranslation;

        // Displacement from local origin to world space position in the rest pose.
        Matrix4 restToWorldMatrix;

        // The inverse of restToWorldMatrix.
        Matrix4 inverseRestMatrix;

        // Transformations from local origin to parent in the current pose.
        Matrix4 poseTranslation;
        Matrix4 poseRotation;

        // Transformation from local origin to world space position in the current pose.
        Matrix4 poseToWorldMatrix;

        // Complete transformation that undoes the rest pose and applies the current pose.
        Matrix4 boneMatrix;

        public ArmatureBone Parent { get; set; }
        public List<ArmatureBone> Children { get; set; }
        public string Name { get; set; }
        public int BoneID { get; set; }

        public ArmatureBone(Matrix4 bindPoseTransform, string name)
        {
            restTranslation = bindPoseTransform;
            poseTranslation = Matrix4.Identity;
            poseRotation = Matrix4.Identity;

            Children = new List<ArmatureBone>();
            Parent = null;
            Name = name;
            BoneID = -1;
        }

        public void setPoseTranslation(Matrix4 newTranslation)
        {
            poseTranslation = newTranslation;
        }

        public void setPoseRotation(Matrix4 newRotation)
        {
            poseRotation = newRotation;
        }

        public Matrix4 getBoneMatrix()
        {
            return boneMatrix;
        }

        /// <summary>
        /// Sets all of the restToWorldMatrix fields in this tree to contain the matrix
        /// transforming the origin of this bone to its position in world space, for the rest pose.
        /// For all bones that are not the root, assumes that the parent's
        /// restToWorldMatrix has already computed.
        /// </summary>
        public void computeAllRestTransforms()
        {
            // If we are the the root bone, then our restPoseMatrix tells us how to transform to world space.
            // So we're already done, as far as computing the transform goes.
            if (Parent == null)
            {
                restToWorldMatrix = restTranslation;
            }
            else
            {
                // Otherwise, our restPoseMatrix tells us how to transform from our local space
                // to our parent's local space. We can also assume that our parent's local-to-world-space
                // matrix has already been computed.
                Matrix4 parentToWorldMatrix = Parent.restToWorldMatrix;
                Matrix4.Mult(ref restTranslation, ref parentToWorldMatrix, out restToWorldMatrix);
            }
            // Either way, invert the rest pose matrix, because we'll need it later
            Matrix4.Invert(ref restToWorldMatrix, out inverseRestMatrix);

            // Finally, recursively compute the same thing for our children.
            foreach (ArmatureBone child in Children)
            {
                child.computeAllRestTransforms();
            }
        }

        /// <summary>
        /// Sets all of the poseToWorldMatrix fields in this tree to contain the matrix
        /// transforming the origin of this bone to its position in world space, for the current pose
        /// as defined by the fields poseTranslation and poseRotation.
        /// For all bones that are not the root, assumes that the parent's
        /// poseToWorldMatrix has already computed.
        /// </summary>
        public void computeAllTransforms()
        {
            // Compute this bone's local-to-parent transformation matrix.
            Matrix4 poseLocalToParent;
            Matrix4.Mult(ref poseRotation, ref poseTranslation, out poseLocalToParent);
            Matrix4.Mult(ref poseLocalToParent, ref restTranslation, out poseLocalToParent);

            // If we have no parent, then this transforms straight to world space,
            // so we can stop here.
            if (Parent == null)
            {
                poseToWorldMatrix = poseLocalToParent;
            }
            // If we have a parent, then we can use the parent's local-to-world-space
            // which was already computed.
            else 
            {
                Matrix4 parentToWorldMatrix = Parent.poseToWorldMatrix;
                Matrix4.Mult(ref poseLocalToParent, ref parentToWorldMatrix, out poseToWorldMatrix);
            }

            // Compute the bone matrix that can be directly used for linear blend skinning
            Matrix4.Mult(ref inverseRestMatrix, ref poseToWorldMatrix, out boneMatrix);

            // Recursively do the same for all children.
            foreach (ArmatureBone child in Children)
            {
                child.computeAllTransforms();
            }
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

        /// <summary>
        /// Creates a deep copy of this skeleton.
        /// This is necessary because, even if we have multiple copies of the
        /// same object with the same rig, they can't share the same instances of
        /// ArmatureBones, because if they did, they would always have the same pose
        /// transformations, so we wouldn't be able to animate them separately.
        /// </summary>
        /// <returns></returns>
        public ArmatureBone cloneTree()
        {
            // Strictly speaking I don't think that the above is true -- we can probably
            // concoct some scheme to share the same objects, but I'll have to see
            // how much trouble it will be.
            ArmatureBone newRoot = new ArmatureBone(this.restTranslation, this.Name);
            newRoot.BoneID = this.BoneID;
            foreach (ArmatureBone ourChild in this.Children)
            {
                ArmatureBone otherChild = ourChild.cloneTree();
                newRoot.addChild(otherChild);
            }
            return newRoot;
        }
    }
}
