// #! csharp
// r "../../src/ModelReady.Core/bin/Release/netstandard2.0/ModelReady.Core.dll"

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using ModelReady.Core;
using Rhino;

var repositoryRoot = Environment.CurrentDirectory;
var fixturePath = Path.Combine(repositoryRoot, "samples", "generated", "modelready-v0.1-fixture.3dm");
var statusPath = Path.Combine(repositoryRoot, "samples", "generated", "dialog-smoke-status.txt");
var plugInPath = Path.Combine(repositoryRoot, "src", "ModelReady.Rhino", "bin", "Release", "net7.0", "ModelReady.rhp");

try
{
    using var document = RhinoDoc.OpenHeadless(fixturePath);
    var plugInAssembly = Assembly.LoadFrom(plugInPath);
    var controllerType = plugInAssembly.GetType(
        "ModelReady.Rhino.UI.ModelReadyDialogController",
        throwOnError: true)!;
    var controller = Activator.CreateInstance(controllerType, document)
        ?? throw new InvalidOperationException("Could not create the dialog controller.");

    SetProperty(controllerType, controller, "ExpectedUnitSystem", "Millimeters");
    var firstReport = InvokeReport(controllerType, controller);
    AssertEqual(ReadinessStatus.ReadyWithWarnings, firstReport.Readiness, "first readiness");
    AssertEqual("READY WITH WARNINGS", GetProperty<string>(controllerType, controller, "ReadinessText"), "first readiness text");
    AssertEqual(7, GetCollectionCount(controllerType, controller, "Results"), "first result count");
    var firstRow = GetFirstItem(controllerType, controller, "Results");
    AssertEqual("MR-DOC-001", GetProperty<string>(firstRow.GetType(), firstRow, "RuleId"), "first rule ID");
    AssertEqual("PASS", GetProperty<string>(firstRow.GetType(), firstRow, "StatusText"), "first rule status");
    AssertEqual(false, GetProperty<bool>(controllerType, controller, "CanLocate"), "Locate state");
    AssertEqual(false, GetProperty<bool>(controllerType, controller, "CanExport"), "Export state");

    SetProperty(controllerType, controller, "ExpectedUnitSystem", "Meters");
    var secondReport = InvokeReport(controllerType, controller);
    AssertEqual(ReadinessStatus.NotReady, secondReport.Readiness, "second readiness");
    AssertEqual("NOT READY", GetProperty<string>(controllerType, controller, "ReadinessText"), "second readiness text");
    AssertEqual(7, GetCollectionCount(controllerType, controller, "Results"), "second result count");

    var dialogType = plugInAssembly.GetType(
        "ModelReady.Rhino.UI.ModelReadyDialog",
        throwOnError: true)!;
    using var dialog = Activator.CreateInstance(dialogType, document) as IDisposable
        ?? throw new InvalidOperationException("Could not create the Eto dialog.");
    var defaultButton = dialogType.GetProperty("DefaultButton")?.GetValue(dialog);
    AssertEqual<object?>(null, defaultButton, "dialog default button");

    File.WriteAllText(
        statusPath,
        "PASS|first=ReadyWithWarnings|second=NotReady|rules=7|locate=false|export=false|dialog=created|default_button=none");
}
catch (Exception exception)
{
    var rootCause = Unwrap(exception);
    File.WriteAllText(statusPath, $"FAIL|{rootCause.GetType().FullName}|{rootCause.Message}");
}

static PreflightReport InvokeReport(Type controllerType, object controller)
{
    var method = controllerType.GetMethod("RunPreflight", Type.EmptyTypes)
        ?? throw new MissingMethodException(controllerType.FullName, "RunPreflight()");
    return method.Invoke(controller, Array.Empty<object>()) as PreflightReport
        ?? throw new InvalidOperationException("RunPreflight did not return a PreflightReport.");
}

static T GetProperty<T>(Type type, object instance, string propertyName)
{
    var property = type.GetProperty(propertyName)
        ?? throw new MissingMemberException(type.FullName, propertyName);
    var value = property.GetValue(instance);
    return value is T typed
        ? typed
        : throw new InvalidOperationException($"Property {propertyName} did not return {typeof(T).Name}.");
}

static int GetCollectionCount(Type type, object instance, string propertyName)
{
    var value = type.GetProperty(propertyName)?.GetValue(instance) as ICollection
        ?? throw new InvalidOperationException($"Property {propertyName} is not a collection.");
    return value.Count;
}

static object GetFirstItem(Type type, object instance, string propertyName)
{
    var value = type.GetProperty(propertyName)?.GetValue(instance) as IList
        ?? throw new InvalidOperationException($"Property {propertyName} is not an indexed collection.");
    return value.Count > 0
        ? value[0] ?? throw new InvalidOperationException($"Property {propertyName} contains a null first item.")
        : throw new InvalidOperationException($"Property {propertyName} is empty.");
}

static void SetProperty(Type type, object instance, string propertyName, object value)
{
    var property = type.GetProperty(propertyName)
        ?? throw new MissingMemberException(type.FullName, propertyName);
    property.SetValue(instance, value);
}

static Exception Unwrap(Exception exception)
{
    while (exception is TargetInvocationException { InnerException: not null })
    {
        exception = exception.InnerException;
    }

    return exception;
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {label} '{expected}', received '{actual}'.");
    }
}
