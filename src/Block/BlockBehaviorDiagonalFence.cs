using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using static DiagonalFences.GeneralExtensions;

namespace DiagonalFences;

public class BlockBehaviorDiagonalFence : BlockBehavior
{
    public BlockBehaviorDiagonalFence(Block block) : base(block) { }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        block.TryPlaceFence(world, blockPos);
        UpdateFences(blockPos, world);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
    {
        block.TryPlaceFence(world, pos);
    }
}
