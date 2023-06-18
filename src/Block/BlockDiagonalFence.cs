using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static DiagonalFences.GeneralExtensions;

namespace DiagonalFences
{
    public class BlockDiagonalFence : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            CanStep = false;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

            if (!this.TryPlaceFence(world, blockSel.Position))
            {
                return false;
            }
            UpdateFences(blockSel.Position, world);
            return true;
        }

        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            UpdateFences(pos, world);
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            UpdateFences(pos, world);
            base.OnBlockRemoved(world, pos);
        }

        public override string GetHeldItemName(ItemStack itemStack) => GetName(itemStack.Collectible);
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName(this);

        public static string GetName(CollectibleObject collectible)
        {
            return Lang.GetMatching(collectible?.Attributes?["name"]?.AsString()) + " (" + Lang.Get("diagonalfences:diagonal") + ")";
        }

        // public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        // {
        //     var sb = new StringBuilder();
        //     sb.AppendFormat("0: {0}", Variant?["type"]).AppendLine();
        //     sb.AppendFormat("1: {0}", new Fence(pos, world).Code).AppendLine();
        //     return sb.ToString();
        // }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[1] { new BlockDropItemStack(handbookStack) };
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            Block block = world.BlockAccessor.GetBlock(new AssetLocation(Attributes?["defaultDrop"]?.AsString()));
            return new ItemStack[1] { new ItemStack(block) };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(Attributes?["defaultDrop"]?.AsString())));
        }
    }
}
