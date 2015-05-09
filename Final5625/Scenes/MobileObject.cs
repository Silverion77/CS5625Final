using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Scenes.Stages;

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

        float moveSpeed = 8;

        protected Stage stage;

        public MobileObject(float s, Vector3 t, Quaternion r, Vector3 v) : base(s, t, r)
        {
            velocity = v;
        }

        public MobileObject(Vector3 t) : base(t) { }

        public MobileObject()
            : this(1, Vector3.Zero, Quaternion.Identity, Vector3.Zero)
        {}

        public void setStage(Stage s)
        {
            stage = s;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            Vector3 origPos = worldPosition;
            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * moveSpeed * velocity, 0);
                Vector3 localVel = Vector4.Transform(worldVel, worldToLocal).Xyz;
                translation = Vector3.Add(translation, localVel);
            }

            // Compute intermediate world position, so we can see if we're out of bounds
            updateMatricesAndWorldPos(parentToWorldMatrix);

            if (stage != null)
            {
                if (stage.worldLocInBounds(origPos) && !stage.worldLocInBounds(worldPosition))
                {
                    // If we're not in bounds, we need to do a projection
                    Vector3 worldSpaceCorrected = stage.projectInBounds(origPos, worldPosition);
                    // Add the correction -- overcorrect slightly
                    Vector3 correction = 1.01f * (worldSpaceCorrected - worldPosition);
                    Console.WriteLine("Went from {0} to {1}, needed correction of {2}", origPos, worldPosition, correction);
                    translation = translation + correction;
                }
            }

            // Now proceed with the actual update
            base.update(e, parentToWorldMatrix);
        }

        public virtual float getMoveSpeed()
        {
            return moveSpeed;
        }

        public virtual void setVelocity(Vector3 vel)
        {
            velocity = vel;
        }
    }
}
