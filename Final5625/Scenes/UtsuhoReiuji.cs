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
        Attacking,          // Okuu is swinging her control rod around. Can't be interrupted, but can buffer another attack after it.
        Recovering,         // Okuu is recovering from an attack. Can be interrupted by backstep, and possibly by continuing the attack
                            //      (depending on the attack stage), but not by anything else.
        Backstepping,       // Okuu is backstepping. Can't interrupt. Has invulnerability frames.
        Transitioning,      // Okuu is moving from one state to another. Can't be interrupted, except by damage.
        Aiming,             // Okuu is aiming her cannon. Can fire, backstep, or lower the cannon, but can't do anything else.
        Uninterruptable,    // Okuu is performing some action that cannot be interrupted until it has finished.
        KO                  // Okuu is knocked out. So sad.
    }

    enum BufferAction
    {
        Nothing,
        Attack,
        Backstep
    }

    public class UtsuhoReiuji : SkeletalMeshNode
    {
        /// <summary>
        /// Makes sure that Okuu has all of the animations we expect her to have.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        bool allAnimsOK(MeshContainer m)
        {
            // Great function
            return m.hasAnimation("idle")
                && m.hasAnimation("walk")
                && m.hasAnimation("run")
                && m.hasAnimation("cheer")
                && m.hasAnimation("ready")
                && m.hasAnimation("attack1_swing")
                && m.hasAnimation("attack1_recover")
                && m.hasAnimation("attack2_swing")
                && m.hasAnimation("attack2_recover")
                && m.hasAnimation("attack3_swing")
                && m.hasAnimation("attack3_recover")
                && m.hasAnimation("backstep")
                && m.hasAnimation("raise_cannon")
                && m.hasAnimation("aiming")
                && m.hasAnimation("fire")
                && m.hasAnimation("lower_cannon")
                && m.hasAnimation("idle_hurt")
                && m.hasAnimation("ready_hurt")
                && m.hasAnimation("run_hurt")
                && m.hasAnimation("run_hurt_recover")
                && m.hasAnimation("ko");
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
        bool backstepPressed = false;
        bool cannonPressed = false;
        bool damageTaken = false;
        bool instantKO = false;
        bool miracle = false;

        BufferAction bufferedAction;

        Vector3 velocityDir = Utils.FORWARD;
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

        // Speed and movement duration for the 3rd attack, which moves us forward
        const float attackMoveSpeed = 6;
        const double attackStartMove = 5.0 / 24;
        const double attackEndMove = 19.0 / 24;

        // The recovery from the 3rd attack moves us back a little
        const float recoverBackSpeed = -1.6f;
        const double recoverStartMove = 22.0 / 24;
        const double recoverEndMove = 36.0 / 24;

        // Backstepping obviously moves us back
        const float backstepSpeed = -8;
        const double backstepStartMove = 2.0 / 24;
        const double backstepEndMove = 17.0 / 24;

        bool running = true;
        bool readyForAction = false;

        // The time in seconds it should take to smoothly interpolate from one rotation to another.
        const double rotationInterpDuration = 0.1;
        // Tracks the interpolation time between the previous rotation and the current one
        double currentRotationInterp;
        // Whether or not rotation interpolation is in process
        bool RotationInterpActive { get { return currentRotationInterp < rotationInterpDuration; } }

        // The state that we will go to after the current Uninterruptable state ends
        OkuuState transitionState = OkuuState.Idle;

        double currentBlinkTime = 0;
        double nextBlinkTime = 1;
        const double halfBlinkDuration = 0.17;
        bool blinkInProgress = false;
        bool blinkEnabled = true;

        public UtsuhoReiuji(MeshContainer m, Vector3 loc)
            : base(m, loc)
        {
            if (!allAnimsOK(m))
            {
                throw new Exception("Okuu doesn't have all of her animations.");
            }
            Console.WriteLine("TODO: make model not clip");
            oldRotation = Quaternion.Identity;
            targetRotation = Quaternion.Identity;
            idle();
            wallRepelDistance = 1.5f;
        }

        public UtsuhoReiuji(Vector3 loc) : this(MeshLibrary.Okuu, loc) { }

        public void toggleRunWalk(bool newRunning)
        {
            if (running != newRunning)
            {
                running = newRunning;
                if (okuuState == OkuuState.Moving)
                {
                    if (running) run();
                    else walk();
                }
            }
        }

        public void toggleReadyIdle()
        {
            toggleReadyIdle(!readyForAction);
        }

        public void toggleReadyIdle(bool newReady)
        {
            if (readyForAction != newReady)
            {
                readyForAction = newReady;
                if (okuuState == OkuuState.Idle)
                {
                    if (readyForAction) ready();
                    else idle();
                }
            }
        }

        void runOrWalk()
        {
            okuuState = OkuuState.Moving;
            if (running) run(); else walk();
        }

        void run()
        {
            switchAnimationSmooth("run");
        }

        void walk()
        {
            switchAnimationSmooth("walk");
        }

        void readyOrIdle()
        {
            okuuState = OkuuState.Idle;
            if (readyForAction) ready(); else idle();
        }

        void ready()
        {
            switchAnimationSmooth("ready");
        }

        void idle()
        {
            switchAnimationSmooth("idle");
        }

        void cheer()
        {
            okuuState = OkuuState.Interruptable;
            switchAnimationSmooth("cheer");
        }

        void attack()
        {
            Console.WriteLine("TODO: attack hitboxes");
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

        void attackRecover()
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

        void backstep()
        {
            okuuState = OkuuState.Backstepping;
            attackStage = 0;
            switchAnimationSmooth("backstep");
        }

        void startAiming()
        {
            // After playing the raise_cannon animation, which should be uninterruptable, 
            // we should go to the Aiming state, and start playing the aiming animation.
            okuuState = OkuuState.Transitioning;
            switchAnimationSmooth("raise_cannon");
            transitionState = OkuuState.Aiming;
        }

        void aim()
        {
            okuuState = OkuuState.Aiming;
            // The aiming animation is designed to follow smoothly from the raise cannon animation
            switchAnimationSmooth("aiming");
        }

        void fire()
        {
            okuuState = OkuuState.Uninterruptable;
            switchAnimationSmooth("fire");
            transitionState = OkuuState.Aiming;
            Console.WriteLine("TODO: Fire missile here");
        }

        void stopAiming()
        {
            // After we play the lower_cannon animation, go back to idle.
            okuuState = OkuuState.Transitioning;
            switchAnimationSmooth("lower_cannon");
            transitionState = OkuuState.Idle;
        }

        void beHurt()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                    if (readyForAction)
                        switchAnimationSmooth("ready_hurt");
                    else
                        switchAnimationSmooth("idle_hurt");
                    transitionState = OkuuState.Idle;
                    break;
                case OkuuState.Moving:
                    switchAnimationSmooth("run_hurt");
                    transitionState = OkuuState.Moving;
                    break;
                case OkuuState.Aiming:
                    switchAnimationSmooth("ready_hurt");
                    transitionState = OkuuState.Aiming;
                    break;
                default:
                    switchAnimationSmooth("ready_hurt");
                    transitionState = OkuuState.Idle;
                    break;
            }
            okuuState = OkuuState.Transitioning;
        }

        void knockedOut()
        {
            setMorphSmooth("eyes_closed", 0, 0);
            setMorphSmooth("><_eyes", 1, 0);
            setMorphSmooth("sad_brow", 1, 1);
            blinkEnabled = false;
            currentBlinkTime = 0;
            okuuState = OkuuState.KO;
            switchAnimationSmooth("ko");
        }

        void clearInputFlags()
        {
            inputMovement = Vector3.Zero;
            cheerPressed = false;
            attackPressed = false;
            backstepPressed = false;
            cannonPressed = false;
            damageTaken = false;
            instantKO = false;
            miracle = false;
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
            else if (okuuState == OkuuState.Backstepping && animTime >= backstepStartMove && animTime <= backstepEndMove)
            {
                return backstepSpeed;
            }
            else if (okuuState == OkuuState.Moving)
            {
                return running ? runSpeed : walkSpeed;
            }
            else if (okuuState == OkuuState.Transitioning && transitionState == OkuuState.Moving)
            {
                float normalSpeed = running ? runSpeed : walkSpeed;
                return 0.5f * normalSpeed;
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

            // Make Okuu blink every so often
            if (blinkEnabled)
            {
                currentBlinkTime += e.Time;
            }
            if (currentBlinkTime > nextBlinkTime)
            {
                if (currentBlinkTime > nextBlinkTime + 1.4 * halfBlinkDuration)
                {
                    currentBlinkTime = 0;
                    nextBlinkTime = Utils.randomDouble(3, 7);
                    Console.WriteLine("Next blink in {0} sec", nextBlinkTime);
                    setMorphSmooth("eyes_closed", 0, halfBlinkDuration);
                    blinkInProgress = false;
                }
                else if (!blinkInProgress)
                {
                    setMorphSmooth("eyes_closed", 1, halfBlinkDuration);
                    blinkInProgress = true;
                }
            }

            interpolateMorphs(e.Time);

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

        void transition()
        {
            switch (transitionState)
            {
                case OkuuState.Aiming:
                    aim();
                    break;
                case OkuuState.Idle:
                    readyOrIdle();
                    break;
                case OkuuState.Moving:
                    runOrWalk();
                    break;
                default:
                    Console.WriteLine("Transitioning to other states is not implemented.");
                    readyOrIdle();
                    break;
            }
            transitionState = OkuuState.Idle;
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
                    case OkuuState.Backstepping:
                        // If we finished some action, regardless of interruptability, then go back to idle
                        readyOrIdle();
                        break;
                    case OkuuState.Attacking:
                        // The follow-on depends on what the user has done
                        switch (bufferedAction)
                        {
                            case BufferAction.Attack:
                                // If the user has buffered another attack, activate it right away.
                                attack();
                                break;
                            case BufferAction.Backstep:
                                // If the user has buffered a backstep, do that.
                                backstep();
                                break;
                            default:
                                // Otherwise nothing in particular was buffered, so recover.
                                attackRecover();
                                break;
                        }
                        // Regardless, clear the buffer.
                        bufferedAction = BufferAction.Nothing;
                        break;
                    case OkuuState.Recovering:
                        // If we finished recovering, go back to the ready/idle position
                        attackStage = 0;
                        readyOrIdle();
                        break;
                    case OkuuState.Uninterruptable:
                    case OkuuState.Transitioning:
                        // Complete the transition
                        transition();
                        break;
                    case OkuuState.Aiming:
                    case OkuuState.Moving:
                    case OkuuState.KO:
                        // If we're aiming, keep aiming
                        // If we're moving, keep moving
                        // And if she's out, she's out
                        break;
                }
            }
        }

        /// <summary>
        /// Current priority of operations (high priority to low)
        /// 
        ///     - Revival triggered (debugging only)
        ///     - Instant KO triggered (debugging only)
        ///     - Damage was taken
        ///     - Player inputs a backstep
        ///     - Player inputs an attack
        ///     - Player inputs raise/lower cannon
        ///     - Player inputs movement
        ///     - Player inputs cheering emote
        ///     - Player does nothing
        /// 
        /// If the player inputs multiple actions in one step, the higher priority action wins.
        /// Returns whether or not this led to a change in state.
        /// </summary>
        bool setNextState()
        {
            if (miracle)
            {
                if (okuuState == OkuuState.KO)
                {
                    blinkEnabled = true;
                    setMorphSmooth("sad_brow", 0, 1);
                    setMorphSmooth("><_eyes", 0, 0);
                    readyOrIdle();
                    return true;
                }
                else return false;
            }
            else if (instantKO)
            {
                if (okuuState == OkuuState.KO)
                {
                    return false;
                }
                else
                {
                    knockedOut();
                    return true;
                }
            }
            else if (damageTaken)
            {
                return processDamageTaken();
            }
            else if (backstepPressed)
            {
                return processBackstepInput();
            }
            else if (attackPressed)
            {
                return processAttackInput();
            }
            else if (cannonPressed)
            {
                return processCannonInput();
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

        bool processDamageTaken()
        {
            switch (okuuState)
            {
                case OkuuState.Uninterruptable:
                case OkuuState.Attacking:
                case OkuuState.KO:
                    // If we're backstepping, we're invulnerable
                    // We'll let "Uninterruptable" mean immune to hit stuns as well
                    // We'll let an attack in progress have priority
                    // And obviously if we're KO'd it doesn't matter
                    return false;
                case OkuuState.Backstepping:
                    // Backstepping makes us invulnerable, but only for the time we're in motion
                    if (animTime <= backstepEndMove)
                    {
                        return false;
                    }
                    else
                    {
                        beHurt();
                        return true;
                    }
                default:
                    // Everywhere else, getting hit matters
                    beHurt();
                    return true;
            }
        }

        bool processBackstepInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                case OkuuState.Moving:
                case OkuuState.Recovering:
                case OkuuState.Aiming:
                    // All of these are OK to step out of
                    backstep();
                    return true;
                case OkuuState.Attacking:
                    // Can't step out of attack, but can buffer it.
                    if (animTime >= 0.50 * currentAnimation.Duration)
                    {
                        // Buffer it if we're late enough in the animation.
                        bufferedAction = BufferAction.Backstep;
                    }
                    return false;
                default:
                    // Can't step out of anything else
                    return false;
            }
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
                    // Can't directly attack here, but can buffer an attack.
                    if (animTime >= 0.50 * currentAnimation.Duration && attackStage < 3)
                    {
                        // If we're late into the attack animation, buffer the attack.
                        bufferedAction = BufferAction.Attack;
                    }
                    return false;
                case OkuuState.Recovering:
                    // If we haven't thrown the last hit of the combo, we can still attack
                    if (attackStage < 3)
                    {
                        attack();
                        return true;
                    }
                    else return false;
                case OkuuState.Aiming:
                    // In this case, we want to shoot the cannon.
                    fire();
                    return true;
                default:
                    // Can't attack out of anything else.
                    return false;
            }
        }

        bool processCannonInput()
        {
            switch (okuuState)
            {
                case OkuuState.Idle:
                case OkuuState.Interruptable:
                case OkuuState.Moving:
                    // These are all OK to start aiming out of
                    startAiming();
                    return true;
                case OkuuState.Aiming:
                    // In this case we want to stop aiming
                    stopAiming();
                    return true;
                default:
                    // In any other case, we can't toggle aiming.
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
                default:
                    // There's no other state where we can start moving.
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
                default:
                    // Otherwise she's indisposed and cannot cheer.
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
                case OkuuState.Backstepping:
                case OkuuState.Transitioning:
                case OkuuState.Aiming:
                case OkuuState.KO:
                    // In any of these cases we should keep doing what we were doing
                    return false;
                case OkuuState.Moving:
                    // Here we should stop moving
                    readyOrIdle();
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

        public void inputBackstep()
        {
            backstepPressed = true;
        }

        public void inputCannon()
        {
            cannonPressed = true;
        }

        public void getHurt(int attackPower)
        {
            Console.WriteLine("TODO: compute how much actual damage");
            damageTaken = true;
        }

        public void instantKnockout()
        {
            instantKO = true;
        }

        public void medicalMiracle()
        {
            miracle = true;
        }
    }
}
