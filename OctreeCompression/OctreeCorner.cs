namespace OctreeCompression;

/// <summary>
/// https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.580.3453&rep=rep1&type=pdf
/// </summary>
[Flags]
public enum OctreeCorner
{
    TopLeftFront = 0b0000_0001,
    TopLeftBack = 0b0000_0010,
    TopRightFront = 0b0000_0100,
    TopRightBack = 0b0000_1000,
    BottomLeftFront = 0b0001_0000,
    BottomLeftBack = 0b0010_0000,
    BottomRightFront = 0b0100_0000,
    BottomRightBack = 0b1000_0000
}

public static class OctreeCornerExtensions
{
    public static int ToArrayIndex(this OctreeCorner corner)
    {
        return corner switch
        {
            OctreeCorner.TopLeftFront => 0,
            OctreeCorner.TopLeftBack => 1,
            OctreeCorner.TopRightFront => 2,
            OctreeCorner.TopRightBack => 3,
            OctreeCorner.BottomLeftFront => 4,
            OctreeCorner.BottomLeftBack => 5,
            OctreeCorner.BottomRightFront => 6,
            OctreeCorner.BottomRightBack => 7,
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
        };
    }

    public static OctreeCorner FromArrayIndex(int arrayIndex)
    {
        return arrayIndex switch
        {
            0 => OctreeCorner.TopLeftFront,
            1 => OctreeCorner.TopLeftBack,
            2 => OctreeCorner.TopRightFront,
            3 => OctreeCorner.TopRightBack,
            4 => OctreeCorner.BottomLeftFront,
            5 => OctreeCorner.BottomLeftBack,
            6 => OctreeCorner.BottomRightFront,
            7 => OctreeCorner.BottomRightBack,
            _ => throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, null)
        };
    }
}