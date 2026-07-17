using System.Collections.Generic;

namespace ModelReady.Core;

public sealed class DocumentSnapshot
{
    public DocumentSnapshot(
        string documentName,
        string unitSystem,
        double absoluteToleranceMetres,
        IEnumerable<ModelObjectSnapshot> objects)
    {
        DocumentName = Guard.NotBlank(documentName, nameof(documentName));
        UnitSystem = Guard.NotBlank(unitSystem, nameof(unitSystem));
        AbsoluteToleranceMetres = Guard.NonNegativeFinite(absoluteToleranceMetres, nameof(absoluteToleranceMetres));
        Objects = Guard.ReadOnlyCopy(objects, nameof(objects), rejectNullItems: true);
    }

    public string DocumentName { get; }

    public string UnitSystem { get; }

    public double AbsoluteToleranceMetres { get; }

    public IReadOnlyList<ModelObjectSnapshot> Objects { get; }
}
