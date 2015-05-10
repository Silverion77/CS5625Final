using System;
using OpenTK;

using Chireiden.Meshes;
using Chireiden.Meshes.Animations;

namespace Chireiden.Scenes
{
    public class SkeletalMeshNode : MeshNode
    {
        public const double DEFAULT_ANIM_INTERP_DURATION = 0.2;

        protected double animTime = 0;
        protected Clip currentAnimation = null;
        protected InterpolationClip interpolator = new InterpolationClip();
        protected bool interpolationActive;

        protected float[] oldMorphWeights = new float[ShaderProgram.MAX_MORPHS];
        protected float[] targetMorphWeights = new float[ShaderProgram.MAX_MORPHS];
        protected double[] morphInterpTimes = new double[ShaderProgram.MAX_MORPHS];
        protected double[] morphAnimTimes = new double[ShaderProgram.MAX_MORPHS];

        SkeletalMeshGroup skeletalMeshes;

        public SkeletalMeshNode(MeshContainer m)
            : base(m)
        {
            skeletalMeshes = m as SkeletalMeshGroup;
            if (m == null)
                throw new Exception("SkeletalMeshNode can only accept a SkeletalMeshGroup.");
        }

        public SkeletalMeshNode(MeshContainer m, Vector3 loc)
            : base(m, loc)
        {
            skeletalMeshes = m as SkeletalMeshGroup;
            if (m == null)
                throw new Exception("SkeletalMeshNode can only accept a SkeletalMeshGroup.");
        }

        /// <summary>
        /// Advances the object's current animation by the given amount of time.
        /// Returns true iff. the animation has ended (meaning the time has wrapped around).
        /// </summary>
        /// <param name="delta"></param>
        /// <returns></returns>
        public bool advanceAnimation(double delta)
        {
            // If there's no animation then don't do anything
            if (currentAnimation == null || currentAnimation.Duration == 0)
            {
                animTime = 0;
                return false;
            }
            // Advance the animation
            else animTime += delta;

            // If we were interpolating but are now done, we should just switch to the
            // target animation, since there's no need for the interpolator anymore
            if (interpolationActive && animTime >= interpolator.InterpolateDuration)
            {
                currentAnimation = interpolator.Target;
                interpolationActive = false;
            }

            // If we're done with the target animation, we should wrap if applicable,
            // and then report that the animation has ended
            if (animTime >= currentAnimation.Duration)
            {
                if (currentAnimation.Wrap)
                {
                    animTime = animTime % currentAnimation.Duration;
                }
                return true;
            }
            // Otherwise nothing has ended, so don't do anything after advancing
            return false;
        }

        public Matrix4 getBoneTransform(string name)
        {
            return skeletalMeshes.getBoneTransform(name);
        }

        public void clearAnimation()
        {
            currentAnimation = null;
            animTime = 0;
        }

        protected Clip fetchAnimation(string animation)
        {
            return meshes.fetchAnimation(animation);
        }

        /// <summary>
        /// Switches the current animation to the requested one, and also resets
        /// the playback time.
        /// </summary>
        /// <param name="animation"></param>
        public void switchAnimation(string animation)
        {
            animTime = 0;
            currentAnimation = meshes.fetchAnimation(animation);
        }

        /// <summary>
        /// Switches the current animation to the requested one, without
        /// resetting the playback time.
        /// </summary>
        /// <param name="animation"></param>
        public void switchAnimationMidStride(string animation)
        {
            currentAnimation = meshes.fetchAnimation(animation);
        }

        public void switchAnimationSmooth(string animation)
        {
            switchAnimationSmooth(animation, DEFAULT_ANIM_INTERP_DURATION);
        }

        public void setMorphSmooth(string animation, float targetWeight, double interpTime)
        {
            int id = skeletalMeshes.idOfMorph(animation);
            if (interpTime == 0)
            {
                skeletalMeshes.setMorphWeight(id, targetWeight);
            }
            else
            {
                oldMorphWeights[id] = skeletalMeshes.getMorphWeight(id);
                targetMorphWeights[id] = targetWeight;
                morphInterpTimes[id] = interpTime;
                morphAnimTimes[id] = 0;
            }
        }

        /// <summary>
        /// Switches to the requested animation "smoothly", by filling in an interpolation
        /// clip between the current pose and the target animation.
        /// </summary>
        /// <param name="animation"></param>
        public void switchAnimationSmooth(string animation, double interpDuration)
        {
            if (currentAnimation == null)
            {
                // If there's no current animation, there's nothing to interpolate from,
                // so just set it.
                switchAnimation(animation);
            }
            else if (currentAnimation.Name.Equals(animation))
            {
                // If the current animation is the same as the one we're switching to, don't do anything.
            }
            else
            {
                // Otherwise we're being asked to switch smoothly from one animation to another, so do that.
                interpolator.setupInterpolation(currentAnimation, animTime, meshes.fetchAnimation(animation), interpDuration);
                interpolationActive = true;
                currentAnimation = interpolator;
                animTime = 0;
            }
        }

        protected void interpolateMorphs(double delta)
        {
            for (int i = 0; i < skeletalMeshes.NumMorphs; i++)
            {
                float currentWeight = skeletalMeshes.getMorphWeight(i);
                if (morphAnimTimes[i] < morphInterpTimes[i])
                {
                    morphAnimTimes[i] += delta;
                    float blendFactor = (float)Math.Min(1, morphAnimTimes[i] / morphInterpTimes[i]);
                    float interpolated = oldMorphWeights[i] * (1 - blendFactor) + targetMorphWeights[i] * (blendFactor);
                    skeletalMeshes.setMorphWeight(i, interpolated);
                }
            }
        }

        public override void render(Camera camera)
        {
            meshes.renderMeshes(camera, toWorldMatrix, currentAnimation, animTime);
            renderChildren(camera);
        }
    }
}
