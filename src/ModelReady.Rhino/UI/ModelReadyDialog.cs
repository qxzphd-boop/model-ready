using System;
using Eto.Drawing;
using Eto.Forms;
using Rhino;

namespace ModelReady.Rhino.UI;

public sealed class ModelReadyDialog : Dialog
{
    private static readonly string[] ExpectedUnitSystems =
    {
        "Millimeters",
        "Centimeters",
        "Meters",
        "Inches",
        "Feet",
    };

    private readonly ModelReadyDialogController _controller;
    private readonly DropDown _expectedUnitsDropDown;
    private readonly Button _runButton;
    private readonly Label _readinessLabel;
    private readonly Label _elapsedLabel;
    private readonly Label _errorLabel;
    private readonly GridView _resultsGrid;
    private readonly Button _locateButton;
    private readonly Button _exportButton;

    public ModelReadyDialog(RhinoDoc document)
    {
        _controller = new ModelReadyDialogController(document);

        Title = "ModelReady — Studio Submission";
        ClientSize = new Size(920, 500);
        MinimumSize = new Size(760, 420);
        Padding = new Padding(16);
        Resizable = true;

        _expectedUnitsDropDown = new DropDown
        {
            DataStore = ExpectedUnitSystems,
            SelectedIndex = 0,
            Width = 180,
        };
        _runButton = new Button { Text = "Run Preflight", Width = 120 };
        _readinessLabel = new Label { Text = _controller.ReadinessText };
        _elapsedLabel = new Label { Text = _controller.ElapsedText };
        _errorLabel = new Label
        {
            Text = string.Empty,
            TextColor = Color.FromArgb(170, 0, 0),
            Wrap = WrapMode.Word,
        };
        _resultsGrid = CreateResultsGrid();
        _locateButton = new Button { Text = "Locate objects", Enabled = false };
        _exportButton = new Button { Text = "Export", Enabled = false };
        var closeButton = new Button { Text = "Close" };

        _runButton.Click += OnRunPreflight;
        closeButton.Click += (_, _) => Close();
        AbortButton = closeButton;

        var fieldLabelWidth = 140;
        var headerLayout = new DynamicLayout
        {
            Spacing = new Size(8, 8),
        };
        headerLayout.AddRow(
            new Label { Text = "Profile", Width = fieldLabelWidth },
            new Label { Text = "Studio Submission", Width = 180 },
            null);
        headerLayout.AddRow(
            new Label { Text = "Expected units", Width = fieldLabelWidth },
            _expectedUnitsDropDown,
            _runButton,
            null);

        var layout = new DynamicLayout
        {
            Spacing = new Size(8, 8),
        };
        layout.Add(headerLayout);
        layout.AddRow(_readinessLabel);
        layout.AddRow(_elapsedLabel);
        layout.AddRow(_errorLabel);
        layout.Add(_resultsGrid, yscale: true);
        layout.AddSeparateRow(null, _locateButton, _exportButton, closeButton);
        Content = layout;
    }

    private static GridView CreateResultsGrid()
    {
        var grid = new GridView
        {
            AllowMultipleSelection = false,
            ShowHeader = true,
            DataStore = Array.Empty<RuleResultViewModel>(),
        };
        grid.Columns.Add(new GridColumn
        {
            HeaderText = "Status",
            DataCell = new TextBoxCell(nameof(RuleResultViewModel.StatusText)),
            Width = 90,
        });
        grid.Columns.Add(new GridColumn
        {
            HeaderText = "Rule",
            DataCell = new TextBoxCell(nameof(RuleResultViewModel.RuleId)),
            Width = 105,
        });
        grid.Columns.Add(new GridColumn
        {
            HeaderText = "Check",
            DataCell = new TextBoxCell(nameof(RuleResultViewModel.Title)),
            Width = 170,
        });
        grid.Columns.Add(new GridColumn
        {
            HeaderText = "Objects",
            DataCell = new TextBoxCell(nameof(RuleResultViewModel.AffectedObjectCount)),
            Width = 70,
        });
        grid.Columns.Add(new GridColumn
        {
            HeaderText = "Result",
            DataCell = new TextBoxCell(nameof(RuleResultViewModel.Message)),
            AutoSize = true,
        });
        return grid;
    }

    private void OnRunPreflight(object? sender, EventArgs eventArgs)
    {
        _controller.ExpectedUnitSystem = _expectedUnitsDropDown.SelectedValue?.ToString() ?? string.Empty;
        _runButton.Enabled = false;
        _expectedUnitsDropDown.Enabled = false;
        _readinessLabel.Text = "SCANNING";
        _readinessLabel.TextColor = Color.FromArgb(40, 80, 140);
        _elapsedLabel.Text = "Inspecting the active Rhino model…";
        _errorLabel.Text = string.Empty;
        Application.Instance.AsyncInvoke(ExecuteScan);
    }

    private void ExecuteScan()
    {
        try
        {
            _controller.RunPreflight();
        }
        catch (Exception exception)
        {
            RhinoApp.WriteLine($"ModelReady scan failed: {exception.Message}");
        }
        finally
        {
            _readinessLabel.Text = _controller.ReadinessText;
            _readinessLabel.TextColor = GetReadinessColor(_controller.ReadinessText);
            _elapsedLabel.Text = _controller.ElapsedText;
            _errorLabel.Text = _controller.ErrorText;
            _resultsGrid.DataStore = _controller.Results;
            _locateButton.Enabled = _controller.CanLocate;
            _exportButton.Enabled = _controller.CanExport;
            _expectedUnitsDropDown.Enabled = true;
            _runButton.Enabled = true;
        }
    }

    private static Color GetReadinessColor(string readinessText)
    {
        return readinessText switch
        {
            "READY" => Color.FromArgb(25, 115, 65),
            "READY WITH WARNINGS" => Color.FromArgb(155, 95, 0),
            "NOT READY" => Color.FromArgb(175, 30, 30),
            "SCAN ERROR" => Color.FromArgb(175, 30, 30),
            _ => Color.FromArgb(45, 45, 45),
        };
    }
}
