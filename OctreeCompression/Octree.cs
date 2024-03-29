﻿using System.Numerics;

namespace OctreeCompression;

// x1, y1, z1, x2, y2, z2 (floatx6)
// 00000001
//        ^ this node is leaf      
// 10001000
// ^   ^   these octanes have children in them

public class Octree
{
    private readonly OctreeBounds _initialBounds;
    private readonly float? _minimumEdgeSize;
    private readonly int? _maximumDepth;

    private readonly OctreeNode _rootNode;

    /// <summary>
    /// Construct by providing known initial bounds (cannot be changed afterwards). Call AddPoint after!
    /// </summary>
    /// <param name="initialBounds"> initial bounds that cover the to-be-added points entirely</param>
    /// <param name="minimumEdgeSize">optional parameter to set the minimum sub-cube edge size</param>
    /// <param name="maximumDepth">optional parameter to set the maximum recursion depth</param>
    public Octree(OctreeBounds initialBounds, float? minimumEdgeSize = null, int? maximumDepth = null)
    {
        _initialBounds = initialBounds;
        _minimumEdgeSize = minimumEdgeSize;
        _maximumDepth = maximumDepth;
        _rootNode = new OctreeNode(new OctreeBounds(initialBounds.From, initialBounds.To), 0,
            minimumEdgeSize, maximumDepth);
    }

    /// <summary>
    /// Construct by providing known initial bounds (cannot be changed afterwards). Call AddPoint after!
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="z1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="z2"></param>
    /// <param name="minimumEdgeSize">optional parameter to set the minimum sub-cube edge size</param>
    /// <param name="maximumDepth">optional parameter to set the maximum recursion depth</param>
    public Octree(float x1, float y1, float z1, float x2, float y2, float z2, float? minimumEdgeSize = null,
        int? maximumDepth = null)
    {
        _minimumEdgeSize = minimumEdgeSize;
        _maximumDepth = maximumDepth;
        _initialBounds = new OctreeBounds(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2));
        _rootNode = new OctreeNode(new OctreeBounds(_initialBounds.From, _initialBounds.To), 0,
            minimumEdgeSize, maximumDepth);
    }

    /// <summary>
    /// Construct by providing known initial points.
    /// Initial bounds is set from given initial points and cannot changed afterwards.
    /// AddPoint can be called later, but those points must be within initial bounds. 
    /// </summary>
    /// <param name="initialPoints"></param>
    /// <param name="minimumEdgeSize">optional parameter to set the minimum sub-cube edge size (minimum resolution)</param>
    /// <param name="maximumDepth">optional parameter to set the maximum recursion depth</param>
    public Octree(IList<Vector3> initialPoints, float? minimumEdgeSize = null, int? maximumDepth = null)
    {
        _minimumEdgeSize = minimumEdgeSize;
        _maximumDepth = maximumDepth;
        _initialBounds = GetBoundsFromPoints(initialPoints);
        _rootNode = new OctreeNode(new OctreeBounds(_initialBounds.From, _initialBounds.To), 0,
            minimumEdgeSize, maximumDepth);
        foreach (var p in initialPoints)
        {
            AddPoint(p);
        }
    }

    /// <summary>
    /// Construct by deserializing from bytes (bytes that were generated by calling ToByteArray).
    /// </summary>
    public Octree(byte[] fromBytes)
    {
        var ms = new MemoryStream(fromBytes);
        using var br = new BinaryReader(ms);
        _initialBounds = ReadHeaders(br);
        _rootNode = new OctreeNode(new OctreeBounds(_initialBounds.From, _initialBounds.To), 0);
        ReadPointsFromBytes(br);
    }

    private OctreeBounds GetBoundsFromPoints(IList<Vector3> points)
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        var maxZ = float.MinValue;

        foreach (var point in points)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            minZ = Math.Min(minZ, point.Z);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
            maxZ = Math.Max(maxZ, point.Z);
        }

        return new OctreeBounds(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }

    public void AddPoint(Vector3 vector3)
    {
        if (!_initialBounds.IsWithinBounds(vector3))
        {
            throw new Exception(
                $"vector {vector3} is not within initial bounds {_initialBounds}, and expanding bounds is not supported.");
        }

        _rootNode.AddPoint(vector3);
    }

    public void AddPoints(params Vector3[] vector3s)
    {
        foreach (var vector3 in vector3s)
        {
            AddPoint(vector3);
        }
    }
    
    public void AddPoints(IEnumerable<Vector3> vector3s)
    {
        foreach (var vector3 in vector3s)
        {
            AddPoint(vector3);
        }
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

    public void ToStream(Stream stream)
    {
        var bw = new BinaryWriter(stream);

        WriteHeaders(bw);

        _rootNode.BinaryWrite(bw);
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

    private static OctreeBounds ReadHeaders(BinaryReader br)
    {
        var x1 = br.ReadSingle();
        var y1 = br.ReadSingle();
        var z1 = br.ReadSingle();

        var x2 = br.ReadSingle();
        var y2 = br.ReadSingle();
        var z2 = br.ReadSingle();

        return new OctreeBounds(new Vector3(x1, y1, z1), new Vector3(x2, y2, z2));
    }


    public IList<Vector3> GetApproximatedPoints()
    {
        var list = new List<Vector3>();
        _rootNode.GetApproximatedPoints(list);
        return list;
    }

    public static Octree FromBytes(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
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

    public int GetMaximumDepth()
    {
        return _rootNode.GetMaximumDepth();
    }
}