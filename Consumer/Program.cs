// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using OctreeCompression;

var filePrefix = "wood_newformat";

var lines = File.ReadAllLines($"{filePrefix}.ply");
var bunnyPointCloud = lines
    .Select(l => l.Split(" "))
    .Select(csv => new Vector3(float.Parse(csv[0], CultureInfo.InvariantCulture),
        float.Parse(csv[1], CultureInfo.InvariantCulture), float.Parse(csv[2], CultureInfo.InvariantCulture))).ToList();



var ms = new MemoryStream();
using var bw = new BinaryWriter(ms);
foreach (var vector3 in bunnyPointCloud)
{
    bw.Write(vector3.X);
    bw.Write(vector3.Y);
    bw.Write(vector3.Z);
}


File.WriteAllBytes($"{filePrefix}raw_vectors.v3",ms.ToArray());

File.WriteAllText($"{filePrefix}original.ply", GetPly(bunnyPointCloud));

var compressionOptions = new float?[] { null};
foreach (var option in compressionOptions)
{
    var octTree = new Octree(bunnyPointCloud, option, 100);

    var bytes = octTree.ToByteArray();
    File.WriteAllBytes($"{filePrefix}_encoded_bytes{option}.ot",bytes);
    using var originalStream = new MemoryStream(bytes);
    using FileStream compressedFileStream = File.Create($"{filePrefix}_encoded_bytes{option}.ot.gz");
    using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
    originalStream.CopyTo(compressor);
    var encoded_bytes = File.ReadAllBytes($"{filePrefix}_encoded_bytes{option}.ot");

    Octree.FromBytes(encoded_bytes);
    var points = octTree.GetApproximatedPoints();
    File.WriteAllText($"{filePrefix}_compress_{option}.ply", GetPly(points));
}

Console.WriteLine("Done");


 string GetPly(IList<Vector3> points)
{
    var pointsAsString = string.Join("\n",points.Select(p =>
            $"{p.X.ToString(CultureInfo.InvariantCulture)} {p.Y.ToString(CultureInfo.InvariantCulture)} {p.Z.ToString(CultureInfo.InvariantCulture)}"))
        ;
    
return  $@"ply
format ascii 1.0
comment zipper output
element vertex {points.Count}
property float x
property float y
property float z
end_header
{pointsAsString}";
}