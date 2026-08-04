namespace BrushForge.Geometry.Validation;

public static class BrushValidationDiagnosticCodes
{
    public const string NoVertices = "BF3100";
    public const string FaceHasTooFewVertices = "BF3101";
    public const string FaceAreaTooSmall = "BF3102";
    public const string CentroidNotInsideFace = "BF3103";
    public const string OpenOrNonManifoldEdges = "BF3104";
    public const string ExtentTooSmall = "BF3105";
    public const string VolumeTooSmall = "BF3106";
    public const string VertexHasTooFewIncidentFaces = "BF3107";
}
