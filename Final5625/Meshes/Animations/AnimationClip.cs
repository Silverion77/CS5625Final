using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes.Animations
{
    public class AnimationClip : Clip
    {
        /// <summary>
        /// boneChannels[i] contains all of the animation keyframes for this animation clip
        /// that affect the bone with ID i.
        /// </summary>
        LocRotKeyframes[] boneChannels;

        string name;
        public override string Name { get { return name; } }
        double duration;
        public override double Duration { get { return duration; } }
        public override int NumChannels { get { return boneChannels.Length; } }
        bool wrap;
        public override bool Wrap { get { return wrap; } }

        public AnimationClip(List<NodeFramesArray> allFrames, AnimationCue cue, int numBones, Dictionary<string, int> boneDict)
        {
            boneChannels = new LocRotKeyframes[numBones];

            foreach (NodeFramesArray frames in allFrames)
            {
                int boneID;
                if (!boneDict.TryGetValue(frames.NodeName, out boneID))
                {
                    // Console.WriteLine("Animation referenced nonexistent or non-deforming bone {0}, ignoring", frames.NodeName);
                    continue;
                }
                RotationKeyframes rotFrames = new RotationKeyframes(frames.RotationKeys, cue.StartTime, cue.EndTime, cue.Wrap);
                LocationKeyframes locFrames = new LocationKeyframes(frames.LocationKeys, cue.StartTime, cue.EndTime, cue.Wrap);

                LocRotKeyframes locRot = new LocRotKeyframes(locFrames, rotFrames, frames.NodeName, boneID);
                boneChannels[boneID] = locRot;

            }
            name = cue.Name;
            wrap = cue.Wrap;
            duration = 0;

            foreach (LocRotKeyframes frames in boneChannels) {
                if (frames == null) continue;
                duration = Math.Max(Duration, frames.Length);
            }
        }

        /// <summary>
        /// Gets the location-rotation keyframe for bone i at time t, if it exists.
        /// Returns true iff. the keyframe channel for the requested bone actually exists.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="t"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public override bool getLocRotAtTime(int i, double t, out Vector3 location, out Quaternion rotation)
        {
            if (boneChannels[i] != null)
            {
                boneChannels[i].getLocRotAtTime(t, out location, out rotation);
                return true;
            }
            else
            {
                location = Vector3.Zero;
                rotation = Quaternion.Identity;
                return false;
            }
        }

        public override void applyAnimationToSkeleton(ArmatureBone[] bones, double time)
        {
            Matrix4 location;
            Matrix4 rotation;
            for (int i = 0; i < bones.Length; i++)
            {
                if (boneChannels[i] == null)
                {
                    bones[i].setPoseToRest();
                }
                else
                {
                    boneChannels[i].getFrameAtTime(time, out location, out rotation);
                    bones[i].setPoseTranslation(location);
                    bones[i].setPoseRotation(rotation);
                }
            }
        }
    }
}
