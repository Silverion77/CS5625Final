using System;
using System.Collections.Generic;

using OpenTK;

namespace Chireiden.Meshes.Animations
{
    /// <summary>
    /// A class for interpolating animations. Note that this does not just directly interpolate between two
    /// poses. It takes a source pose, which is taken as a playback time of a source animation clip,
    /// and a target animation. Then, when playing this clip, the target animation will play, but
    /// the resulting poses will actually be somewhere in between the source pose and the target animation's pose,
    /// with the blend factor sliding from 0 to 1 over time.
    /// 
    /// I like to think of this as describing a homotopy from the constant "path" defined by the source pose,
    /// to the "path" defined by the target animation.
    /// </summary>
    class InterpolationClip : Clip
    {
        Pose sourcePose;
        Clip target;

        public double InterpolateDuration { get; set; }
        public override int NumChannels { get { return target.NumChannels; } }
        public override string Name { get { return target.Name; } }
        public override double Duration { get { return target.Duration; } }
        public override bool Wrap { get { return false; } }

        public Clip Target { get { return target; } }

        public InterpolationClip() : this(1) { }

        public InterpolationClip(int numChannels)
        {
            sourcePose = new Pose(numChannels);
            InterpolateDuration = 0;
        }

        /// <summary>
        /// Sets up this interpolation clip to interpolate from the source time at the source animation,
        /// to the target animation.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="sourceTime"></param>
        /// <param name="duration"></param>
        public void setupInterpolation(Clip from, double sourceTime, Clip to, double duration)
        {
            sourcePose.setFromAnimationTime(from, sourceTime);
            target = to;
            InterpolateDuration = Math.Min(duration, to.Duration);
        }

        public bool interpolationEnded(double t)
        {
            return t >= InterpolateDuration;
        }

        /// <summary>
        /// Computes the interpolated pose between the source pose and target animation at the given time.
        /// Returns true iff. the specified channel actually has valid data.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="time"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public override bool getLocRotAtTime(int i, double time, out Vector3 location, out Quaternion rotation)
        {
            Vector3 sourceLocation;
            Quaternion sourceRotation;

            Vector3 targetLocation;
            Quaternion targetRotation;

            bool sourceChannelExists = sourcePose.getLocRotOnChannel(i, out sourceLocation, out sourceRotation);
            bool targetChannelExists = target.getLocRotAtTime(i, time, out targetLocation, out targetRotation);

            if (!sourceChannelExists && !targetChannelExists)
            {
                location = Vector3.Zero;
                rotation = Quaternion.Identity;
                return false;
            }
            else if (!sourceChannelExists)
            {
                location = targetLocation;
                rotation = targetRotation;
                return true;
            }
            else if (!targetChannelExists)
            {
                location = sourceLocation;
                rotation = sourceRotation;
                return true;
            }
            // Both channels have something for this bone, so interpolate
            else
            {
                double blendFactor = Math.Min(1, time / InterpolateDuration);
                location = Vector3.Lerp(sourceLocation, targetLocation, (float)blendFactor);
                rotation = Quaternion.Slerp(sourceRotation, targetRotation, (float)blendFactor);
                return true;
            }
        }

        public override void applyAnimationToSkeleton(ArmatureBone[] bones, double time)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                Vector3 location;
                Quaternion rotation;

                // If neither source nor target has data for this channel, default to rest pose
                if (!getLocRotAtTime(i, time, out location, out rotation))
                {
                    bones[i].setPoseToRest();
                }
                // Otherwise use the interpolated one we just computed
                else
                {
                    bones[i].setPoseTranslation(Matrix4.CreateTranslation(location));
                    bones[i].setPoseRotation(Matrix4.CreateFromQuaternion(rotation));
                }
            }
        }
    }
}
