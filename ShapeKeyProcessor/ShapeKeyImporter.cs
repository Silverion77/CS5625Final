using System;
using System.Collections.Generic;
using System.IO;

using Assimp;
using Assimp.Configs;

namespace ShapeKeyProcessor
{
    struct MorphDisplacement
    {
        Vector3D source;
        Vector3D displacement;

        public Vector3D Vertex
        {
            get
            {
                return source;
            }
        }

        public Vector3D Displacement
        {
            get
            {
                return displacement;
            }
        }

        public MorphDisplacement(Vector3D from, Vector3D diff)
        {
            source = from;
            displacement = diff;
        }

        public override string ToString()
        {
            return Vertex.X + " " + Vertex.Y + " " + Vertex.Z + " " + Displacement.X + " " + Displacement.Y + " " + Displacement.Z;
        }
    }

    struct IDDisplacement
    {
        public int MeshNum;
        public int VertID;
        public Vector3D Displacement;

        public IDDisplacement(int meshNum, int vert, Vector3D disp)
        {
            MeshNum = meshNum;
            VertID = vert;
            Displacement = disp;
        }

        public override string ToString()
        {
            return MeshNum + " " + VertID + " " + Displacement.X + " " + Displacement.Y + " " + Displacement.Z;
        }
    }

    class ShapeKeyImporter
    {
        public static Vector3D[] VerticesFromFile(string filename)
        {
            List<Vector3D> verts = new List<Vector3D>();
            using (StreamReader sr = new StreamReader(filename))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] pieces = line.Split(' ');
                    if (!pieces[0].Equals("v")) continue;

                    float x = 0;
                    float y = 0;
                    float z = 0;
                    
                    bool success = float.TryParse(pieces[1], out x) &&
                        float.TryParse(pieces[2], out y) &&
                        float.TryParse(pieces[3], out z);

                    if (!success)
                    {
                        throw new FormatException();
                    }

                    verts.Add(new Vector3D(x, y, z));
                }
            }
            return verts.ToArray();
        }

        public static List<MorphDisplacement> differences(Vector3D[] basis, Vector3D[] morph)
        {
            List<MorphDisplacement> diffs = new List<MorphDisplacement>();
            for (int i = 0; i < basis.Length; i++)
            {
                Vector3D diff = morph[i] - basis[i];
                if (diff.Length() > 1e-6)
                {
                    diffs.Add(new MorphDisplacement(basis[i], diff));
                }
            }
            Console.WriteLine("{0} diffs", diffs.Count);
            return diffs;
        }

        static bool findVertexAtPos(Vector3D pos, List<MorphDisplacement> morphDiffs, out MorphDisplacement morph)
        {
            foreach (MorphDisplacement md in morphDiffs)
            {
                Vector3D diff = md.Vertex - pos;
                if (diff.Length() < 1e-6)
                {
                    morph = md;
                    return true;
                }
            }
            morph = morphDiffs[0];
            return false;
        }

        public static Scene ReadFromFile(string baseModelFile)
        {
            Console.WriteLine("Importing {0}", baseModelFile);
            // Create a new importer
            AssimpContext importer = new AssimpContext();

            //This is how we add a configuration (each config is its own class)
            NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
            importer.SetConfig(config);

            //Import the model. All configs are set. The model
            //is imported, loaded into managed memory. Then the unmanaged memory is released, and everything is reset.
            Scene model = importer.ImportFile(baseModelFile, PostProcessPreset.TargetRealTimeMaximumQuality);

            importer.Dispose();
            return model;
        }

        public static List<IDDisplacement> diffMorphsToBase(Scene model, List<MorphDisplacement> morphDiffs)
        {
            List<IDDisplacement> dispsByID = new List<IDDisplacement>();

            int meshNum = 0;
            foreach (Mesh m in model.Meshes) {
                int vertID = 0;
                foreach (Vector3D vert in m.Vertices)
                {
                    MorphDisplacement disp;
                    if (!findVertexAtPos(vert, morphDiffs, out disp))
                    {
                        vertID++;
                        continue;
                    }
                    IDDisplacement idd = new IDDisplacement(meshNum, vertID, disp.Displacement);
                    dispsByID.Add(idd);
                    vertID++;
                }
                meshNum++;
            }

            return dispsByID;
        }
    }
}
