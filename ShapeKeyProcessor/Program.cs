using System;
using System.Collections.Generic;
using System.IO;

using Assimp;

namespace ShapeKeyProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] morphNames = new string[] {
                                      "basis",
                                      "eyes_closed",
                                      "smile_eyes",
                                      "smile_mouth",
                                      "determined_eyes",
                                      "brow_down",
                                      "aa",
                                      "left_eye_closed",
                                      "caution",
                                      "><_eyes",
                                      "oo",
                                      "sad_brow"
                                  };

            List<MorphDisplacement>[] allMorphs = new List<MorphDisplacement>[morphNames.Length];
            Vector3D[] basis = ShapeKeyImporter.VerticesFromFile("../../frames/okuu_000000.obj");

            List<IDDisplacement>[] allMorphsByID = new List<IDDisplacement>[morphNames.Length];

            Scene baseModel = ShapeKeyImporter.ReadFromFile("../../okuu.dae");

            for (int i = 1; i <= 11; i++)
            {
                string num = i.ToString("D6");
                string morphName = morphNames[i];
                string filename = "../../frames/okuu_" + num + ".obj";
                Console.WriteLine("{0} contains {1}", filename, morphName);
                Vector3D[] morphPos = ShapeKeyImporter.VerticesFromFile(filename);
                List<MorphDisplacement> morphDiffs = ShapeKeyImporter.differences(basis, morphPos);
                allMorphs[i] = morphDiffs;
                allMorphsByID[i] = ShapeKeyImporter.diffMorphsToBase(baseModel, morphDiffs);
                Console.WriteLine("Matched {0} diffs for morph {1}", allMorphsByID[i].Count, morphName);
            }

            using (StreamWriter sw = new StreamWriter("../../blend_shapes.txt"))
            {
                for (int i = 1; i <= 11; i++)
                {
                    string morphName = morphNames[i];
                    List<IDDisplacement> morphDiffs = allMorphsByID[i];
                    sw.WriteLine("morph {0}", morphName);
                    foreach (IDDisplacement md in morphDiffs) {
                        sw.WriteLine("v {0}", md);
                    }
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
