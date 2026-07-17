// #! csharp
// r "../../src/ModelReady.Core/bin/Release/netstandard2.0/ModelReady.Core.dll"

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModelReady.Core;
using Rhino;

var repositoryRoot = Environment.CurrentDirectory;
var fixturePath = Path.Combine(repositoryRoot, "samples", "generated", "modelready-v0.1-fixture.3dm");
var statusPath = Path.Combine(repositoryRoot, "samples", "generated", "snapshot-smoke-status.txt");
var plugInPath = Path.Combine(repositoryRoot, "src", "ModelReady.Rhino", "bin", "Release", "net7.0", "ModelReady.rhp");

try
{
    if (!File.Exists(fixturePath))
    {
        throw new FileNotFoundException("Fixture has not been generated.", fixturePath);
    }

    using var document = RhinoDoc.OpenHeadless(fixturePath);
    var plugInAssembly = Assembly.LoadFrom(plugInPath);
    var factoryType = plugInAssembly.GetType(
        "ModelReady.Rhino.Scanning.RhinoDocumentSnapshotFactory",
        throwOnError: true)!;
    var factory = Activator.CreateInstance(factoryType)
        ?? throw new InvalidOperationException("Could not create the Rhino snapshot factory.");
    var createMethod = factoryType.GetMethod("Create", new[] { typeof(RhinoDoc) })
        ?? throw new MissingMethodException(factoryType.FullName, "Create(RhinoDoc)");
    var snapshot = createMethod.Invoke(factory, new object[] { document }) as DocumentSnapshot
        ?? throw new InvalidOperationException("The factory did not return a DocumentSnapshot.");

    AssertEqual("Millimeters", snapshot.UnitSystem, "unit system");
    AssertNear(0.00001, snapshot.AbsoluteToleranceMetres, 1e-12, "metre-normalised tolerance");
    AssertEqual(5, snapshot.Objects.Count, "supported object count");
    AssertEqual(2, snapshot.Objects.Count(item => item.DuplicateSetId is not null), "duplicate member count");
    AssertEqual(1, snapshot.Objects.Where(item => item.DuplicateSetId is not null).Select(item => item.DuplicateSetId).Distinct().Count(), "duplicate group count");
    AssertEqual(1, snapshot.Objects.Count(item => item.BoundingBoxDiagonalMetres < 0.001), "tiny object count");
    AssertEqual(1, snapshot.Objects.Count(item => item.BoundingBoxCentreDistanceMetres > 1000.0), "far object count");
    AssertEqual(1, snapshot.Objects.Count(item => item.IsOnDefaultLayer), "Default-layer object count");

    var report = new PreflightRunner().Run(snapshot, StudioSubmissionProfile.CreateDefault());
    AssertEqual(7, report.Results.Count, "rule result count");
    AssertEqual(0, report.Errors.Count, "rule execution error count");
    AssertEqual(ReadinessStatus.ReadyWithWarnings, report.Readiness, "readiness");

    File.WriteAllText(
        statusPath,
        "PASS|supported_objects=5|duplicate_groups=1|tiny=1|far=1|default_layer=1|readiness=ReadyWithWarnings");
}
catch (Exception exception)
{
    var rootCause = exception is TargetInvocationException { InnerException: not null }
        ? exception.InnerException
        : exception;
    File.WriteAllText(statusPath, $"FAIL|{rootCause.GetType().FullName}|{rootCause.Message}");
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {label} '{expected}', received '{actual}'.");
    }
}

static void AssertNear(double expected, double actual, double tolerance, string label)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"Expected {label} '{expected:G17}', received '{actual:G17}'.");
    }
}
