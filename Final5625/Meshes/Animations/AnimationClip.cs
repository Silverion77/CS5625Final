using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes.Animations
{
    public class AnimationClip
    {
        public string Name { get; set; }
        /// <summary>
        /// boneChannels[i] contains all of the animation keyframes for this animation clip
        /// that affect the bone with ID i.
        /// </summary>
        LocRotKeyframes[] boneChannels;

        public double Duration { get; set; }

        public bool Wrap { get; set; }

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
            Name = cue.Name;
            Wrap = cue.Wrap;
            Duration = 0;

            foreach (LocRotKeyframes frames in boneChannels) {
                if (frames == null) continue;
                Duration = Math.Max(Duration, frames.Length);
            }
        }

        public void applyAnimationToSkeleton(ArmatureBone[] bones, double time)
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
