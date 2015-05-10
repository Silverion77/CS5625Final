using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chireiden.Meshes;

namespace Chireiden
{
    public class MeshLibrary
    {
        public static MeshContainer TextCube;
        public static MeshContainer Okuu;
        public static MeshContainer HappySphere;
        public static MeshContainer ZombieFairy;
        public static MeshContainer GoalFlag;
        public static MeshContainer Sphere;

        public static void loadMeshes()
        {
            TextCube = MeshImporter.importFromFile("data/model/textCube/textureCube.dae");
            Okuu = MeshImporter.importFromFile("data/model/okuu/okuu.dae");
            HappySphere = MeshImporter.importFromFile("data/model/happysphere/happysphere.dae");
            ZombieFairy = MeshImporter.importFromFile("data/model/zombie_fairy/zombie_fairy.dae");
            GoalFlag = MeshImporter.importFromFile("data/model/goal_flag/goal_flag.dae");
            Sphere = MeshImporter.importFromFile("data/model/sphere/sphere.dae");
        }
    }
}
