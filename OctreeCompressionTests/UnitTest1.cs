using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using OctreeCompression;

namespace OctreeCompressionTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SinglePoint()
    {
        var v =  new Vector3(1, 1, 1);
        var octree = new Octree(new List<Vector3>(){v});
        var bytes = octree.ToByteArray();

        var octreeFromBytes =  Octree.FromBytes(bytes);
        
        Assert.AreEqual(octree.GetApproximatedPoints(), octreeFromBytes.GetApproximatedPoints());
    }
    
    [Test]
    public void SimpleVector()
    {
        var v =  Enumerable.Range(0,101).Select(i => new Vector3((float)i / 100)).ToList();
        var octree = new Octree(v);
        var bytes = octree.ToByteArray();

        var octreeFromBytes =  Octree.FromBytes(bytes);
        
        Assert.AreEqual(octree.GetApproximatedPoints(), octreeFromBytes.GetApproximatedPoints());
    }

    [Test]
    public void Bunny()
    {
        var lines = File.ReadAllLines("bun_zipper.ply");
        var bunnyPointCloud = lines
            .Select(l => l.Split(" "))
            .Select(csv => new Vector3(float.Parse(csv[0], CultureInfo.InvariantCulture),
                float.Parse(csv[1], CultureInfo.InvariantCulture), float.Parse(csv[2], CultureInfo.InvariantCulture)))
            .ToList();

        var octree = new Octree(bunnyPointCloud);
        var bytes = octree.ToByteArray();

        var octreeFromBytes = Octree.FromBytes(bytes);

        Assert.AreEqual(octree.GetApproximatedPoints(), octreeFromBytes.GetApproximatedPoints());
    }
    
    [Test]
    public void BunnyWithMinimumResolution()
    {
        var lines = File.ReadAllLines("bun_zipper.ply");
        var bunnyPointCloud = lines
            .Select(l => l.Split(" "))
            .Select(csv => new Vector3(float.Parse(csv[0], CultureInfo.InvariantCulture),
                float.Parse(csv[1], CultureInfo.InvariantCulture), float.Parse(csv[2], CultureInfo.InvariantCulture)))
            .ToList();

        var octree = new Octree(bunnyPointCloud, 0.0000001f);
        var bytes = octree.ToByteArray();

        var octreeFromBytes = Octree.FromBytes(bytes);

        Assert.AreEqual(octree.GetApproximatedPoints(), octreeFromBytes.GetApproximatedPoints());

    }
    

}