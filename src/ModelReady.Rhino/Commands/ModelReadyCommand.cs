using Rhino;
using Rhino.Commands;
using Rhino.UI;
using ModelReady.Rhino.UI;

namespace ModelReady.Rhino.Commands;

public sealed class ModelReadyCommand : Command
{
    public override string EnglishName => "ModelReady";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        try
        {
            using var dialog = new ModelReadyDialog(doc);
            dialog.ShowModal(RhinoEtoApp.MainWindowForDocument(doc));
            return Result.Success;
        }
        catch (System.Exception exception)
        {
            RhinoApp.WriteLine($"ModelReady could not open: {exception.Message}");
            return Result.Failure;
        }
    }
}
