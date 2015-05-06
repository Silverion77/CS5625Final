using System;
using System.Collections.Generic;

using OpenTK;

namespace Chireiden.Meshes.Animations
{
    /// <summary>
    /// An interface representing anything that captures the motion of a skeleton over time.
    /// </summary>
    public abstract class Clip
    {
        public abstract string Name { get; }
        public abstract int NumChannels { get; }
        public abstract double Duration { get; }
        public abstract bool Wrap { get; }

        /// <summary>
        /// Gets the pose data of this clip on channel i at time t.
        /// Returns true iff. the keyframe channel for the requested bone actually exists.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="t"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public abstract bool getLocRotAtTime(int i, double t, out Vector3 location, out Quaternion rotation);

        /// <summary>
        /// Applies the pose of this clip from the given time to the given skeleton.
        /// </summary>
        /// <param name="bones"></param>
        /// <param name="time"></param>
        public abstract void applyAnimationToSkeleton(ArmatureBone[] bones, double time);
    }
}
