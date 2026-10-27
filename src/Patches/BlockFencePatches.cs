using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DiagonalFences;

/* Each patch runs after the vanilla method and only adds to its result,
   and only for fences that have BlockBehaviorDiagonalFence.
   This is all done to be as inobtrusive as possible so if a mod interacts
   with the vanilla fence behaviour if they still dont do anything fancy and
   keep the return fairly similiar we can alter the result to add our
   things in the postfix event
*/

[HarmonyPatch(typeof(BlockFence), nameof(BlockFence.OnJsonTesselation))]
public static class AddDiagonalArmsToFenceMesh
{
    public static void Postfix(BlockFence __instance, ref MeshData sourceMesh, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        var diagonalFence = __instance.GetBehavior<BlockBehaviorDiagonalFence>();
        if (diagonalFence == null || chunkExtBlocks == null) return;

        bool isStacked = IsStacked(chunkExtBlocks, extIndex3d);
        sourceMesh = diagonalFence.WithDiagonalArms(sourceMesh, corner => chunkExtBlocks[NeighbourIndex(extIndex3d, corner)], pos, isStacked);
    }

    // chunkExtBlocks is the chunk+1 block border, MoveIndex is filled in by the game at runtime so we cant cache it.
    private static int NeighbourIndex(int fenceIndex, Corner corner)
    {
        return fenceIndex
            + TileSideEnum.MoveIndex[corner.NorthOrSouth.Index]
            + TileSideEnum.MoveIndex[corner.EastOrWest.Index];
    }

    // Same check stackable stone walls use to drop their capstones when another fence sits on top
    private static bool IsStacked(Block[] chunkExtBlocks, int fenceIndex)
    {
        Block blockAbove = chunkExtBlocks[fenceIndex + TileSideEnum.MoveIndex[TileSideEnum.Up]];
        return blockAbove is BlockFence;
    }
}

// Stackable stone walls (drystone, coral) replace OnJsonTesselation without calling
// the BlockFence one, so the patch above never runs for them: they need their own
[HarmonyPatch(typeof(BlockFenceStackAware), nameof(BlockFenceStackAware.OnJsonTesselation))]
public static class AddDiagonalArmsToStackableWallMesh
{
    public static void Postfix(BlockFenceStackAware __instance, ref MeshData sourceMesh, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        AddDiagonalArmsToFenceMesh.Postfix(__instance, ref sourceMesh, pos, chunkExtBlocks, extIndex3d);
    }
}

[HarmonyPatch(typeof(BlockFence), nameof(BlockFence.GetSelectionBoxes))]
public static class AddDiagonalSelectionBoxes
{
    public static void Postfix(BlockFence __instance, IBlockAccessor blockAccessor, BlockPos pos, ref Cuboidf[] __result)
    {
        var diagonalFence = __instance.GetBehavior<BlockBehaviorDiagonalFence>();
        if (diagonalFence == null) return;

        __result = diagonalFence.WithDiagonalBoxes(__result, blockAccessor, pos, BlockBehaviorDiagonalFence.SelectionHeight);
    }
}

// BlockFence doesn't have its own GetCollisionBoxes, so this patches the one every block uses
[HarmonyPatch(typeof(Block), nameof(Block.GetCollisionBoxes))]
public static class AddDiagonalCollisionBoxes
{
    public static void Postfix(Block __instance, IBlockAccessor blockAccessor, BlockPos pos, ref Cuboidf[] __result)
    {
        if (__instance is not BlockFence) return;
        var diagonalFence = __instance.GetBehavior<BlockBehaviorDiagonalFence>();
        if (diagonalFence == null) return;

        __result = diagonalFence.WithDiagonalBoxes(__result, blockAccessor, pos, BlockBehaviorDiagonalFence.CollisionHeight);
    }
}
