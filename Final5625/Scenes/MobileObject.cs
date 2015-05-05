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
    /// A placeable object that also has velocity, causing its position
    /// to change over time.
    /// </summary>
    public abstract class MobileObject : PlaceableObject
    {
        /// <summary>
        /// The world space velocity of this object.
        /// </summary>
        protected Vector3 velocity;
        float moveSpeed = 3;

        public MobileObject(float s, Vector3 t, Quaternion r, Vector3 v) : base(s, t, r)
        {
            velocity = v;
        }

        public MobileObject(Vector3 t) : base(t) { }

        public MobileObject()
            : this(1, Vector3.Zero, Quaternion.Identity, Vector3.Zero)
        {}

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * moveSpeed * velocity, 0);
                Vector3 localVel = Vector4.Transform(worldVel, worldToLocal).Xyz;
                translation = Vector3.Add(translation, localVel);
            }
            base.update(e, parentToWorldMatrix);
        }

        public float getMoveSpeed()
        {
            return moveSpeed;
        }

        public virtual void setVelocity(Vector3 vel)
        {
            velocity = vel;
        }
    }
}
