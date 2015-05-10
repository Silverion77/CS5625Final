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
        protected float wallRepelDistance = 1;

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
            updatePosition(e, parentToWorldMatrix);

            // Now proceed with the actual update
            base.update(e, parentToWorldMatrix);
        }

        protected void updatePosition(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            Vector3 origPos = worldPosition;
            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * velocity, 0);
                Vector3 localVel = Vector4.Transform(worldVel, worldToLocal).Xyz;
                translation = Vector3.Add(translation, localVel);
            }

            // Update intermediate world position
            updateMatricesAndWorldPos(parentToWorldMatrix);

            correctPosition(origPos, parentToWorldMatrix);

            // Now update corrected position
            updateMatricesAndWorldPos(parentToWorldMatrix);
        }

        protected virtual void correctPosition(Vector3 origPos, Matrix4 parentToWorldMatrix)
        {
            if (stage != null)
            {
                Vector3 worldSpaceCorrected = worldPosition;
                Vector3 correction;
                // First do a pass to make sure we don't push through a wall
                if (stage.worldLocInBounds(origPos) && !stage.worldLocInBounds(worldPosition))
                {
                    // If we're not in bounds, we need to do a projection
                    worldSpaceCorrected = stage.computeCollisionTime(origPos, worldPosition);
                    // Add the correction -- overcorrect slightly
                    correction = 1.01f * (worldSpaceCorrected - worldPosition);
                    Console.WriteLine("Went from {0} to {1}, needed correction of {2}", origPos, worldPosition, correction);
                    translation = translation + correction;
                }
                // Then do a pass to keep us a safe distance from each wall
                Vector3 repelledPos = stage.repelFromWall(worldSpaceCorrected, wallRepelDistance);
                correction = repelledPos - worldSpaceCorrected;
                translation = translation + correction;
            }
        }

        public virtual float getMoveSpeed()
        {
            return moveSpeed;
        }

        public virtual void setVelocity(Vector3 vel)
        {
            velocity = moveSpeed * vel;
        }

        public virtual float camRightOffset()
        {
            return 0;
        }
    }
}
