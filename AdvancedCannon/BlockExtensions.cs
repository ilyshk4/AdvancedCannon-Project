using Modding.Blocks;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public static class BlockExtensions
    {
        public static bool CompareType(this Block block, BlockType type)
        {
            return (BlockType)block.Prefab.InternalObject.ID == type;
        }
    }
}