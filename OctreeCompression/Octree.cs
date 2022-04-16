using System.Numerics;

namespace OctreeCompression;

// x1, y1, z1, x2, y2, z2 (floatx6)
// 00000001
//        ^ this node is leaf      
// 10001000
// ^   ^   these octanes have children in them

public class Octree
{
    private readonly OctreeBBox _initialBounds;

    private readonly OctreeNode _rootNode;

    public Octree(OctreeBBox initialBounds)
    {
        _initialBounds = initialBounds;
        _rootNode = new OctreeNode(initialBounds with { From = initialBounds.From, To = initialBounds.To }, 0);
    }

    public Octree(IList<Vector3> initialPoints)
    {
        _initialBounds = GetBoundsFromPoints(initialPoints);
        _rootNode = new OctreeNode(_initialBounds with { From = _initialBounds.From, To = _initialBounds.To }, 0);
        foreach (var p in initialPoints)
        {
            AddPoint(p);
        }
    }

    private OctreeBBox GetBoundsFromPoints(IList<Vector3> points)
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        var maxZ = float.MinValue;

        foreach (var initialPoint in points)
        {
            minX = Math.Min(minX, initialPoint.X);
            minY = Math.Min(minY, initialPoint.Y);
            minZ = Math.Min(minZ, initialPoint.Z);
            maxX = Math.Max(maxX, initialPoint.X);
            maxY = Math.Max(maxY, initialPoint.Y);
            maxZ = Math.Max(maxZ, initialPoint.Z);
        }

        return new OctreeBBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }

    public void AddPoint(Vector3 vector3)
    {
        if (!_initialBounds.IsWithinBounds(vector3))
        {
            throw new Exception("cannot expand initial bbox yet...");
        }

        _rootNode.AddPoint(vector3);
    }

    public byte[] ToByteArray()
    {
        using var ms = ToStream();

        return ms.ToArray();
    }

    public MemoryStream ToStream()
    {
        var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);

        WriteHeaders(bw);

        _rootNode.BinaryWrite(bw);
        
        return ms;
    }

    private void WriteHeaders(BinaryWriter bw)
    {
        bw.Write(_initialBounds.From.X);
        bw.Write(_initialBounds.From.Y);
        bw.Write(_initialBounds.From.Z);

        bw.Write(_initialBounds.To.X);
        bw.Write(_initialBounds.To.Y);
        bw.Write(_initialBounds.To.Z);
    }

    private static OctreeBBox ReadHeaders(BinaryReader br)
    {
        var x1 = br.ReadSingle();
        var y1 = br.ReadSingle();
        var z1 = br.ReadSingle();

        var x2 = br.ReadSingle();
        var y2 = br.ReadSingle();
        var z2 = br.ReadSingle();
        
        return new OctreeBBox(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2));
    }


    public IList<Vector3> GetApproximatedPoints()
    {
        var list = new List<Vector3>();
        _rootNode.GetApproximatedPoints(list);
        return list;
    }

    public static Octree FromBytes(byte[] encodedBytes)
    {
        var ms = new MemoryStream(encodedBytes);
        using var br = new BinaryReader(ms);
        var initialBBox = ReadHeaders(br);
        var octree = new Octree(initialBBox);
        octree.ReadPointsFromBytes(br);
        return octree;
    }

    internal void ReadPointsFromBytes(BinaryReader br)
    {
        _rootNode.ReadPointsFromBytes(br);
        
    }
}