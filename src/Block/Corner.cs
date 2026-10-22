using Vintagestory.API.MathTools;

namespace DiagonalFences;

/// <summary>One of the four diagonal directions a fence can connect to.</summary>
public class Corner
{
    public static readonly Corner NorthEast = new(BlockFacing.NORTH, BlockFacing.EAST, armRotation: 0);
    public static readonly Corner SouthEast = new(BlockFacing.SOUTH, BlockFacing.EAST, armRotation: 270);
    public static readonly Corner SouthWest = new(BlockFacing.SOUTH, BlockFacing.WEST, armRotation: 180);
    public static readonly Corner NorthWest = new(BlockFacing.NORTH, BlockFacing.WEST, armRotation: 90);

    public static readonly Corner[] All = [NorthEast, SouthEast, SouthWest, NorthWest];

    // Vanilla fence posts and bars sit between these two lines on both axes
    private const float PostStart = 0.375f;
    private const float PostEnd = 0.625f;

    public BlockFacing NorthOrSouth { get; }
    public BlockFacing EastOrWest { get; }

    /// <summary>How far the north-east arm shape has to turn to point into this corner.</summary>
    public float ArmRotation { get; }

    private Corner(BlockFacing northOrSouth, BlockFacing eastOrWest, float armRotation)
    {
        NorthOrSouth = northOrSouth;
        EastOrWest = eastOrWest;
        ArmRotation = armRotation;
    }

    /// <summary>Short name like "ne" or "sw".</summary>
    public string Code => $"{NorthOrSouth.Code[0]}{EastOrWest.Code[0]}";

    public BlockPos NeighbourOf(BlockPos pos) => pos.AddCopy(NorthOrSouth).Add(EastOrWest, 1);

    /// <summary>
    /// A box filling this corner of the block. It touches the post and the neighbour's
    /// corner box only at a point, which is still enough to stop anyone walking through.
    /// </summary>
    public Cuboidf Box(float height)
    {
        bool east = EastOrWest == BlockFacing.EAST;
        bool south = NorthOrSouth == BlockFacing.SOUTH;

        return new Cuboidf(
            x1: east ? PostEnd : 0, y1: 0, z1: south ? PostEnd : 0,
            x2: east ? 1 : PostStart, y2: height, z2: south ? 1 : PostStart);
    }
}
