using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    /// <summary>
    /// An object in the scene tree that can be subjected to translations, rotations, and scaling.
    /// </summary>
    public abstract class PlaceableObject : SceneTreeNode
    {
        protected float scale;
        protected Vector3 translation;
        protected Quaternion rotation;
        public Vector3 worldPosition;

        static Vector4 localCenter = new Vector4(0, 0, 0, 1);

        public PlaceableObject(float s, Vector3 t, Quaternion r) : base()
        {
            scale = s;
            translation = t;
            rotation = r;
            worldPosition = Vector3.Zero;
        }

        public PlaceableObject(Vector3 translation)
            : this(1, translation, Quaternion.Identity)
        { }

        public PlaceableObject()
            : this(1, new Vector3(0, 0, 0), Quaternion.Identity)
        { }

        public Matrix4 modelMatrix()
        {
            Matrix4 scaleMat = Matrix4.CreateScale(scale);
            Matrix4 translationMat = Matrix4.CreateTranslation(translation);
            Matrix4 rotationMat = Matrix4.CreateFromQuaternion(rotation);
            Matrix4 scaleRot = Matrix4.Mult(scaleMat, rotationMat);
            return Matrix4.Mult(scaleRot, translationMat);
        }

        protected void updateMatricesAndWorldPos(Matrix4 parentToWorldMatrix)
        {
            toParentMatrix = modelMatrix();
            toWorldMatrix = Matrix4.Mult(toParentMatrix, parentToWorldMatrix);
            // The center of the local space object is (0, 0, 0), so we transform
            // that to world space.
            worldPosition = Vector4.Transform(localCenter, toWorldMatrix).Xyz;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            updateMatricesAndWorldPos(parentToWorldMatrix);
            updateChildren(e, toWorldMatrix);
        }

        /// <summary>
        /// Rotates this object around the given axis, by the given angle.
        /// </summary>
        public void addRotation(Vector3 axis, float angle)
        {
            Quaternion toCompose = Quaternion.FromAxisAngle(axis, angle);
            Quaternion.Multiply(ref toCompose, ref rotation, out rotation);
        }

        /// <summary>
        /// Returns the direction in which this object is facing.
        /// </summary>
        /// <returns></returns>
        public Vector3 getFacingDirection()
        {
            return Vector3.Transform(Utils.FORWARD, rotation);
        }
    }
}
