using System.Collections.Generic;
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
    public void TestSingleVector()
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
}