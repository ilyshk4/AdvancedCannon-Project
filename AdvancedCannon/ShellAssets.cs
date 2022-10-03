    using Modding;
using UnityEngine;

namespace AdvancedCannon
{
    // Proxifimity fuse
    // reactive armor

    public struct ShellAssets
    {
        public Mesh mesh;
        public Texture2D texture;

        public ShellAssets(string prefix)
        {
            mesh = ModResource.GetMesh($"{prefix}_Mesh");
            texture = ModResource.GetTexture($"{prefix}_Texture");
        }
    }
}
