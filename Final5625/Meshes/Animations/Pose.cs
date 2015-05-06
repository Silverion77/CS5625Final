using System;
using System.Collections.Generic;

using OpenTK;

namespace Chireiden.Meshes.Animations
{
    /// <summary>
    /// Wraps a set of keyframes for a skeleton. Used for interpolations.
    /// </summary>
    class Pose
    {
        Vector3[] locationKeys;
        Quaternion[] rotationKeys;
        bool[] hasChannel;

        public Pose()
        {
            makeArrays(1);
        }

        public Pose(int numChannels)
        {
            makeArrays(numChannels);
        }

        public void makeArrays(int numChannels)
        {
            locationKeys = new Vector3[numChannels];
            rotationKeys = new Quaternion[numChannels];
            hasChannel = new bool[numChannels];
        }

        public void setFromAnimationTime(Clip clip, double t)
        {
            if (clip.NumChannels != locationKeys.Length)
            {
                makeArrays(clip.NumChannels);
            }
            for (int i = 0; i < clip.NumChannels; i++)
            {
                Vector3 loc;
                Quaternion rot;
                if (!clip.getLocRotAtTime(i, t, out loc, out rot))
                {
                    hasChannel[i] = false;
                }
                else
                {
                    hasChannel[i] = true;
                    locationKeys[i] = loc;
                    rotationKeys[i] = rot;
                }
            }
        }

        public bool getLocRotOnChannel(int i, out Vector3 location, out Quaternion rotation)
        {
            if (hasChannel[i])
            {
                location = locationKeys[i];
                rotation = rotationKeys[i];
                return true;
            }
            else
            {
                location = Vector3.Zero;
                rotation = Quaternion.Identity;
                return false;
            }
        }
    }
}
