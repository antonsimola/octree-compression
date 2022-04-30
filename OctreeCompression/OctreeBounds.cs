using System.Numerics;

namespace OctreeCompression;

public record  OctreeBounds
{
    private Vector3? _centroid;
    public Vector3 From { get; }

    public Vector3 To { get; }

    private OctreeBounds?[]? _octants;
    
    public OctreeBounds(Vector3 from, Vector3 to)
    {
        From = Vector3.Min(from, to);
        To = Vector3.Max(from, to);
        TopLeftEdge = new Vector3(From.X, From.Y, To.Z) - From;
        TopRightEdge = new Vector3(To.X, From.Y, From.Z) - From;
        BottomLeftEdge = new Vector3(From.X, To.Y, From.Z) - From;
        _centroid = GetCentroid();
    }

    public Vector3 TopLeftEdge { get; }
    public Vector3 TopRightEdge { get; }

    public Vector3 BottomLeftEdge { get; }

    public float GetMinEdgeLength()
    {
        return
            Vector3.Min(Vector3.Min(TopLeftEdge, TopRightEdge), BottomLeftEdge).Length();
    }


    public float GetMaxEdgeLength()
    {
        return Math.Abs(Vector3.Max(Vector3.Max(TopLeftEdge, TopRightEdge), BottomLeftEdge).Length());
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
        if (_octants == null)
        {
            _octants = new OctreeBounds[8];
        }

        var arrIndex = corner.ToArrayIndex();
        var octant = _octants[arrIndex];
        if (octant != null)
        {
            return octant;
        }
        
         octant =  corner switch
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
         _octants[arrIndex] = octant;
         return octant;
    }

    public OctreeCorner GetCorner(Vector3 vector3)
    {
        for (var i = 0; i < 8; i++)
        {
            var corner = OctreeCornerExtensions.FromArrayIndex(i); 
            var octant = GetOctant(corner);
            if (octant.IsWithinBounds(vector3))
            {
                return corner;
            }
        }

        throw new Exception($"{vector3} is not within bounds {this} ");
    }
}