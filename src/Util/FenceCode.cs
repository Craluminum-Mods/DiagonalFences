using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DiagonalFences
{
    public class Fence
    {
        public string Code { get; }
        public Block Block { get; }
        public Block Default { get; }
        public Block DefaultDiagonal { get; }

        public Fence(BlockPos centerPos, IWorldAccessor world) => Code = centerPos.GetFenceCode(world, out _);

        public Fence(Block block, BlockPos centerPos, IWorldAccessor world)
        {
            Default = world.GetBlock(new AssetLocation(block?.Attributes?["defaultCardinal"]?.AsString() ?? "game:woodenfence-oak-empty-free"));
            DefaultDiagonal = world.GetBlock(new AssetLocation(block?.Attributes?["defaultDrop"]?.AsString() ?? "diagonalfences:woodenfence-oak-ne_sw-free"));
            Code = centerPos.GetFenceCode(world, out var hasIntercardinalDirections);

            var interCardinalCode = DefaultDiagonal?.CodeWithVariants(new[] { "type", "wood" }, new[] { Code, block.Variant["wood"] });
            var cardinalCode = Default?.CodeWithVariants(new[] { "type", "wood" }, new[] { Code, block.Variant["wood"] });
            Block = hasIntercardinalDirections
                ? world.BlockAccessor.GetBlock(interCardinalCode)
                : world.BlockAccessor.GetBlock(cardinalCode);
        }
    }
}
