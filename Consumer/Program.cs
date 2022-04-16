// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Numerics;
using OctreeCompression;


var lines = File.ReadAllLines("bun_zipper.ply");
var bunnyPointCloud = lines
    .Take(35947)
    .Select(l => l.Split(" "))
    .Select(csv => new Vector3(float.Parse(csv[0], CultureInfo.InvariantCulture),
        float.Parse(csv[1], CultureInfo.InvariantCulture), float.Parse(csv[2], CultureInfo.InvariantCulture))).ToList();


var octTree = new Octree(bunnyPointCloud);

var bytes = octTree.ToByteArray();

var ms = new MemoryStream();
using var bw = new BinaryWriter(ms);
foreach (var vector3 in bunnyPointCloud)
{
    bw.Write(vector3.X);
    bw.Write(vector3.Y);
    bw.Write(vector3.Z);
}
File.WriteAllBytes("raw_vectors.v3",ms.ToArray());
File.WriteAllBytes("encoded_bytes.ot",bytes);
var encoded_bytes = File.ReadAllBytes("encoded_bytes.ot");

Octree.FromBytes(encoded_bytes);

var points = octTree.GetApproximatedPoints();
var pointsAsString = string.Join("\n",points.Select(p =>
    $"{p.X.ToString(CultureInfo.InvariantCulture)} {p.Y.ToString(CultureInfo.InvariantCulture)} {p.Z.ToString(CultureInfo.InvariantCulture)}"))
    ;

var contents = $@"ply
format ascii 1.0
comment zipper output
element vertex {points.Count}
property float x
property float y
property float z
property float confidence
property float intensity
element face 69451
property list uchar int vertex_indices
end_header
{pointsAsString}";
File.WriteAllText("bunny_compress.ply", contents);

Console.WriteLine("Done");