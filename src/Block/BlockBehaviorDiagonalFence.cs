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
        if (armShape != null)
        {
            armShapePath = AssetLocation.Create(armShape).WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
        }
    }

    public void BuildArmMeshes(ICoreClientAPI capi)
    {
        Shape armShape = armShapePath == null ? null : Shape.TryGet(capi, armShapePath);
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
        List<Corner> corners = ConnectedCorners(neighbourAt).ToList();
        if (corners.Count == 0 || armMeshes.Count == 0) return fenceMesh;

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

        return cornerBoxes.Length == 0 ? fenceBoxes : [.. fenceBoxes ?? [], .. cornerBoxes];
    }

    public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        if (!DiagonalFenceDebug.IsEnabled) return "";

        List<string> corners = ConnectedCorners(world.BlockAccessor, pos)
            .Select(corner => corner.Code)
            .ToList();

        return "Diagonals: " + (corners.Count == 0 ? "none" : string.Join(", ", corners));
    }

    public IEnumerable<Corner> ConnectedCorners(IBlockAccessor world, BlockPos pos)
    {
        return ConnectedCorners(corner => world.GetBlock(corner.NeighbourOf(pos)));
    }

    /// <summary>
    /// A corner connects when the neighbour is also a diagonal fence and neither side
    /// next to that corner is already connected, so an L shape stays an L instead of a triangle.
    /// </summary>
    private IEnumerable<Corner> ConnectedCorners(NeighbourLookup neighbourAt)
    {
        return Corner.All.Where(corner =>
            !IsConnectedTowards(corner.NorthOrSouth) &&
            !IsConnectedTowards(corner.EastOrWest) &&
            IsDiagonalFence(neighbourAt(corner)));
    }

    // Vanilla stores its connections in the "type" variant: "ns", "new", "empty" ...
    private bool IsConnectedTowards(BlockFacing side)
    {
        string connections = block.Variant["type"];
        return connections != "empty" && connections.Contains(side.Code[0]);
    }

    private static bool IsDiagonalFence(Block block) => block?.HasBehavior<BlockBehaviorDiagonalFence>() == true;

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
