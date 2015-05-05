using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Meshes;
using Chireiden.Meshes.Animations;

namespace Chireiden.Scenes
{
    /// <summary>
    /// Each action that Okuu can take should put her in an associated one of these
    /// </summary>
    enum OkuuState
    {
        Idle,               // Okuu is not in motion and is not doing anything. Play idle/ready animation.
        Interruptable,      // Okuu is performing some action, but it can be interrupted by moving or pressing some action key.
        Moving,             // Okuu is in motion, either running or walking. Play run/walk animation.
        Uninterruptable,    // Okuu is performing some action that cannot be interrupted until it has finished.
        KO                  // Okuu is knocked out, sad
    }

    class UtsuhoReiuji : SkeletalMeshNode
    {
        bool allAnimsOK(MeshContainer m)
        {
            return m.hasAnimation("idle")
                && m.hasAnimation("walk")
                && m.hasAnimation("run")
                && m.hasAnimation("cheer");
        }

        // Recall that we have the following fields from PlaceableObject
        // Vector3 translation;
        // Quaternion rotation;
        
        // We have the following from MobileObject
        // Vector3 velocity;

        // We have the following from MeshNode
        // MeshContainer meshes;

        // We have the following from SkeletalMeshNode
        // double animTime;
        // AnimationClip currentAnimation;

        // The state we are currently in
        OkuuState okuuState;

        // The direction that the player has attempted to move in this step.
        Vector3 inputMovement;
        bool cheerPressed;

        Vector3 targetVelocity;

        Quaternion oldRotation;
        Quaternion targetRotation;

        float runSpeed = 6;
        float walkSpeed = 2;

        bool running;

        // The time in seconds it should take to smoothly interpolate from one rotation to another.
        const double rotationInterpDuration = 0.1;
        // Tracks the interpolation time between the previous rotation and the current one
        double currentRotationInterp;

        bool RotationInterpActive { get { return currentRotationInterp < rotationInterpDuration; } }

        // TODO: some actions should let you buffer a second one by pressing the input
        // key before the end, e.g. basic 3-hit attack combo.
        AnimationClip followingAnimation;

        public UtsuhoReiuji(MeshContainer m, Vector3 loc)
            : base(m, loc)
        {
            if (!allAnimsOK(m))
            {
                throw new Exception("Okuu doesn't have all of her animations.");
            }
            running = true;
            oldRotation = Quaternion.Identity;
            targetRotation = Quaternion.Identity;
            targetVelocity = Vector3.Zero;
            inputMovement = Vector3.Zero;
            cheerPressed = false;
            idle();
        }

        public void toggleRunWalk(bool newRunning)
        {
            if (running != newRunning)
            {
                running = newRunning;
                if (running) run();
                else walk();
            }
        }

        public void runOrWalk()
        {
            if (running) run(); else walk();
        }

        public void run()
        {
            okuuState = OkuuState.Moving;
            switchAnimationSmooth("run");
        }

        public void walk()
        {
            okuuState = OkuuState.Moving;
            switchAnimationSmooth("walk");
        }

        public void idle()
        {
            okuuState = OkuuState.Idle;
            targetVelocity = Vector3.Zero;
            switchAnimationSmooth("idle");
        }

        public void cheer()
        {
            okuuState = OkuuState.Interruptable;
            switchAnimationSmooth("cheer");
        }

        /// <summary>
        /// Advances Okuu's current animation by the given amount of time.
        /// Returns true iff. the animation has ended (meaning the time has wrapped around).
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        public new bool advanceAnimation(double delta)
        {
            if (currentAnimation == null || currentAnimation.Duration == 0)
            {
                animTime = 0;
                return false;
            }
            else animTime += delta;
            if (animTime >= currentAnimation.Duration)
            {
                if (currentAnimation.Wrap)
                {
                    animTime = animTime % currentAnimation.Duration;
                }
                return true;
            }
            return false;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            bool stateChangedFromInput = setNextState();
            if (!stateChangedFromInput)
            {
                advanceCurrentState(e.Time);
            }

            // Clear input flags in preparation for nexts tep
            inputMovement = Vector3.Zero;
            cheerPressed = false;

            rotation = targetRotation;
            updateMatricesAndWorldPos(parentToWorldMatrix);

            if (RotationInterpActive)
                setRotationInterp(e.Time);
            // TODO: smoothly interpolate to this too
            velocity = targetVelocity;


            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                float moveSpeed = running ? runSpeed : walkSpeed;
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * moveSpeed * velocity, 0);
                // We want to get the velocity in local space, and then rotate it so it goes in the right direction
                Vector3 localVel = Vector4.Transform(Vector4.Transform(worldVel, worldToLocal), targetRotation).Xyz;
                translation = Vector3.Add(translation, localVel);
            }

