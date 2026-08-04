using BrushForge.ProjectModel.Projects;

namespace BrushForge.ProjectModel.Serialization;

/// <summary>
/// Public deterministic JSON facade for BrushForge project state.
/// </summary>
public static class BrushForgeProjectJsonSerializer
{
    public static string Serialize(
        BrushForgeProject project,
        bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(project);

        return BrushForgeProjectJsonWriter.Write(
            project,
            indented);
    }

    public static BrushForgeProject Deserialize(
        string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return BrushForgeProjectJsonReader.Read(
            json);
    }
}
