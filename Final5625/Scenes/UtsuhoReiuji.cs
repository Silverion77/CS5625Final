﻿using System;
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
        Attacking,          // Okuu is swinging her control rod around. Can't be interrupted, but can buffer another attack after it.
        Recovering,         // Okuu is recovering from an attack. Can be interrupted by backstep, and possibly by continuing the attack
                            //      (depending on the attack stage), but not by anything else.
        Uninterruptable,    // Okuu is performing some action that cannot be interrupted until it has finished.
        KOO                 // Okuu is knocked out. So sad.
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

        // Which attack of the 1-2-3 combo we're at; default is 0 = not attacking
        int attackStage = 0;

        // The direction that the player has attempted to move in this step.
        Vector3 inputMovement = Vector3.Zero;
        bool cheerPressed = false;
        bool attackPressed = false;
        bool attackBuffered = false;

        Vector3 velocityDir;
        float targetSpeed = 0;
        float oldSpeed = 0;
        float speed = 0;
        const double speedInterpDuration = 0.1;
        double currentSpeedInterp = 0;
        bool SpeedInterpActive { get { return currentSpeedInterp < speedInterpDuration; } }

        Quaternion oldRotation;
        Quaternion targetRotation;

        const float runSpeed = 8;
        const float walkSpeed = 2;
        const float attackMoveSpeed = 6;
        const float recoverBackSpeed = -1.6f;
        const double attackStartMove = 5.0 / 24;
        const double attackEndMove = 19.0 / 24;

        const double recoverStartMove = 22.0 / 24;
        const double recoverEndMove = 36.0 / 24;

        bool running;

        // The time in seconds it should take to smoothly interpolate from one rotation to another.
        const double rotationInterpDuration = 0.1;
        // Tracks the interpolation time between the previous rotation and the current one
        double currentRotationInterp;
        // Whether or not rotation interpolation is in process
        bool RotationInterpActive { get { return currentRotationInterp < rotationInterpDuration; } }

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
            velocityDir = Vector3.Zero;
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

        public void readyOrIdle()
        {
            // TODO: add conditions for ready
            idle();
        }

        public void idle()
        {
            okuuState = OkuuState.Idle;
            switchAnimationSmooth("idle");
        }

        public void cheer()
        {
            okuuState = OkuuState.Interruptable;
            switchAnimationSmooth("cheer");
        }

        public void attack()
        {
            okuuState = OkuuState.Attacking;
            // If the player is simultaneously holding movement keys down,
            // we should attack in that direction.
            Vector3 attackDir;
            if (!inputMovement.Equals(Vector3.Zero))
                attackDir = inputMovement;
            else
                attackDir = getFacingDirection();

            // if something's wrong, we should cheer about it! lol
            string attack = "cheer";
            if (attackStage == 0)
            {
                attack = "attack1_swing";
                setTargetRotationFromDir(attackDir);
            }
            else if (attackStage == 1)
            {
                attack = "attack2_swing";
                setTargetRotationFromDir(attackDir);
            }
            else if (attackStage == 2)
            {
                // The first and second attacks are in place, but the third moves us forward.
                attack = "attack3_swing";
                setTargetRotationAndVelocityFromDir(attackDir);
            }
            switchAnimationSmooth(attack);
            attackStage++;
        }

        public void bufferAttack()
        {
            attackBuffered = true;
        }

        public void attackRecover()
        {
            okuuState = OkuuState.Recovering;
            string recovery = "cheer";
            if (attackStage == 1)
            {
                recovery = "attack1_recover";
            }
            else if (attackStage == 2)
            {
                recovery = "attack2_recover";
            }
            else if (attackStage == 3)
            {
                recovery = "attack3_recover";
            }
            // These recovery animations were made so that they follow
            // from the attacks in one continuous motion, so we don't need
            // interpolation here.
            switchAnimation(recovery);
        }

        void clearInputFlags()
        {
            inputMovement = Vector3.Zero;
            cheerPressed = false;
            attackPressed = false;
        }

        public override float getMoveSpeed()
        {
            if (okuuState == OkuuState.Attacking && attackStage == 3 && animTime >= attackStartMove && animTime <= attackEndMove)
            {
                return attackMoveSpeed;
            }
            else if (okuuState == OkuuState.Recovering && attackStage == 3 && animTime >= recoverStartMove && animTime <= recoverEndMove)
            {
                return recoverBackSpeed;
            }
            else if (okuuState == OkuuState.Moving)
            {
                return running ? runSpeed : walkSpeed;
            }
            else return 0;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            bool stateChangedFromInput = setNextState();
            if (!stateChangedFromInput)
            {
                advanceCurrentState(e.Time);
            }

            // Clear input flags in preparation for nexts tep
            clearInputFlags();

            rotation = targetRotation;
            updateMatricesAndWorldPos(parentToWorldMatrix);

            if (RotationInterpActive)
                setRotationInterp(e.Time);

            float supposedSpeed = getMoveSpeed();
            if (targetSpeed != supposedSpeed)
            {
                oldSpeed = speed;
                targetSpeed = supposedSpeed;
                currentSpeedInterp = 0;
            }

            if (SpeedInterpActive)
                setSpeedInterp(e.Time);

            velocity = speed * velocityDir;


            if (toWorldMatrix != null && !velocity.Equals(Vector3.Zero))
            {
                float moveSpeed = getMoveSpeed();
                Matrix4 worldToLocal = toWorldMatrix.Inverted();
                Vector4 worldVel = new Vector4((float)e.Time * velocity, 0);
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
            double blendFactor = Math.Min(1, currentRotationInterp / rotationInterpDuration);
            rotation = Quaternion.Slerp(oldRotation, targetRotation, (float)blendFactor);
        }

        void setSpeedInterp(double delta)
        {
            currentSpeedInterp += delta;
            float blendFactor = (float)(Math.Min(1, currentSpeedInterp / speedInterpDuration));
            speed = oldSpeed * (1f - blendFactor) + targetSpeed * blendFactor;
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
                    case OkuuState.Attacking:
                        // The follow-on depends on what the user has done
                        if (attackBuffered)
                        {
                            // If the user has buffered another attack, activate it right away.
                            attack();
                            attackBuffered = false;
                        }
                        else
                        {
                            // Otherwise recover
                            attackRecover();
                        }
                        break;
                    case OkuuState.Recovering:
                        // If we finished recovering, go back to the ready/idle position
                        attackStage = 0;
                        readyOrIdle();
                        break;
                    case OkuuState.Moving:
                    case OkuuState.KOO:
                        // If we're moving, keep moving
                        // And if she's out, she's out
                        break;
                }
            }
        }

        /// <summary>
        /// Current priority of operations (high priority to low)
        /// 
        ///     - Player inputs an attack
        ///     - Player inputs movement
        ///     - Player inputs cheering emote
        ///     - Player does nothing
        /// 
        /// If the player inputs multiple actions in one step, the higher priority action wins.
        /// Returns whether or not this led to a change in state.
        /// </summary>
        bool setNextState()
        {
            if (attackPressed)
            {
                return processAttackInput();
            }
            else if (!inputMovement.Equals(Vector3.Zero))
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
        /// so that we are facing in that direction. Also activates rotation interpolation.
        /// //
        /// </summary>
        void setTargetRotationFromDir(Vector3 vec)
        {
            currentRotationInterp = 0;
            oldRotation = rotation;
            float rotationAngle = (float)Utils.worldRotationOfDir(vec);
            targetRotation = Quaternion.FromAxisAngle(Utils.UP, rotationAngle);
        }
        /// <summary>
        /// Given a direction in which we want to be moving, sets the rotation
        /// so that we are facing in that direction, activates rotation interpolation,
        /// and sets the velocity so that we are actually moving in that direction.
        /// </summary>
        void setTargetRotationAndVelocityFromDir(Vector3 vec)
        {
            setTargetRotationFromDir(vec);
            velocityDir = vec;
        }

        /// <summary>
        /// If the player's most significant input is attack, then do this.
        /// </summary>
        /// <returns></returns>
        bool processAttackInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                case OkuuState.Moving:
                    // These are all OK to attack out of
                    attack();
                    return true;
                case OkuuState.Attacking:
                    // TODO: Can't directly attack here, but can buffer an attack.
                    if (animTime >= 0.50 * currentAnimation.Duration && attackStage < 3)
                    {
                        // If we're late into the attack animation, buffer the attack.
                        bufferAttack();
                        return false;
                    }
                    else return false;
                case OkuuState.Recovering:
                    // If we haven't thrown the last hit of the combo, we can still attack
                    if (attackStage < 3)
                    {
                        attack();
                        return true;
                    }
                    else return false;
                case OkuuState.Uninterruptable:
                case OkuuState.KOO:
                    // Can't attack out of these.
                    return false;
                default:
                    return false;
            }
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
                    setTargetRotationAndVelocityFromDir(inputMovement);
                    return true;
                case OkuuState.Moving:
                    // If she's already moving, we should set her to move in the player's requested
                    // direction, but not set her animation.
                    setTargetRotationAndVelocityFromDir(inputMovement);
                    return false;
                case OkuuState.Attacking:
                case OkuuState.Recovering:
                case OkuuState.Uninterruptable:
                case OkuuState.KOO:
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
                case OkuuState.Attacking:
                case OkuuState.Recovering:
                case OkuuState.Uninterruptable:
                case OkuuState.KOO:
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
                case OkuuState.Attacking:
                case OkuuState.Recovering:
                case OkuuState.KOO:
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

        public void inputAttack()
        {
            attackPressed = true;
        }
    }
}
