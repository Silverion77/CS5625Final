using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes.Animations
{
    /// <summary>
    /// A series of animation keyframes, which supports the ability to ask for the
    /// state of the animation at a given time. The implementation should then look up
    /// the two keyframes that are closest in time, in either direction from the query time.
    /// It should interpolate between those frames and return the result.
    /// </summary>
    public abstract class KeyframeSeries
    {
        public const double LEEWAY = 0.1 / MeshImporter.FRAMES_PER_SECOND;

        public static int findTimeBelow(double t, double[] times)
        {
            // stupid binary search
            int lower = 0;
            int upper = times.Length;
            while (lower < upper)
            {
                int middle = (lower + upper) / 2;
                double middleValue = times[middle];
                if (middle == lower) return middle;
                if (middleValue == t) return middle;
                else if (middleValue < t) lower = middle;
                else upper = middle;
            }
            return lower;
        }

        public static int findTimeBelow(double t, Assimp.VectorKey[] times)
        {
            // stupid binary search II
            int lower = 0;
            int upper = times.Length;
            while (lower < upper)
            {
                int middle = (lower + upper) / 2;
                double middleValue = times[middle].Time;
                if (middle == lower) return middle;
                if (middleValue == t) return middle;
                else if (middleValue < t) lower = middle;
                else upper = middle;
            }
            return lower;
        }

        public static int findTimeBelow(double t, Assimp.QuaternionKey[] times)
        {
            // stupid binary search III
            int lower = 0;
            int upper = times.Length;
            while (lower < upper)
            {
                int middle = (lower + upper) / 2;
                double middleValue = times[middle].Time;
                if (middle == lower) return middle;
                if (middleValue == t) return middle;
                else if (middleValue < t) lower = middle;
                else upper = middle;
            }
            return lower;
        }

        /// <summary>
        /// Given a point in time, interpolates between the two closest keyframes in
        /// this keyframe series, and returns the result. If this series only contains
        /// one keyframe, returns the one keyframe.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public abstract Matrix4 getFrameAtTime(double t);
    }

    /// <summary>
    /// This class packs together location and rotation keyframes into one,
    /// and provides a way to sample from both at once.
    /// </summary>
    public class LocRotKeyframes
    {
        public double Length { get; set; }
        public int BoneID { get; set; }
        public string BoneName { get; set; }

        RotationKeyframes rotations;
        LocationKeyframes translations;

        public LocRotKeyframes(LocationKeyframes loc, RotationKeyframes rots, string boneName, int boneID)
        {
            translations = loc;
            rotations = rots;
            BoneID = boneID;
            BoneName = boneName;
            Length = Math.Max(loc.Length, rots.Length);
        }

        public void getFrameAtTime(double t, out Matrix4 location, out Matrix4 rotation)
        {
            location = translations.getFrameAtTime(t);
            rotation = rotations.getFrameAtTime(t);
        }
    }

    public class RotationKeyframes : KeyframeSeries
    {
        Quaternion[] keyframes;
        // AssImp gave these to us as doubles, so why not
        double[] times;

        public double Length { get; set; }

        /// <summary>
        /// Given an array of all translations in the entire animation strip, and the
        /// start and end frames of the animation clip we're actually interested in,
        /// constructs an instance containing that clip.
        /// 
        /// Note that the range defined by startFrame and endFrame is inclusive.
        /// 
        /// If wrap is true, we append the start frame to the end of the keyframe
        /// series as well, so that the animation ends up at the same position it
        /// started at.
        /// </summary>
        /// <param name="rotations"></param>
        /// <param name="startFrame"></param>
        /// <param name="endFrame"></param>
        /// <param name="wrap"></param>
        public RotationKeyframes(Assimp.QuaternionKey[] rotations, double startTime, double endTime, bool wrap)
        {
            int startFrame = findTimeBelow(startTime + LEEWAY, rotations);
            int endFrame = findTimeBelow(endTime + LEEWAY, rotations);

            int wrapAdd;
            if (wrap && startFrame != endFrame) wrapAdd = 1; else wrapAdd = 0;
            int numFrames = endFrame - startFrame + 1;
            keyframes = new Quaternion[numFrames + wrapAdd];
            times = new double[numFrames + wrapAdd];

            startTime = rotations[startFrame].Time;

            for (int i = 0; i < numFrames; i++)
            {
                times[i] = rotations[startFrame + i].Time - startTime;
                Assimp.Quaternion r = rotations[startFrame + i].Value;
                Quaternion ourRotation = new Quaternion(r.X, r.Y, r.Z, r.W);
                keyframes[i] = ourRotation;
            }

            if (wrap && numFrames > 1)
            {
                double timeStep = times[1] - times[0];
                keyframes[numFrames] = keyframes[0];
                times[numFrames] = times[numFrames - 1] + timeStep;
            }

            Length = times[times.Length - 1];
        }

        public override Matrix4 getFrameAtTime(double t)
        {
            if (keyframes.Length == 1)
            {
                return Matrix4.CreateFromQuaternion(keyframes[0]);
            }
            int frameBefore = findTimeBelow(t, times);
            if (frameBefore >= keyframes.Length - 1)
            {
                return Matrix4.CreateFromQuaternion(keyframes[frameBefore]);
            }

            int frameAfter = frameBefore + 1;

            // Figure out what percentage of the way between timeBefore and timeAfter we are at
            double timeBefore = times[frameBefore];
            double timeAfter = times[frameAfter];
            double blendFactor = (t - timeBefore) / (timeAfter - timeBefore);

            Quaternion rotationBefore = keyframes[frameBefore];
            Quaternion rotationAfter = keyframes[frameAfter];

            Quaternion interpolated = Quaternion.Slerp(rotationBefore, rotationAfter, (float)blendFactor);
            return Matrix4.CreateFromQuaternion(interpolated);
        }
    }

    public class LocationKeyframes : KeyframeSeries
    {
        public double Length { get; set; }
        Vector3[] keyframes;
        double[] times;

        /// <summary>
        /// Given an array of all translations in the entire animation strip, and the
        /// start and end frames of the animation clip we're actually interested in,
        /// constructs an instance containing that clip.
        /// 
        /// Note that the range defined by startFrame and endFrame is inclusive.
        /// </summary>
        /// <param name="locations"></param>
        /// <param name="startFrame"></param>
        /// <param name="endFrame"></param>
        public LocationKeyframes(Assimp.VectorKey[] locations, double startTime, double endTime, bool wrap)
        {
            int startFrame = findTimeBelow(startTime + LEEWAY, locations);
            int endFrame = findTimeBelow(endTime + LEEWAY, locations);

            int wrapAdd;
            if (wrap && startFrame != endFrame) wrapAdd = 1; else wrapAdd = 0;
            int numFrames = endFrame - startFrame + 1;
            keyframes = new Vector3[numFrames + wrapAdd];
            times = new double[numFrames + wrapAdd];

            startTime = locations[startFrame].Time;

            for (int i = 0; i < numFrames; i++)
            {
                times[i] = locations[startFrame + i].Time - startTime;
                Assimp.Vector3D trans = locations[startFrame + i].Value;
                Vector3 ourTrans = new Vector3(trans.X, trans.Y, trans.Z);
                keyframes[i] = ourTrans;
            }

            Length = times[times.Length - 1];
        }

        public override Matrix4 getFrameAtTime(double t)
        {
            if (keyframes.Length == 1)
            {
                Matrix4 mat = Matrix4.CreateTranslation(keyframes[0]);
                return mat;
            }
            int frameBefore = findTimeBelow(t, times);
            if (frameBefore >= keyframes.Length - 1)
            {
                return Matrix4.CreateTranslation(keyframes[frameBefore]);
            }

            int frameAfter = frameBefore + 1;

            // Figure out what percentage of the way between timeBefore and timeAfter we are at
            double timeBefore = times[frameBefore];
            double timeAfter = times[frameAfter];
            double blendFactor = (t - timeBefore) / (timeAfter - timeBefore);

            Vector3 interpolated = Vector3.Lerp(keyframes[frameBefore], keyframes[frameAfter], (float)blendFactor);
            return Matrix4.CreateTranslation(interpolated);
        }
    }
}
