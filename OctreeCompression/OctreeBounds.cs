using System.Numerics;

namespace OctreeCompression;

public record OctreeBounds
{
    private  Vector3? _centroid;
    public Vector3 From { get; set; }

    public Vector3 To { get; set; }

    public OctreeBounds(Vector3 from, Vector3 to)
    {
        From = Vector3.Min(from, to);
        To = Vector3.Max(from, to);
        _centroid = GetCentroid();
    }

    public Vector3 TopLeftBackVector => new Vector3(From.X, From.Y, To.Z);
    public Vector3 TopRightFrontVector => new Vector3(To.X, From.Y, From.Z);

    public Vector3 TopRightBackVector => new Vector3(To.X, From.Y, To.Z);

    public Vector3 BottomLeftFrontVector => new Vector3(From.X, To.Y, From.Z);
    
    public float GetMinEdgeLength()
    {
        return Vector3.Min(Vector3.Min(TopLeftBackVector, TopRightFrontVector), BottomLeftFrontVector).Length();
    }
    
    
    public float GetMaxEdgeLength()
    {
        return Vector3.Max(Vector3.Max(TopLeftBackVector, TopRightFrontVector), BottomLeftFrontVector).Length();
    }

    public bool IsWithinBounds(Vector3 vector3Like)
    {
        return From.X <= vector3Like.X && vector3Like.X <= To.X &&
               From.Y <= vector3Like.Y && vector3Like.Y <= To.Y &&
               From.Z <= vector3Like.Z && vector3Like.Z <= To.Z;
    }

    public Vector3 GetCentroid()
    {
        if (_centroid == null)
        {
            _centroid = 
                new Vector3(
                    From.X + (To.X - From.X) / 2,
                    From.Y + (To.Y - From.Y) / 2,
                    From.Z + (To.Z - From.Z) / 2
                );            
        }

        return _centroid.Value;

    }

    public OctreeBounds GetOctant(OctreeCorner corner)
    {
        return corner switch
        {
            OctreeCorner.TopLeftFront => new OctreeBounds(From, GetCentroid()),
            OctreeCorner.TopLeftBack => new OctreeBounds(new Vector3(From.X, From.Y, To.Z), GetCentroid()),
            OctreeCorner.TopRightFront => new OctreeBounds(new Vector3(To.X, From.Y, From.Z), GetCentroid()),
            OctreeCorner.TopRightBack => new OctreeBounds(new Vector3(To.X, From.Y, To.Z), GetCentroid()),
            OctreeCorner.BottomLeftFront => new OctreeBounds(new Vector3(From.X, To.Y, From.Z), GetCentroid()),
            OctreeCorner.BottomLeftBack => new OctreeBounds(new Vector3(From.X, To.Y, To.Z), GetCentroid()),
            OctreeCorner.BottomRightFront => new OctreeBounds(new Vector3(To.X, To.Y, From.Z), GetCentroid()),
            OctreeCorner.BottomRightBack => new OctreeBounds(To, GetCentroid()),
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