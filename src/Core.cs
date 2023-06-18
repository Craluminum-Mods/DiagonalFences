using Vintagestory.API.Common;

[assembly: ModInfo("Diagonal Fences")]

namespace DiagonalFences;

public class Core : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("BlockDiagonalFence", typeof(BlockDiagonalFence));
        api.World.Logger.Event("started 'Diagonal Fences' mod");
    }
}