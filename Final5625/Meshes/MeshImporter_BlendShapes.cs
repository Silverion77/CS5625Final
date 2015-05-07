using System;
using System.Collections.Generic;
using System.IO;

using OpenTK;
using Assimp;

namespace Chireiden.Meshes
{
    public struct MeshVertDisplacement
    {
        public int MeshID;
        public int VertID;
        public Vector3 Displacement;

        public MeshVertDisplacement(int m, int v, Vector3 vec)
        {
            MeshID = m;
            VertID = v;
            Displacement = vec;
        }
    }

    public class BlendShapeInfo
    {
        Queue<MeshVertDisplacement> disps;
        string name;

        public string Name { get { return name; } }

        public BlendShapeInfo(string n)
        {
            disps = new Queue<MeshVertDisplacement>();
            name = n;
        }

        public void appendMVD(MeshVertDisplacement mvd)
        {
            disps.Enqueue(mvd);
        }

        public bool meshVertexMatches(int meshNum, int vertID)
        {
            if (disps.Count == 0) return false;
            MeshVertDisplacement mvd = disps.Peek();
            if (mvd.MeshID == meshNum && mvd.VertID == vertID)
            {
                return true;
            }
            return false;
        }

        public Vector3 popFirstDisplacement()
        {
            MeshVertDisplacement mvd = disps.Dequeue();
            return mvd.Displacement;
        }
    }

    public partial class MeshImporter
    {
        static BlendShapeInfo[] readBlendShapes(string filename)
        {
            List<BlendShapeInfo> allMorphs = new List<BlendShapeInfo>();
            BlendShapeInfo bsi = null;
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] pieces = line.Split(' ');
                    if (pieces[0].Equals("morph"))
                    {
                        if (bsi != null)
                        {
                            allMorphs.Add(bsi);
                        }
                        Console.WriteLine("Begin reading data for morph {0}", pieces[1]);
                        bsi = new BlendShapeInfo(pieces[1]);
                    }
                    else if (pieces[0].Equals("v"))
                    {
                        int meshID = 0;
                        int vertID = 0;
                        float x = 0;
                        float y = 0;
                        float z = 0;
                        bool success = Int32.TryParse(pieces[1], out meshID) &&
                            Int32.TryParse(pieces[2], out vertID) &&
                            float.TryParse(pieces[3], out x) &&
                            float.TryParse(pieces[4], out y) &&
                            float.TryParse(pieces[5], out z);
                        if (!success) throw new FormatException("Could not parse blend_shapes.txt");
                        Vector3 disp = new Vector3(x, y, z);
                        MeshVertDisplacement mvd = new MeshVertDisplacement(meshID, vertID, disp);
                        bsi.appendMVD(mvd);
                    }
                }
            }
            if (bsi != null)
            {
                allMorphs.Add(bsi);
            }
            return allMorphs.ToArray();
        }
    }
}
