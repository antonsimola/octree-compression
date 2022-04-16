using System.Numerics;

namespace OctreeCompression;

public class OctreeNode
{
    private const object Tombstone = null!;
    private static readonly object Empty = new object();

    public OctreeBounds Bounds { get; }
    public int Depth { get; }
    private OctreeNode[]? _children;

    private object[] _leafs = new object[8] { Empty, Empty, Empty, Empty, Empty, Empty, Empty, Empty };

    public OctreeNode(OctreeBounds bounds, int depth)
    {
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
        if (IsLeafEmpty(corner))
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
        if (_leafs[arrayIndex] != (object)Tombstone)
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
        _children[0] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopLeftFront), newDepth);
        _children[1] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopLeftBack), newDepth);
        _children[2] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopRightFront), newDepth);
        _children[3] = new OctreeNode(Bounds.GetOctant(OctreeCorner.TopRightBack), newDepth);
        _children[4] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomLeftFront), newDepth);
        _children[5] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomLeftBack), newDepth);
        _children[6] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomRightFront), newDepth);
        _children[7] = new OctreeNode(Bounds.GetOctant(OctreeCorner.BottomRightBack), newDepth);
    }

    public void BinaryWrite(BinaryWriter writer)
    {
        int leafBits = 0;

        foreach (var corner in Enum.GetValues<OctreeCorner>())
        {
            if (!IsLeafEmpty(corner) && _leafs[corner.ToArrayIndex()] != (object)Tombstone)
            {
                FlagsHelper.Set(ref leafBits, (byte)corner);
            }
            else
            {
                FlagsHelper.Unset(ref leafBits, (byte)corner);
            }
        }

        writer.Write((byte)leafBits);

        if (_children == null)
        {
            writer.Write((byte)0);
        }
        else
        {
            var childBits = 0;
            var i = 0;
            foreach (var corner in Enum.GetValues<OctreeCorner>())
            {
                //TOD _leafs is length = 0 if there is nothing interesting in the whole node
                if (_children[i].HasAnyLeaf())
                {
                    FlagsHelper.Set(ref childBits, (int)corner);
                }

                i++;
            }

            writer.Write(Convert.ToByte(childBits));

            foreach (var child in _children)
            {
                if (child.HasAnyLeaf())
                {
                    child.BinaryWrite(writer);
                }
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

        foreach (var corner in Enum.GetValues<OctreeCorner>())
        {
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
            var i = 0;

            foreach (var corner in Enum.GetValues<OctreeCorner>())
            {
                bool isChild = FlagsHelper.IsSet(childByte, (int)corner);
                if (isChild)
                {
                    _children[i].ReadPointsFromBytes(br);
                }

                i++;
            }
        }
    }
}