using System.Numerics;

namespace OctreeCompression;

public class OctreeNode
{
    public OctreeBBox Bounds { get; }
    public int Depth { get; }
    private OctreeNode[]? _children;
    private IDictionary<OctreeCorner, Vector3?> _leafs = new Dictionary<OctreeCorner, Vector3?>(); //TODO make it array

    public OctreeNode(OctreeBBox bounds, int depth)
    {
        // 0x1000_0001;
        //     / \
        // 0x1000_0000 //0x0000_0000;    
        //     \
        // 0x0000_0000
        Bounds = bounds;
        Depth = depth;
    }


    public void AddPoint(Vector3 vector3)
    {
        var corner = Bounds.GetCorner(vector3);
        if (!_leafs.ContainsKey(corner))
        {
            _leafs.Add(corner, vector3);
            return;
        }

        // if division is not yet done, divide 
        if (_children == null)
        {
            Divide();
        }

        AddToChildren(vector3); // pass the vector down to children

        // if it was just split, we need to move the previously leaf node down to children as well
        if (_leafs[corner] != null)
        {
            AddToChildren(_leafs[corner]!.Value);
        }

        _leafs[corner] = null;
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
        //TODO loop
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
            if (_leafs.TryGetValue(corner, out var vector3) && vector3.HasValue)
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
                if (_children[i]._leafs.Count != 0)
                {
                    FlagsHelper.Set(ref childBits, (int)corner);
                }

                i++;
            }

            writer.Write(Convert.ToByte(childBits));

            foreach (var child in _children)
            {
                if (child._leafs.Count != 0)
                {
                    child.BinaryWrite(writer);
                }
            }
        }
    }

    public void GetApproximatedPoints(List<Vector3> list)
    {
        foreach (var leaf in _leafs)
        {
            if (leaf.Value != null)
            {
                var bbox = Bounds.GetOctant(leaf.Key);
                list.Add(bbox.GetCentroid());
            }
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
                _leafs[corner] = Bounds.GetOctant(corner).GetCentroid();
            }
            else
            {
                _leafs[corner] = null;
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