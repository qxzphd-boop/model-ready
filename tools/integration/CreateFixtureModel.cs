// #! csharp

using System;
using System.Drawing;
using System.IO;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

var repositoryRoot = System.Environment.CurrentDirectory;
var outputDirectory = Path.Combine(repositoryRoot, "samples", "generated");
var fixturePath = Path.Combine(outputDirectory, "modelready-v0.1-fixture.3dm");
var statusPath = Path.Combine(outputDirectory, "fixture-status.txt");
const string PassStatus = "PASS|supported_objects=5|excluded_point=1|excluded_block=1|units=Millimeters|tolerance_mm=0.01";

Directory.CreateDirectory(outputDirectory);

try
{
    if (File.Exists(fixturePath))
    {
        File.WriteAllText(statusPath, PassStatus);
        return;
    }

    using var document = RhinoDoc.CreateHeadless(null);
    document.ModelUnitSystem = UnitSystem.Millimeters;
    document.ModelAbsoluteTolerance = 0.01;

    var architectureLayerIndex = document.Layers.Add("Architecture", Color.Black);
    if (architectureLayerIndex < 0)
    {
        throw new InvalidOperationException("Could not create the Architecture layer.");
    }

    var box = Brep.CreateFromBox(new BoundingBox(
        new Point3d(0, 0, 0),
        new Point3d(1000, 1000, 1000)));
    AddBrep(document, box, architectureLayerIndex, ObjectMode.Normal);
    AddBrep(document, box.DuplicateBrep(), architectureLayerIndex, ObjectMode.Hidden);

    AddCurve(
        document,
        new LineCurve(new Point3d(2000, 0, 0), new Point3d(2000.5, 0, 0)),
        architectureLayerIndex,
        ObjectMode.Normal);
    AddCurve(
        document,
        new LineCurve(new Point3d(1_500_000, 0, 0), new Point3d(1_501_000, 0, 0)),
        architectureLayerIndex,
        ObjectMode.Locked);
    AddCurve(
        document,
        new LineCurve(new Point3d(0, 2000, 0), new Point3d(1000, 2000, 0)),
        0,
        ObjectMode.Normal);

    var unsupportedPointId = document.Objects.AddPoint(new Point3d(500, 500, 500));
    if (unsupportedPointId == Guid.Empty)
    {
        throw new InvalidOperationException("Could not add the unsupported point fixture.");
    }

    var blockDefinitionIndex = document.InstanceDefinitions.Add(
        "Excluded block",
        "Verifies that instance definitions and references are not scanned.",
        Point3d.Origin,
        new LineCurve(new Point3d(0, 0, 0), new Point3d(250, 0, 0)),
        Attributes(architectureLayerIndex, ObjectMode.Normal));
    if (blockDefinitionIndex < 0)
    {
        throw new InvalidOperationException("Could not add the excluded block definition.");
    }

    var instanceId = document.Objects.AddInstanceObject(
        blockDefinitionIndex,
        Transform.Translation(0, 3000, 0));
    if (instanceId == Guid.Empty)
    {
        throw new InvalidOperationException("Could not add the excluded block instance.");
    }

    using var writeOptions = new FileWriteOptions
    {
        FileVersion = 8,
        SuppressAllInput = true,
        SuppressDialogBoxes = true,
        UpdateDocumentPath = false,
    };

    if (!document.Write3dmFile(fixturePath, writeOptions))
    {
        throw new InvalidOperationException("Rhino could not write the deterministic fixture.");
    }

    File.WriteAllText(statusPath, PassStatus);
}
catch (Exception exception)
{
    File.WriteAllText(statusPath, $"FAIL|{exception.GetType().FullName}|{exception.Message}");
}

static void AddBrep(RhinoDoc document, Brep brep, int layerIndex, ObjectMode mode)
{
    var objectId = document.Objects.AddBrep(brep, Attributes(layerIndex, mode));
    if (objectId == Guid.Empty)
    {
        throw new InvalidOperationException("Could not add fixture Brep.");
    }
}

static void AddCurve(RhinoDoc document, Curve curve, int layerIndex, ObjectMode mode)
{
    var objectId = document.Objects.AddCurve(curve, Attributes(layerIndex, mode));
    if (objectId == Guid.Empty)
    {
        throw new InvalidOperationException("Could not add fixture curve.");
    }
}

static ObjectAttributes Attributes(int layerIndex, ObjectMode mode)
{
    return new ObjectAttributes
    {
        LayerIndex = layerIndex,
        Mode = mode,
    };
}
