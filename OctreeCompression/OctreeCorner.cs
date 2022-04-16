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