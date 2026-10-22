using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DiagonalFences;

/// <summary>
/// Client side debug view, toggled with ".diagonalfences debug": lists the diagonal connections
/// in a fence's block info and highlights the fences it connects to.
/// </summary>
public class DiagonalFenceDebug
{
    private const int HighlightSlot = 4815;  // any id, it just has to differ from other mods' highlights
    private const int CheckIntervalMs = 100;
    private static readonly int ConnectedColor = ColorUtil.ColorFromRgba(0, 255, 0, 96);

    public static bool IsEnabled { get; private set; }

    private readonly ICoreClientAPI capi;
    private long checkListenerId;
    private List<BlockPos> highlighted = [];

    public DiagonalFenceDebug(ICoreClientAPI capi)
    {
        this.capi = capi;

        capi.ChatCommands.Create("diagonalfences")
            .WithAlias("df")
            .WithDescription("Diagonal Fences tools")
            .RequiresPrivilege(Privilege.chat)
            .BeginSubCommand("debug")
                .WithDescription("Show or hide the diagonal connections of the fence you are looking at")
                .HandleWith(_ => Toggle())
            .EndSubCommand();
    }

    private TextCommandResult Toggle()
    {
        IsEnabled = !IsEnabled;

        if (IsEnabled)
        {
            checkListenerId = capi.Event.RegisterGameTickListener(_ => HighlightConnectedFences(), CheckIntervalMs);
            return TextCommandResult.Success("Diagonal connections are now shown on the fence you look at");
        }

        capi.Event.UnregisterGameTickListener(checkListenerId);
        Highlight([]);
        return TextCommandResult.Success("Diagonal connections are now hidden");
    }

    private void HighlightConnectedFences()
    {
        List<BlockPos> connected = FencesConnectedToLookedAtFence();
        if (!connected.SequenceEqual(highlighted)) Highlight(connected);
    }

    private List<BlockPos> FencesConnectedToLookedAtFence()
    {
        BlockPos pos = capi.World.Player.CurrentBlockSelection?.Position;
        if (pos == null) return [];

        var fence = capi.World.BlockAccessor.GetBlock(pos).GetBehavior<BlockBehaviorDiagonalFence>();
        if (fence == null) return [];

        return fence.ConnectedCorners(capi.World.BlockAccessor, pos)
            .Select(corner => corner.NeighbourOf(pos))
            .ToList();
    }

    private void Highlight(List<BlockPos> positions)
    {
        highlighted = positions;
        List<int> colors = positions.Select(_ => ConnectedColor).ToList();
        capi.World.HighlightBlocks(capi.World.Player, HighlightSlot, positions, colors,
            EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary, 1f);
    }
}
