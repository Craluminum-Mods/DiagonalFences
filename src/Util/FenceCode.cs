using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DiagonalFences
{
    public class Fence
    {
        public string Code { get; }
        public Block Block { get; }
        public Block Default { get; }
        public Block DefaultDiagonal { get; }

        public Fence(Block block, BlockPos centerPos, IWorldAccessor world)
        {
            Default = world.GetBlock(new AssetLocation(block?.Attributes?["defaultCardinal"]?.AsString() ?? "game:woodenfence-oak-empty-free"));
            DefaultDiagonal = world.GetBlock(new AssetLocation(block?.Attributes?["defaultDrop"]?.AsString() ?? "diagonalfences:woodenfence-oak-ne_sw-free"));
            Code = centerPos.GetFenceCode(world, Default as BlockFence, out var hasIntercardinalDirections);

            var cover = block.Variant["cover"] ?? "free";
            var interCardinalCode = DefaultDiagonal?.CodeWithVariants(new[] { "type", "wood", "cover" }, new[] { Code, block.Variant["wood"], cover });

            var cardinalCode = Default?.CodeWithVariants(new[] { "type", "wood", "cover" }, new[] { Code, block.Variant["wood"], cover });
            Block = hasIntercardinalDirections
                ? world.BlockAccessor.GetBlock(interCardinalCode)
                : world.BlockAccessor.GetBlock(cardinalCode);
        }
    }
}
