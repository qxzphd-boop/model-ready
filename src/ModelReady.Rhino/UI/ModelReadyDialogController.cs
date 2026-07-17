using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ModelReady.Core;
using ModelReady.Rhino.Scanning;
using Rhino;

namespace ModelReady.Rhino.UI;

public sealed class ModelReadyDialogController
{
    private readonly RhinoDoc _document;
    private readonly IRhinoDocumentSnapshotFactory _snapshotFactory;
    private readonly PreflightRunner _runner;

    public ModelReadyDialogController(RhinoDoc document)
        : this(document, new RhinoDocumentSnapshotFactory(), new PreflightRunner())
    {
    }

    internal ModelReadyDialogController(
        RhinoDoc document,
        IRhinoDocumentSnapshotFactory snapshotFactory,
        PreflightRunner runner)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _snapshotFactory = snapshotFactory ?? throw new ArgumentNullException(nameof(snapshotFactory));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        Results = Array.Empty<RuleResultViewModel>();
        ExpectedUnitSystem = StudioSubmissionProfile.CreateDefault().ExpectedUnitSystem;
        ReadinessText = "NOT SCANNED";
        ElapsedText = "Run preflight to inspect the active model.";
        ErrorText = string.Empty;
    }

    public string ExpectedUnitSystem { get; set; }

    public string ReadinessText { get; private set; }

    public string ElapsedText { get; private set; }

    public string ErrorText { get; private set; }

    public IReadOnlyList<RuleResultViewModel> Results { get; private set; }

    public PreflightReport? LastReport { get; private set; }

    public bool IsRunning { get; private set; }

    public bool CanLocate => false;

    public bool CanExport => false;

    public PreflightReport RunPreflight()
    {
        if (string.IsNullOrWhiteSpace(ExpectedUnitSystem))
        {
            throw new InvalidOperationException("Expected units must be selected before scanning.");
        }

        IsRunning = true;
        ErrorText = string.Empty;

        try
        {
            var defaults = StudioSubmissionProfile.CreateDefault();
            var profile = new StudioSubmissionProfile(
                defaults.Name,
                ExpectedUnitSystem,
                defaults.MinToleranceMetres,
                defaults.MaxToleranceMetres,
                defaults.FarFromOriginMetres,
                defaults.TinyGeometryMetres);
            var snapshot = _snapshotFactory.Create(_document);
            var report = _runner.Run(snapshot, profile);
            var rows = new List<RuleResultViewModel>(report.Results.Count);
            foreach (var result in report.Results)
            {
                rows.Add(new RuleResultViewModel(result));
            }

            LastReport = report;
            Results = new ReadOnlyCollection<RuleResultViewModel>(rows);
            ReadinessText = ToReadinessText(report.Readiness);
            ElapsedText = $"Scanned {snapshot.Objects.Count} supported object(s) in {report.Elapsed.TotalMilliseconds:0} ms.";
            ErrorText = FormatRuleErrors(report);
            return report;
        }
        catch (Exception exception)
        {
            LastReport = null;
            Results = Array.Empty<RuleResultViewModel>();
            ReadinessText = "SCAN ERROR";
            ElapsedText = "The scan did not complete.";
            ErrorText = string.IsNullOrWhiteSpace(exception.Message)
                ? exception.GetType().Name
                : exception.Message;
            throw;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static string ToReadinessText(ReadinessStatus? readiness)
    {
        return readiness switch
        {
            ReadinessStatus.Ready => "READY",
            ReadinessStatus.ReadyWithWarnings => "READY WITH WARNINGS",
            ReadinessStatus.NotReady => "NOT READY",
            null => "SCAN ERROR",
            _ => throw new ArgumentOutOfRangeException(nameof(readiness), readiness, "Unknown readiness status."),
        };
    }

    private static string FormatRuleErrors(PreflightReport report)
    {
        if (report.Errors.Count == 0)
        {
            return string.Empty;
        }

        var messages = new List<string>(report.Errors.Count);
        foreach (var error in report.Errors)
        {
            messages.Add($"{error.RuleId}: {error.Message}");
        }

        return string.Join(Environment.NewLine, messages);
    }
}