            updateMatricesAndWorldPos(parentToWorldMatrix);
            
            // Okuu has kids? News to me
            updateChildren(e, toWorldMatrix);
        }

        /// <summary>
        /// Sets the rotation to be interpolated between the previous rotation and the target rotation.
        /// </summary>
        /// <param name="delta"></param>
        void setRotationInterp(double delta)
        {
            currentRotationInterp += delta;
            double blendFactor = currentRotationInterp / rotationInterpDuration;
            rotation = Quaternion.Slerp(oldRotation, targetRotation, (float)Math.Min(1, blendFactor));
        }

        /// <summary>
        /// Take any actions that are entailed by the current state.
        /// </summary>
        void advanceCurrentState(double delta)
        {
            // Try advancing the animation.
            bool animationEnded = advanceAnimation(delta);
            // If it's over, then there are a few things we may need to do.
            if (animationEnded)
            {
                switch (okuuState)
                {
                    case OkuuState.Idle:
                        // Just keep idling
                        break;
                    case OkuuState.Interruptable:
                    case OkuuState.Uninterruptable:
                        // If we finished some action, regardless of interruptability, then go back to idle
                        idle();
                        break;
                    case OkuuState.Moving:
                    case OkuuState.KO:
                        // If we're moving, keep moving
                        // And if she's out, she's out
                        break;
                }
            }
        }

        /// <summary>
        /// Current priority of operations (high priority to low)
        /// 
        ///     - Player inputs movement
        ///     - Player inputs cheering emote
        ///     - Player does nothing
        ///     
        /// Returns whether or not this led to a change in state.
        /// </summary>
        bool setNextState()
        {
            if (!inputMovement.Equals(Vector3.Zero))
            {
                return processMovementInput();
            }
            else if (cheerPressed)
            {
                return processCheerInput();
            }
            else
            {
                // The player input nothing, so do nothing.
                return processIdleInput();
            }
        }

        /// <summary>
        /// Given a direction in which we want to be moving, sets the rotation
        /// so that we are facing in that direction.
        /// </summary>
        void setTargetRotationFromDir(Vector3 vec)
        {
            currentRotationInterp = 0;
            oldRotation = rotation;
            targetVelocity = vec;
            float rotationAngle = (float)Utils.worldRotationOfDir(vec);
            targetRotation = Quaternion.FromAxisAngle(Utils.UP, rotationAngle);
        }

        /// <summary>
        /// If the player's most significant input is movement, then do this.
        /// </summary>
        bool processMovementInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                    // If she's idle, or in some state where she can be interrupted by movement,
                    // then we should start the run/walk animation
                    runOrWalk();
                    // and also start her moving.
                    setTargetRotationFromDir(inputMovement);
                    return true;
                case OkuuState.Moving:
                    // If she's already moving, we should set her to move in the player's requested
                    // direction, but not set her animation.
                    setTargetRotationFromDir(inputMovement);
                    return false;
                case OkuuState.Uninterruptable:
                case OkuuState.KO:
                    // If she's in some uninterruptable state, nothing changes.
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// If the player's most significant movement is the cheer emote, then do this.
        /// </summary>
        /// <returns></returns>
        bool processCheerInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                    // If she's not doing anything else, or doing something else we can interrupt,
                    // then we can cheer.
                    cheer();
                    return true;
                case OkuuState.Moving:
                case OkuuState.Uninterruptable:
                case OkuuState.KO:
                    // Otherwise she's indisposed and cannot cheer.
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// If the player doesn't actually input anything, then do this.
        /// </summary>
        /// <returns></returns>
        bool processIdleInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                case OkuuState.Uninterruptable:
                case OkuuState.KO:
                    // In any of these cases we should keep doing what we were doing
                    return false;
                case OkuuState.Moving:
                    // Here we should stop moving
                    idle();
                    return true;
                default:
                    return false;
            }
        }

        public void inputCheer()
        {
            cheerPressed = true;
        }

        public void inputMove(Vector3 vel)
        {
            inputMovement = vel;
        }
    }
}
