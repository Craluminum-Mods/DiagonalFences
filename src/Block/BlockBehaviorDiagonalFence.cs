using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace DiagonalFences;

public delegate Block NeighbourLookup(Corner corner);

/// <summary>
/// Lets a vanilla fence connect diagonally. The fence block itself never changes:
/// the patches in BlockFencePatches ask this behavior for the extra arms and boxes.
/// Patch property "armShape": the shape of one arm pointing north-east.
/// </summary>
public class BlockBehaviorDiagonalFence : BlockBehavior
{
    // Same heights as vanilla fences (reminiscent of the old version that was
    // a little bit less and creatures could get on top and get stuck)
    public const float SelectionHeight = 1.0625f;
    public const float CollisionHeight = 1.3125f;

    private AssetLocation armShapePath;
    private readonly Dictionary<Corner, MeshData> armMeshes = new();

    public BlockBehaviorDiagonalFence(Block block) : base(block) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        string armShape = properties["armShape"].AsString();
        if (armShape == null) return;

        armShapePath = AssetLocation.Create(armShape)
            .WithPathPrefixOnce("shapes/")
            .WithPathAppendixOnce(".json");
    }

    public void BuildArmMeshes(ICoreClientAPI capi)
    {
        Shape armShape = null;
        if (armShapePath != null)
        {
            armShape = Shape.TryGet(capi, armShapePath);
        }

        if (armShape == null)
        {
            capi.Logger.Warning("[diagonalfences] {0} has no usable armShape ({1}), it won't connect diagonally", block.Code, armShapePath);
            return;
        }

        foreach (Corner corner in Corner.All)
        {
            capi.Tesselator.TesselateShape(block, armShape, out MeshData arm, new Vec3f(0, corner.ArmRotation, 0));
            armMeshes[corner] = arm;
        }
    }

    public MeshData WithDiagonalArms(MeshData fenceMesh, NeighbourLookup neighbourAt)
    {
        bool hasArms = armMeshes.Count > 0;
        if (!hasArms) return fenceMesh;

        List<Corner> corners = ConnectedCorners(neighbourAt).ToList();
        if (corners.Count == 0) return fenceMesh;

        // the vanilla mesh is shared by every fence of this type, so work on a copy
        MeshData mesh = fenceMesh.Clone();
        foreach (Corner corner in corners)
        {
            mesh.AddMeshData(armMeshes[corner]);
        }
        return mesh;
    }

    public Cuboidf[] WithDiagonalBoxes(Cuboidf[] fenceBoxes, IBlockAccessor world, BlockPos pos, float height)
    {
        Cuboidf[] cornerBoxes = ConnectedCorners(world, pos)
            .Select(corner => corner.Box(height))
            .ToArray();

        if (cornerBoxes.Length == 0) return fenceBoxes;

        // a new array: the vanilla one is shared by every fence of this type
        Cuboidf[] vanillaBoxes = fenceBoxes ?? [];
        return [.. vanillaBoxes, .. cornerBoxes];
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        if (!DiagonalFenceDebug.IsEnabled) return "";

        List<string> corners = ConnectedCorners(world.BlockAccessor, pos)
            .Select(corner => corner.Code)
            .ToList();

        if (corners.Count == 0) return "Diagonals: none";

        return "Diagonals: " + string.Join(", ", corners);
    }

    public IEnumerable<Corner> ConnectedCorners(IBlockAccessor world, BlockPos pos)
    {
        return ConnectedCorners(corner => world.GetBlock(corner.NeighbourOf(pos)));
    }

    /// <summary>
    /// A corner connects only when both fences agree: the neighbour there is a diagonal fence
    /// and each of the two can connect towards the other. Otherwise one side would draw half a link.
    /// </summary>
    private IEnumerable<Corner> ConnectedCorners(NeighbourLookup neighbourAt)
    {
        foreach (Corner corner in Corner.All)
        {
            if (!CanConnectTowards(corner)) continue;

            Block neighbourBlock = neighbourAt(corner);
            var neighbour = neighbourBlock?.GetBehavior<BlockBehaviorDiagonalFence>();
            if (neighbour == null) continue;

            if (!neighbour.CanConnectTowards(corner.Opposite)) continue;

            yield return corner;
        }
    }

    // Not when a side next to that corner is already connected, so an L shape stays an L instead of a triangle
    private bool CanConnectTowards(Corner corner)
    {
        bool northOrSouthConnected = IsConnectedTowards(corner.NorthOrSouth);
        bool eastOrWestConnected = IsConnectedTowards(corner.EastOrWest);

        return !northOrSouthConnected && !eastOrWestConnected;
    }

    // Vanilla stores its connections in the "type" variant: "ns", "new", "empty" ...
    private bool IsConnectedTowards(BlockFacing side)
    {
        string connections = block.Variant["type"];
        if (connections == "empty") return false;

        char sideLetter = side.Code[0];
        return connections.Contains(sideLetter);
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        RedrawDiagonalNeighbours(world, blockPos);
    }

    public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        RedrawDiagonalNeighbours(world, pos);
    }

    // The game only updates the 6 neighbours sharing a face, but the diagonal ones draw an arm towards us
    private static void RedrawDiagonalNeighbours(IWorldAccessor world, BlockPos pos)
    {
        foreach (Corner corner in Corner.All)
        {
            world.BlockAccessor.MarkBlockDirty(corner.NeighbourOf(pos));
        }
    }
}
