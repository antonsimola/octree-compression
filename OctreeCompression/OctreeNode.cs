using System.Numerics;

namespace OctreeCompression;

public class OctreeNode
{
    private readonly float? _minimumEdgeSize;
    private readonly float? _maximumDepth;
    private const object Tombstone = null!;
    private static readonly object Empty = new object();

    public OctreeBounds Bounds { get; }
    public int Depth { get; }
    private OctreeNode[]? _children;

    private object[] _leafs = new object[8] { Empty, Empty, Empty, Empty, Empty, Empty, Empty, Empty };

    public OctreeNode(OctreeBounds bounds, int depth, float? minimumEdgeSize = null, float? maximumDepth = null)
    {
        _minimumEdgeSize = minimumEdgeSize;
        _maximumDepth = maximumDepth;
        Bounds = bounds;
        Depth = depth;
    }

    internal bool IsLeafEmpty(OctreeCorner corner)
    {
        return _leafs[corner.ToArrayIndex()] == (object)Empty;
    }

    internal bool HasAnyLeaf()
    {
        return _leafs.Any(l => l != (object)Empty);
    }

    internal IEnumerable<(OctreeCorner, Vector3)> IterateNonEmptyLeafs()
    {
        return _leafs.Where(leaf => leaf != (object)Tombstone && leaf != (object)Empty).Select((leaf, i) =>
            (OctreeCornerExtensions.FromArrayIndex(i), (Vector3)leaf));
    }


    public void AddPoint(Vector3 vector3)
    {
        var corner = Bounds.GetCorner(vector3);
        var arrayIndex = corner.ToArrayIndex();

        if (_maximumDepth.HasValue && Depth > _maximumDepth)
        {
            _leafs[arrayIndex] = vector3;
            return;
        }

        if (IsLeafEmpty(corner) &&
            (!_minimumEdgeSize.HasValue || _minimumEdgeSize.Value > Bounds.GetMaxEdgeLength())
           )
        {
            _leafs[arrayIndex] = vector3;
            return;
        }

        // if division is not yet done, divide 
        if (_children == null)
        {
            Divide();
        }

        AddToChildren(vector3); // pass the vector down to children

        // if it was just split, need to move the leaf node  down to children as well
        if (!IsLeafEmpty(corner) && _leafs[arrayIndex] != (object)Tombstone)
        {
            AddToChildren((Vector3)_leafs[arrayIndex]);
        }

        _leafs[arrayIndex] = Tombstone;
    }

    private void AddToChildren(Vector3 vector3)
    {
        foreach (var octreeNode in _children)
        {
            if (octreeNode.Bounds.IsWithinBounds(vector3))
            {
                octreeNode.AddPoint(vector3);
                break;
            }
        }
    }

    private void Divide()
    {
        _children = new OctreeNode[8];
        var newDepth = Depth + 1;
        //unwrapped loop...
        _children[0] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopLeftFront), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[1] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopLeftBack), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[2] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopRightFront), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[3] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopRightBack), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[4] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomLeftFront), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[5] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomLeftBack), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[6] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomRightFront), newDepth, _minimumEdgeSize,
            _maximumDepth);
        _children[7] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomRightBack), newDepth, _minimumEdgeSize,
            _maximumDepth);
    }

    public void BinaryWrite(BinaryWriter writer)
    {
        BinaryWriteLeafs(writer);
        BinaryWriteChildren(writer);
    }

    private void BinaryWriteLeafs(BinaryWriter writer)
    {
        int leafBits = 0;

        for (var i = 0; i < 8; i++)
        {
            var corner = OctreeCornerExtensions.FromArrayIndex(i);
            if (!IsLeafEmpty(corner) && _leafs[i] != (object)Tombstone)
            {
                FlagsHelper.Set(ref leafBits, (byte)corner);
            }
        }

        writer.Write((byte)leafBits);
    }

    private void BinaryWriteChildren(BinaryWriter writer)
    {
        var childBits = 0;
        
        for (var i =0; i< 8; i++)
        {
            if (_children?[i].HasAnyLeaf() ?? false)
            {
                var corner = OctreeCornerExtensions.FromArrayIndex(i);
                FlagsHelper.Set(ref childBits, (int)corner);
            }
        }

        writer.Write(Convert.ToByte(childBits));

        foreach (var child in _children ?? Array.Empty<OctreeNode>())
        {
            if (child.HasAnyLeaf())
            {
                child.BinaryWrite(writer);
            }
        }
    }

    public void GetApproximatedPoints(List<Vector3> list)
    {
        foreach (var (corner, leaf) in IterateNonEmptyLeafs())
        {
            var bbox = Bounds.GetOctant(corner);
            list.Add(bbox.GetCentroid());
        }

        if (_children == null) return;

        foreach (var child in _children)
        {
            child.GetApproximatedPoints(list);
        }
    }

    public void ReadPointsFromBytes(BinaryReader br)
    {
        var leafByte = br.ReadByte();

        for (var i = 0; i < 8; i++)
        {
            var corner = OctreeCornerExtensions.FromArrayIndex(i);
            bool isLeaf = FlagsHelper.IsSet((int)leafByte, (int)corner);
            if (isLeaf)
            {
                _leafs[corner.ToArrayIndex()] = Bounds.GetOctant(corner).GetCentroid();
            }
            else
            {
                _leafs[corner.ToArrayIndex()] = Empty;
            }
        }

        var childByte = br.ReadByte();

        if (childByte > 0)
        {
            Divide();

            for (var i = 0; i < 8; i++)
            {
                var corner = OctreeCornerExtensions.FromArrayIndex(i);
                bool isChild = FlagsHelper.IsSet(childByte, (int)corner);
                if (isChild)
                {
                    _children[i].ReadPointsFromBytes(br);
                }
            }
        }
    }

    public int GetMaximumDepth()
    {
        if (_children == null) return Depth;
        return _children.Max(c => c.GetMaximumDepth());
    }
}