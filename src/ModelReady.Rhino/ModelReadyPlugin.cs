namespace ModelReady.Rhino;

/// <summary>
/// Rhino creates the single plug-in instance when the assembly is loaded.
/// </summary>
public sealed class ModelReadyPlugin : global::Rhino.PlugIns.PlugIn
{
    public ModelReadyPlugin()
    {
        Instance = this;
    }

    public static ModelReadyPlugin Instance { get; private set; } = null!;
}
