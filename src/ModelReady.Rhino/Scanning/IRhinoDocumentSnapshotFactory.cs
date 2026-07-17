using ModelReady.Core;
using Rhino;

namespace ModelReady.Rhino.Scanning;

public interface IRhinoDocumentSnapshotFactory
{
    DocumentSnapshot Create(RhinoDoc document);
}
