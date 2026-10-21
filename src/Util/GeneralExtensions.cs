using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DiagonalFences;

public static class GeneralExtensions
{
    public static bool TryPlaceFence(this Block fromBlock, IWorldAccessor world, BlockPos pos)
    {
        var toFence = new Fence(fromBlock, pos, world);
        if (toFence.Block == null) return false;
        if (fromBlock is BlockFenceGate or BlockFenceGateRoughHewn) return false;
        if (world.BlockAccessor.GetBlock(pos).Id == toFence.Block.Id) return false;

        world.BlockAccessor.SetBlock(toFence.Block.BlockId, pos);
        return true;
    }

    public static void UpdateFences(BlockPos centerPos, IWorldAccessor world)
    {
        var positionsAround = centerPos.GetPositionsAround();
        foreach (var _pos in positionsAround)
        {
            var _block = world.BlockAccessor.GetBlock(_pos);
            _block.TryPlaceFence(world, _pos);
        }
    }

    public static BlockPos[] GetPositionsAround(this BlockPos centerPos)
    {
        return new[]
        {
            centerPos.NorthCopy(),
            centerPos.EastCopy(),
            centerPos.SouthCopy(),
            centerPos.WestCopy(),
            centerPos.EastCopy().NorthCopy(),
            centerPos.EastCopy().SouthCopy(),
            centerPos.WestCopy().SouthCopy(),
            centerPos.WestCopy().NorthCopy(),
        };
    }

    public static bool IsFence(BlockPos targetPos, IWorldAccessor world)
    {
        return world.BlockAccessor.GetBlock(targetPos) is BlockFence or BlockDiagonalFence;
    }

    public static string GetFenceCode(this BlockPos centerPos, IWorldAccessor world, BlockFence vanillaFence, out bool hasIntercardinalDirections)
    {
        List<string> matchedDirections = new();

        bool n = vanillaFence.ShouldConnectAt(world, centerPos, BlockFacing.NORTH);
        bool e = vanillaFence.ShouldConnectAt(world, centerPos, BlockFacing.EAST);
        bool s = vanillaFence.ShouldConnectAt(world, centerPos, BlockFacing.SOUTH);
        bool w = vanillaFence.ShouldConnectAt(world, centerPos, BlockFacing.WEST);
        bool ne = IsFence(centerPos.EastCopy().NorthCopy(), world);
        bool es = IsFence(centerPos.EastCopy().SouthCopy(), world);
        bool sw = IsFence(centerPos.WestCopy().SouthCopy(), world);
        bool nw = IsFence(centerPos.WestCopy().NorthCopy(), world);

        //Checks are made in order for two reasons:
        // - block variant names
        // - Preferring L shaped instead of diagonal

        if (n) matchedDirections.Add("n");
        if (!n && !e && ne) matchedDirections.Add("ne");
        if (e) matchedDirections.Add("e");
        if (!e && !s && es) matchedDirections.Add("es");
        if (s) matchedDirections.Add("s");
        if (!s && !w && sw) matchedDirections.Add("sw");
        if (w) matchedDirections.Add("w");
        if (!n && !w && nw) matchedDirections.Add("nw");


        hasIntercardinalDirections = matchedDirections.Any(dir => dir.Length == 2);

        if (matchedDirections.Count == 0)
        {
            return "empty";
        }
        else if (matchedDirections.All(dir => dir.Length == 1))
        {
            return string.Concat(matchedDirections);
        }
        else
        {
            return string.Join("_", matchedDirections);
        }
    }
}