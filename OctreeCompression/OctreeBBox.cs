using System.Numerics;

namespace OctreeCompression;

public record OctreeBBox
{
    public Vector3 From { get; set; }
    
    public Vector3 To { get; set; }

    public OctreeBBox(Vector3 from, Vector3 to)
    {
        From = Vector3.Min(from, to);
        To = Vector3.Max(from, to);
    }
    public bool IsWithinBounds(Vector3 vector3Like)
    {

        return From.X <= vector3Like.X && vector3Like.X <= To.X &&
               From.Y <= vector3Like.Y && vector3Like.Y <= To.Y &&
               From.Z <= vector3Like.Z && vector3Like.Z <= To.Z;
    }

    public Vector3 GetCentroid()
    {
        return
            new Vector3(
                From.X + (To.X - From.X) / 2,
                From.Y + (To.Y - From.Y) / 2,
                From.Z + (To.Z - From.Z) / 2
            );
    }

    public OctreeBBox GetOctant(OctreeCorner corner)
    {
        return corner switch
        {
            OctreeCorner.TopLeftFront => new OctreeBBox(From, GetCentroid()),
            OctreeCorner.TopLeftBack => new OctreeBBox(new Vector3(From.X, From.Y, To.Z), GetCentroid()),
            OctreeCorner.TopRightFront => new OctreeBBox(new Vector3(To.X, From.Y, From.Z), GetCentroid()),
            OctreeCorner.TopRightBack => new OctreeBBox(new Vector3(To.X, From.Y, To.Z), GetCentroid()),
            OctreeCorner.BottomLeftFront => new OctreeBBox(new Vector3(From.X, To.Y, From.Z), GetCentroid()),
            OctreeCorner.BottomLeftBack => new OctreeBBox(new Vector3(From.X, To.Y, To.Z), GetCentroid()),
            OctreeCorner.BottomRightFront => new OctreeBBox(new Vector3(To.X, To.Y, From.Z), GetCentroid()),
            OctreeCorner.BottomRightBack => new OctreeBBox(To, GetCentroid()),
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null)
        };
    }
    public OctreeCorner GetCorner(Vector3 vector3)
    {
        foreach (var corner in Enum.GetValues<OctreeCorner>())
        {
            var octant = GetOctant(corner);
            if (octant.IsWithinBounds(vector3))
            {
                return corner;
            }
        }

        throw new Exception("not within bounds");
    }
}