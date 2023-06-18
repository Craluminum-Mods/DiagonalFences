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

    public static bool IsFenceOrGate(BlockPos targetPos, IWorldAccessor world)
    {
        return world.BlockAccessor.GetBlock(targetPos) is BlockFence or BlockDiagonalFence or BlockFenceGate or BlockFenceGateRoughHewn;
    }

    public static string GetFenceCode(this BlockPos centerPos, IWorldAccessor world, out bool hasIntercardinalDirections)
    {
        List<string> matchedDirections = new();

        if (IsFenceOrGate(centerPos.NorthCopy(), world)) matchedDirections.Add("n");
        if (IsFenceOrGate(centerPos.EastCopy().NorthCopy(), world)) matchedDirections.Add("ne");
        if (IsFenceOrGate(centerPos.EastCopy(), world)) matchedDirections.Add("e");
        if (IsFenceOrGate(centerPos.EastCopy().SouthCopy(), world)) matchedDirections.Add("es");
        if (IsFenceOrGate(centerPos.SouthCopy(), world)) matchedDirections.Add("s");
        if (IsFenceOrGate(centerPos.WestCopy().SouthCopy(), world)) matchedDirections.Add("sw");
        if (IsFenceOrGate(centerPos.WestCopy(), world)) matchedDirections.Add("w");
        if (IsFenceOrGate(centerPos.WestCopy().NorthCopy(), world)) matchedDirections.Add("nw");

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