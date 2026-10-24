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
/// Patch properties:
///   "armShape": the shape of one arm pointing north-east, or a list of alternatives
///   that each fence picks from by its position, like vanilla's random shapes.
///   "snowArmShape" (optional): same, used instead when the fence's cover is snow. For snowy
///   blocktypes that inherit this behavior from their snow-free one, like the rough-hewn fence.
///   "cardinalOvershootShape" (optional): rails continuing a west connection past the centre,
///   for fences like bamboo whose rails cross at every joint.
/// </summary>
public class BlockBehaviorDiagonalFence : BlockBehavior
{
    // Same heights as vanilla fences (reminiscent of the old version that was
    // a little bit less and creatures could get on top and get stuck)
    public const float SelectionHeight = 1.0625f;
    public const float CollisionHeight = 1.3125f;

    private List<AssetLocation> armShapePaths = [];
    private List<AssetLocation> snowArmShapePaths = [];
    private AssetLocation cardinalOvershootShapePath;
    private readonly List<Dictionary<Corner, MeshData>> armVariants = [];
    private readonly Dictionary<BlockFacing, MeshData> cardinalOvershootMeshes = new();

    public BlockBehaviorDiagonalFence(Block block) : base(block) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        armShapePaths = ShapePaths(properties["armShape"]);
        snowArmShapePaths = ShapePaths(properties["snowArmShape"]);
        cardinalOvershootShapePath = ShapePath(properties["cardinalOvershootShape"].AsString());
    }

    private static List<AssetLocation> ShapePaths(JsonObject shapes)
    {
        if (!shapes.Exists) return [];

        if (!shapes.IsArray())
        {
            return [ShapePath(shapes.AsString())];
        }

        return shapes.AsArray()
            .Select(shape => ShapePath(shape.AsString()))
            .ToList();
    }

    private static AssetLocation ShapePath(string shape)
    {
        if (shape == null) return null;

        return AssetLocation.Create(shape)
            .WithPathPrefixOnce("shapes/")
            .WithPathAppendixOnce(".json");
    }

    public void BuildArmMeshes(ICoreClientAPI capi)
    {
        foreach (AssetLocation path in ArmShapePathsForThisFence())
        {
            Shape armShape = LoadShape(capi, path);
            if (armShape == null)
            {
                capi.Logger.Warning("[diagonalfences] {0} can't load its armShape {1}", block.Code, path);
                continue;
            }

            armVariants.Add(TesselateArms(capi, armShape));
        }

        if (armVariants.Count == 0)
        {
            capi.Logger.Warning("[diagonalfences] {0} has no usable armShape, it won't connect diagonally", block.Code);
            return;
        }

        BuildCardinalOvershootMeshes(capi);
    }

    private List<AssetLocation> ArmShapePathsForThisFence()
    {
        bool isSnowCovered = block.Variant["cover"] == "snow";
        bool hasSnowArms = snowArmShapePaths.Count > 0;

        if (isSnowCovered && hasSnowArms) return snowArmShapePaths;
        return armShapePaths;
    }

    private Dictionary<Corner, MeshData> TesselateArms(ICoreClientAPI capi, Shape armShape)
    {
        var arms = new Dictionary<Corner, MeshData>();
        foreach (Corner corner in Corner.All)
        {
            capi.Tesselator.TesselateShape(block, armShape, out MeshData arm, new Vec3f(0, corner.ArmRotation, 0));
            arms[corner] = arm;
        }
        return arms;
    }

    private void BuildCardinalOvershootMeshes(ICoreClientAPI capi)
    {
        if (cardinalOvershootShapePath == null) return;

        Shape overshootShape = LoadShape(capi, cardinalOvershootShapePath);
        if (overshootShape == null)
        {
            capi.Logger.Warning("[diagonalfences] {0} has no usable cardinalOvershootShape ({1})", block.Code, cardinalOvershootShapePath);
            return;
        }

        foreach (BlockFacing side in BlockFacing.HORIZONTALS)
        {
            capi.Tesselator.TesselateShape(block, overshootShape, out MeshData overshoot, new Vec3f(0, RotationFromWest(side), 0));
            cardinalOvershootMeshes[side] = overshoot;
        }
    }

    private static Shape LoadShape(ICoreClientAPI capi, AssetLocation path)
    {
        if (path == null) return null;

        return Shape.TryGet(capi, path);
    }

    // Same turns vanilla uses to point its one-sided "w" fence shape at each side
    private static float RotationFromWest(BlockFacing side)
    {
        if (side == BlockFacing.NORTH) return 270;
        if (side == BlockFacing.EAST) return 180;
        if (side == BlockFacing.SOUTH) return 90;
        return 0;
    }

    public MeshData WithDiagonalArms(MeshData fenceMesh, NeighbourLookup neighbourAt, BlockPos pos)
    {
        bool hasArms = armVariants.Count > 0;
        if (!hasArms) return fenceMesh;

        List<Corner> corners = ConnectedCorners(neighbourAt).ToList();
        if (corners.Count == 0) return fenceMesh;

        Dictionary<Corner, MeshData> arms = ArmVariantAt(pos);

        // the vanilla mesh is shared by every fence of this type, so work on a copy
        MeshData mesh = fenceMesh.Clone();
        foreach (Corner corner in corners)
        {
            mesh.AddMeshData(arms[corner]);
        }

        AddCardinalOvershoot(mesh);
        return mesh;
    }

    // Picked from the position like vanilla's random shapes, so a fence always keeps the same look
    private Dictionary<Corner, MeshData> ArmVariantAt(BlockPos pos)
    {
        int variant = GameMath.MurmurHash3Mod(pos.X, pos.Y, pos.Z, armVariants.Count);
        return armVariants[variant];
    }

    // Vanilla's dead-end shapes stop their rails at the centre, so where a diagonal now joins
    // a dead end, its rails get continued past the centre to cross the diagonal ones
    private void AddCardinalOvershoot(MeshData mesh)
    {
        bool hasOvershoot = cardinalOvershootMeshes.Count > 0;
        if (!hasOvershoot) return;

        List<BlockFacing> connectedSides = BlockFacing.HORIZONTALS
            .Where(IsConnectedTowards)
            .ToList();

        // with two sides vanilla already uses its corner shape, which crosses its own rails
        bool isDeadEnd = connectedSides.Count == 1;
        if (!isDeadEnd) return;

        BlockFacing deadEndSide = connectedSides[0];
        mesh.AddMeshData(cardinalOvershootMeshes[deadEndSide]);
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
