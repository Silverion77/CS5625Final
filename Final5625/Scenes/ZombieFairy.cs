using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Meshes;
using Chireiden.Meshes.Animations;

namespace Chireiden.Scenes
{
    enum ZombieState
    {
        Idle,
        Moving,
        Attacking,
        Staggering,
        KO
    }

    class ZombieFairy : SkeletalMeshNode
    {

        ZombieState zombieState;

        Vector3 targetLoc;
        Vector3 velocityDir;
        Vector3 leashLocation;

        Quaternion targetRotation;

        double idleTimer;
        double moveDistanceGoal = 0.5;
        double wanderDistance = 5;

        const float moveSpeed = 3f;
        const float staggerSpeed = -2f;

        public ZombieFairy(MeshContainer m, Vector3 loc) : base(m, loc)
        {
            if (!allAnimsOK(m))
            {
                throw new Exception("Okuu doesn't have all of her animations.");
            }
            idle();
            wallRepelDistance = 1.5f;
            idleTimer = 5;
            targetLoc = loc;
            velocityDir = Utils.FORWARD;
            targetRotation = Quaternion.Identity;
            leashLocation = loc;
        }

        public ZombieFairy(Vector3 loc) : this(MeshLibrary.ZombieFairy, loc)
        { }

        bool allAnimsOK(MeshContainer m)
        {
            return m.hasAnimation("idle")
                && m.hasAnimation("move")
                && m.hasAnimation("attack")
                && m.hasAnimation("shoot")
                && m.hasAnimation("hurt")
                && m.hasAnimation("ko");
        }

        void idle()
        {
            zombieState = ZombieState.Idle;
            switchAnimationSmooth("idle");
            idleTimer = Utils.randomDouble(5, 10);
        }

        void move(Vector3 target)
        {
            zombieState = ZombieState.Moving;
            switchAnimationSmooth("move");
            idleTimer = Utils.randomDouble(5, 10);
            targetLoc = target;
        }

        void attack()
        {
            zombieState = ZombieState.Attacking;
            switchAnimationSmooth("attack");
        }

        void shoot()
        {
            zombieState = ZombieState.Attacking;
            switchAnimationSmooth("shoot");
        }

        void beHurt()
        {
            zombieState = ZombieState.Staggering;
            switchAnimationSmooth("hurt");
        }

        void knockedOut()
        {
            zombieState = ZombieState.KO;
            switchAnimationSmooth("ko");
        }

        /// <summary>
        /// Returns whether or not this fairy can "see" Okuu.
        /// </summary>
        public bool canSeeOkuu()
        {
            return false;
        }

        public override float getMoveSpeed()
        {
            if (zombieState == ZombieState.Moving)
            {
                return moveSpeed;
            }
            if (zombieState == ZombieState.Staggering)
            {
                return staggerSpeed;
            }
            return 0;
        }

        /// <summary>
        /// Sets the direction of our velocity to be pointing toward the target location.
        /// </summary>
        void setVelocityDir()
        {
            if (worldPosition.Equals(targetLoc))
            {
                // then just let it stay what it was before
            }
            else
            {
                velocityDir = targetLoc - worldPosition;
                velocityDir.Normalize();
                float rotationAngle = (float)Utils.worldRotationOfDir(velocityDir);
                targetRotation = Quaternion.FromAxisAngle(Utils.UP, rotationAngle);
            }

        }

        /// <summary>
        /// Basic AI for zombie fairies:
        /// 
        ///     - If we can see Okuu:
        ///         - If Okuu is in sight, but not in projectile range, move toward her.
        ///         - If Okuu is in melee range, attack her.
        ///         - If Okuu is not in melee but is in projectile range, shoot at her.
        ///         
        ///     - If we can't see Okuu, 50% chance of either of the following:
        ///         - Idle for a random period of time.
        ///         - Choose a random location nearby and move there.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="parentToWorldMatrix"></param>
        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            // TODO: if we got hurt this timestep, don't do the following
            // Advance whatever we were doing before
            if (canSeeOkuu())
            {
                advanceStateActive(e.Time);
            }
            else
            {
                advanceStateIdle(e.Time);
            }

            setVelocityDir();
            rotation = targetRotation;

            velocity = getMoveSpeed() * velocityDir;

            Vector3 origPos = worldPosition;
            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * velocity, 0);
                // We want to get the velocity in local space, and then rotate it so it goes in the right direction
                Vector3 localVel = Vector4.Transform(Vector4.Transform(worldVel, worldToLocal), targetRotation).Xyz;
                translation = Vector3.Add(translation, localVel);
            }

            correctPosition(origPos, parentToWorldMatrix);

            updateMatricesAndWorldPos(parentToWorldMatrix);

            // Okuu has kids? News to me
            updateChildren(e, toWorldMatrix);
        }

        void randomIdleAction()
        {
            if (Utils.randomDouble() > 0.7)
            {
                idle();
            }
            else
            {
                double angle = Utils.randomDouble() * 2 * Math.PI;
                Vector3 dir = new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0);
                double scaling = Utils.randomDouble() * wanderDistance;
                Vector3 target = leashLocation + ((float)scaling * dir);
                move(target);
            }
        }

        /// <summary>
        /// Advances the state assuming that we can see Okuu.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        bool advanceStateActive(double delta)
        {
            return advanceStateIdle(delta);
        }

        /// <summary>
        /// Advances the state assuming that we can't see Okuu.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        bool advanceStateIdle(double delta)
        {
            bool animationEnded = advanceAnimation(delta);
            switch (zombieState)
            {
                case ZombieState.Idle:
                    idleTimer -= delta;
                    if (idleTimer <= 0)
                    {
                        randomIdleAction();
                        return true;
                    }
                    return false;
                case ZombieState.Moving:
                    idleTimer -= delta;
                    double dist = (worldPosition - targetLoc).Length;
                    if (dist < moveDistanceGoal || idleTimer <= 0)
                    {
                        idle();
                        return true;
                    }
                    return false;
                case ZombieState.KO:
                    return false;
                default:
                    if (animationEnded)
                    {
                        idle();
                        return true;
                    }
                    return false;
            }
        }
    }
}
