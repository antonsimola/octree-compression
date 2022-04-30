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
        BinaryWriteChildren(writer);
    }
    private void BinaryWriteChildren(BinaryWriter writer)
    {
        var childBits = 0;

        for (var i = 0; i < 8; i++)
        {
            if ((_leafs[i] != (object) Tombstone && _leafs[i] != Empty) ||
                (_children?[i].HasAnyLeaf() ?? false))   
            {
                var corner = OctreeCornerExtensions.FromArrayIndex(i);
                FlagsHelper.Set(ref childBits, (int)corner);
            }
        }

        writer.Write(Convert.ToByte(childBits));

       
        for (var i = 0; i < 8; i++)
        {
            var isLeaf = _leafs[i] != (object)Tombstone && _leafs[i] != Empty;

            if (isLeaf)
            {
                writer.Write((byte) 0);
            }

            else if (_children?[i]?.HasAnyLeaf() ?? false)
            {
                _children[i].BinaryWrite(writer);
            }
        }
        
    }

    public void GetApproximatedPoints(List<Vector3> list)
    {
        var i = 0;
        foreach (var leaf in _leafs)
        {
            if (leaf != Tombstone && leaf != Empty)
            {
                var bbox = Bounds.GetOctant(OctreeCornerExtensions.FromArrayIndex(i));
                list.Add(bbox.GetCentroid());    
            }
            i++;
        }

        if (_children == null) return;

        foreach (var child in _children)
        {
            child.GetApproximatedPoints(list);
        }
    }
    
    
    public bool ReadPointsFromBytes(BinaryReader br)
    {
        var childByte = br.ReadByte();
        if (childByte == 0)
        {
            return true;
        }

        Divide();

        for (var i = 0; i < 8; i++)
        {
            var corner = OctreeCornerExtensions.FromArrayIndex(i);
            bool isChild = FlagsHelper.IsSet(childByte, (int)corner);
            if (isChild)
            {
                var wasLeaf =  _children[i].ReadPointsFromBytes(br);
                if (wasLeaf)
                {
                    _leafs[i] = Bounds.GetOctant(corner).GetCentroid();
                }
                else
                {
                    _leafs[i] = Empty;
                }
            }
        }


        return false;
    }

    public int GetMaximumDepth()
    {
        if (_children == null) return Depth;
        return _children.Max(c => c.GetMaximumDepth());
    }
}