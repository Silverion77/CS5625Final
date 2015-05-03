using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using OpenTK;

using Assimp;
using Assimp.Configs;

using Chireiden.Materials;
using Chireiden.Meshes.Animations;

namespace Chireiden.Meshes
{
    public struct AnimationCue
    {
        public string Name;
        public double StartTime;
        public double EndTime;
        public bool Wrap;

        public AnimationCue(string name, int start, int end, bool wrap)
        {
            Name = name;
            StartTime = start / MeshImporter.FRAMES_PER_SECOND;
            EndTime = end / MeshImporter.FRAMES_PER_SECOND;
            Wrap = wrap;
        }
    }

    public class AnimationCueSheet
    {
        public List<AnimationCue> Cues;

        public AnimationCueSheet(string directory)
        {
            Cues = new List<AnimationCue>();

            string cueFilePath = System.IO.Path.Combine(directory, "animations.txt");
            using (StreamReader sr = new StreamReader(cueFilePath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] pieces = line.Split(' ');
                    AnimationCue cue = new AnimationCue(pieces[0], Int32.Parse(pieces[1]),
                        Int32.Parse(pieces[2]), Boolean.Parse(pieces[3]));
                    Cues.Add(cue);
                }
            }
        }
    }

    public class NodeFramesArray
    {
        public QuaternionKey[] RotationKeys { get; set; }
        public VectorKey[] LocationKeys { get; set; }
        public string NodeName { get; set; }

        public NodeFramesArray(NodeAnimationChannel channel)
        {
            NodeName = channel.NodeName;
            RotationKeys = channel.RotationKeys.ToArray();
            LocationKeys = channel.PositionKeys.ToArray();
        }
    }

    public partial class MeshImporter
    {
        /// <summary>
        /// The number of keyframes per second that Blender has exported.
        /// </summary>
        public const double FRAMES_PER_SECOND = 24.0;

        /// <summary>
        /// Extracts all of the animations.
        /// 
        /// Blender's exporters are kind of lacking, so the only way that 
        /// we can export multiple animations in the same file is by combining them
        /// all into one long animation strip, and separately specifying begin/end frames
        /// for each individual animation clip.
        /// </summary>
        /// <param name="model"></param>
        public static List<AnimationClip> importAnimations(Scene model, Dictionary<string, int> boneDict, int numBones, string directory)
        {
            AnimationCueSheet cueSheet = new AnimationCueSheet(directory);

            if (model.AnimationCount < 1)
            {
                Console.WriteLine("ERROR: file contains no animations");
                return null;
            }
            if (model.AnimationCount > 1)
            {
                throw new Exception("ERROR: file contains multiple animation clips, which we do not support");
            }

            Animation animStrip = model.Animations[0];
            List<NodeFramesArray> allFrames = new List<NodeFramesArray>();

            foreach (NodeAnimationChannel channel in animStrip.NodeAnimationChannels)
            {
                allFrames.Add(new NodeFramesArray(channel));
            }

            List<AnimationClip> clips = new List<AnimationClip>();

            foreach (AnimationCue cue in cueSheet.Cues)
            {
                AnimationClip clip = new AnimationClip(allFrames, cue, numBones, boneDict);
                clips.Add(clip);
            }
            return clips;
        }
    }
}
