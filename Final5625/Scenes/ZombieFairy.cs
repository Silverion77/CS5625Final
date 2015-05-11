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
        Shooting,
        Staggering,
        KO
    }

    public class MorphState
    {
        public float[] oldMorphWeights;
        public float[] targetMorphWeights;
        public float[] morphWeights;
        public double[] morphInterpTimes;
        public double[] morphAnimTimes;

        public MorphState(int numMorphs)
        {
            oldMorphWeights = new float[numMorphs];
            targetMorphWeights = new float[numMorphs];
            morphInterpTimes = new double[numMorphs];
            morphAnimTimes = new double[numMorphs];
            morphWeights = new float[numMorphs];
        }
    }

    public class ZombieFairy : SkeletalMeshNode
    {

        public const float hitCylinderRadius = 1f;
        public const float hitCylinderHeight = 4;

        ZombieState zombieState;

        Vector3 targetLoc;
        Vector3 velocityDir;
        Vector3 leashLocation;

        // Every step, the zombie fairy should be given Okuu's location at the previous timestep
        Vector3 okuuLocation;

        // Becomes true once we see her; doesn't become false until she runs far enough away
        bool awareOfOkuu = false;

        bool wasHurt = false;

        Quaternion targetRotation;

        double idleTimer;
        double moveDistanceGoal = 0.5;
        double wanderDistance = 5;

        const float visionDistance = 20;
        const float leashDistance = 50;

        const float moveSpeed = 3f;
        const float staggerSpeed = -1.5f;
        const double staggerExtraDelay = 1;

        double staggerTime = 0;

        const float projectileDistance = 10;
        const float meleeMoveDistance = 5;
        const float meleeDistance = 2;

        const double shootTime = 85.0 / 24;

        double koTime = 0;

        int hitStage;
        int hitPoints = 10;

        Matrix4 palmRMatrix;

        MorphState morphState = new MorphState(ShaderProgram.MAX_MORPHS);

        static Vector3 handLoc = new Vector3(-0.64f, -0.1f, 0.95f);
        FireScatterer fireHand = new FireScatterer(handLoc, 100f, 0.025f);

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
            if (zombieState == ZombieState.Attacking) animTime = 0;
            zombieState = ZombieState.Attacking;
            switchAnimationSmooth("attack");
        }

        void shoot(Vector3 target)
        {
            if (zombieState == ZombieState.Shooting)
                animTime = 0;
            zombieState = ZombieState.Shooting;
            switchAnimationSmooth("shoot");
            targetLoc = target;
        }

        ZombieProjectile outgoingProjectile;

        void launchProjectile()
        {
            Vector3 bulletPos = Vector4.Transform(new Vector4(handLoc, 1), palmRMatrix).Xyz;
            outgoingProjectile = new ZombieProjectile(.5f, bulletPos, 20 * getFacingDirection());
        }

        public bool getProjectile(out ZombieProjectile projectile)
        {
            if (outgoingProjectile != null)
            {
                projectile = outgoingProjectile;
                outgoingProjectile = null;
                return true;
            }
            projectile = null;
            return false;
        }

        void beHurt()
        {
            if (zombieState == ZombieState.Staggering)
                animTime = 0;
            zombieState = ZombieState.Staggering;
            switchAnimationSmooth("hurt");
        }

        void knockedOut()
        {
            zombieState = ZombieState.KO;
            switchAnimationSmooth("ko");
            setMorphSmooth(morphState, "><_eyes", 1, 1);
            setMorphSmooth(morphState, "tears", 1, 1);
            setMorphSmooth(morphState, "oo", 1, 1);
        }

        float distFromOkuu = 0;

        public void updateOkuuLocation(Vector3 loc)
        {
            okuuLocation = loc;
            distFromOkuu = (worldPosition - okuuLocation).Length;
        }

        public double timeKOed()
        {
            return koTime;
        }

        bool outOfLeashRange()
        {
            Vector3 diff = worldPosition - leashLocation;
            return diff.Length > leashDistance;
        }

        /// <summary>
        /// Returns whether or not this fairy can "see" Okuu.
        /// </summary>
        bool canSeeOkuu()
        {
            if (awareOfOkuu) return true;
            Vector3 diff = okuuLocation - worldPosition;
            float dist = diff.Length;
            Vector3 facing = getFacingDirection();
            // We can see her if she's in vision distance, and also in front of us
            return (dist < visionDistance) && (Vector3.Dot(diff, facing) > 0);
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
            if (distFromOkuu > GameMain.RenderDistance) return;

            if (zombieState == ZombieState.KO)
            {
                koTime += e.Time;
            }
            if (outOfLeashRange())
            {
                move(leashLocation);
                awareOfOkuu = false;
                idleTimer = 20;
            }
            else if (wasHurt)
            {
                wasHurt = false;
                if (hitPoints <= 0)
                    knockedOut();
                else if (zombieState != ZombieState.KO)
                    beHurt();
            }
            // Advance whatever we were doing before
            else if (awareOfOkuu || canSeeOkuu())
            {
                awareOfOkuu = true;
                advanceStateActive(e.Time);
            }
            else
            {
                advanceStateIdle(e.Time);
            }

            setVelocityDir();
            rotation = targetRotation;

            velocity = getMoveSpeed() * velocityDir;

            interpolateMorphs(morphState, e.Time);

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

            if (zombieState == ZombieState.Attacking || zombieState == ZombieState.Shooting)
            {
                fireHand.update(e, palmRMatrix);
            }

            updateChildren(e, toWorldMatrix);
        }

        public override void render(Camera camera)
        {
            if (distFromOkuu > GameMain.RenderDistance) return;
            setMorphWeights(morphState);
            base.render(camera);

            palmRMatrix = getBoneTransform("palm.r");
            palmRMatrix = palmRMatrix * toWorldMatrix;
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

        void goAfterOkuu()
        {
            // If she's not close enough to shoot, move closer
            if (distFromOkuu > projectileDistance)
            {
                move(okuuLocation);
            }
            // If she's in projectile range, shoot
            else if (distFromOkuu > meleeMoveDistance)
            {
                shoot(okuuLocation);
            }
            // If she's close enough to melee, close in for that
            else if (distFromOkuu > meleeDistance)
            {
                move(okuuLocation);
            }
            // If she's in melee range, swing
            else
            {
                attack();
            }
        }

        /// <summary>
        /// Advances the state assuming that we can see Okuu.
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        bool advanceStateActive(double delta)
        {
            bool animationEnded = advanceAnimation(delta);
            switch (zombieState)
            {
                case ZombieState.Idle:
                    if (staggerTime > 0)
                    {
                        staggerTime -= delta;
                        return false;
                    }
                    else
                    {
                        goAfterOkuu();
                        return true;
                    }
                case ZombieState.Moving:
                    goAfterOkuu();
                    // If we were already moving then this isn't a state change
                    if (zombieState == ZombieState.Moving) return false;
                    return true;
                case ZombieState.KO:
                    return false;
                case ZombieState.Staggering:
                    if (animationEnded)
                    {
                        staggerTime = staggerExtraDelay;
                        hitStage = 0;
                        idle();
                        return true;
                    }
                    return false;
                case ZombieState.Shooting:
                    double prevAnimTime = animTime - delta;
                    targetLoc = okuuLocation;
                    if (prevAnimTime < shootTime && animTime >= shootTime)
                    {
                        launchProjectile();
                    }
                    if (animationEnded)
                    {
                        goAfterOkuu();
                        return true;
                    }
                    else return false;
                default:
                    // If we're in the middle of something else, let it finish
                    // Otherwise, continue going after Okuu.
                    if (animationEnded)
                    {
                        goAfterOkuu();
                        return true;
                    }
                    else targetLoc = okuuLocation;
                    return false;
            }
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

        public void registerHit(int stage)
        {
            Console.WriteLine("Fairy was hurt");
            awareOfOkuu = true;
            wasHurt = true;
            hitPoints -= stage;
            hitStage = stage;
        }

        public int lastHitStage()
        {
            return hitStage;
        }

        const double startAttackTime = 37.0 / 24;
        const double endAttackTime = 53.0 / 24;
        const float AttackHitSphereRadius = 0.5f;

        public void checkAttackHit(UtsuhoReiuji okuu)
        {
            if (zombieState != ZombieState.Attacking || animTime < startAttackTime || animTime > endAttackTime) return;

            Vector3 collisionPos = okuu.worldPosition;
            if (Math.Abs(collisionPos.X - worldPosition.X) > 3
                || Math.Abs(collisionPos.Y - worldPosition.Y) > 3) return;
            float distRequired = AttackHitSphereRadius + UtsuhoReiuji.hitCylinderHeight;
            bool hit = false;
            Vector3 hitSpherePos = fireHand.worldPosition;
            float dist = (collisionPos.Xy - hitSpherePos.Xy).Length;
            if (dist <= distRequired && hitSpherePos.Z > 0 && hitSpherePos.Z < UtsuhoReiuji.hitCylinderHeight)
            {
                hit = true;
            }
            if (hit)
                okuu.getHurt(3);
        }
    }
}
