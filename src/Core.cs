using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

[assembly: ModInfo("Diagonal Fences", modID: "diagonalfences", Version = "0.0.3")]

namespace DiagonalFences;

public class Core : ModSystem
{
    private const string HarmonyId = "diagonalfences";

    private Harmony harmony;

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockBehaviorClass("DiagonalFence", typeof(BlockBehaviorDiagonalFence));
        PatchVanillaFencesOnce();
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        // our arms are built with the original texture of the item, load them first
        capi.Event.BlockTexturesLoaded += () => BuildArmMeshes(capi);
        new DiagonalFenceDebug(capi);
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(HarmonyId);
    }

    // Don't patch singleplayer two times (Server+Client)
    private void PatchVanillaFencesOnce()
    {
        if (Harmony.HasAnyPatches(HarmonyId)) return;

        harmony = new Harmony(HarmonyId);
        harmony.PatchAll();
    }

    private static void BuildArmMeshes(ICoreClientAPI capi)
    {
        foreach (Block block in capi.World.Blocks)
        {
            block?.GetBehavior<BlockBehaviorDiagonalFence>()?.BuildArmMeshes(capi);
        }
    }
}
