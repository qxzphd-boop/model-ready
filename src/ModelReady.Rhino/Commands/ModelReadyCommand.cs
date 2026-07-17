using Rhino;
using Rhino.Commands;

namespace ModelReady.Rhino.Commands;

public sealed class ModelReadyCommand : Command
{
    public override string EnglishName => "ModelReady";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        RhinoApp.WriteLine("ModelReady v0.1 scaffold loaded. Preflight rules are not implemented yet.");
        return Result.Success;
    }
}
