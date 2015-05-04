﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chireiden.Meshes;

namespace Chireiden
{
    class MeshLibrary
    {
        public static MeshContainer TextCube;
        public static MeshContainer Okuu;
        public static MeshContainer HappySphere;

        public static void loadMeshes()
        {
            TextCube = MeshImporter.importFromFile("data/model/textCube/textureCube.dae");
            Okuu = MeshImporter.importFromFile("data/model/okuu/okuu.dae");
            HappySphere = MeshImporter.importFromFile("data/model/happysphere/happysphere.dae");
        }
    }
}
