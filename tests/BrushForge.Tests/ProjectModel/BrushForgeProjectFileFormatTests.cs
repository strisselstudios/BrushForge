using BrushForge.ProjectModel.Storage;

namespace BrushForge.Tests.ProjectModel;

public sealed class BrushForgeProjectFileFormatTests
{
    [Fact]
    public void FileExtensionUsesBrushForgeSuffix()
    {
        Assert.Equal(
            ".brushforge",
            BrushForgeProjectFileFormat.FileExtension);
    }

    [Fact]
    public void HasSupportedExtensionAcceptsCanonicalExtension()
    {
        Assert.True(
            BrushForgeProjectFileFormat.HasSupportedExtension(
                "project.brushforge"));
    }

    [Fact]
    public void HasSupportedExtensionIsCaseInsensitive()
    {
        Assert.True(
            BrushForgeProjectFileFormat.HasSupportedExtension(
                "project.BRUSHFORGE"));
    }

    [Fact]
    public void HasSupportedExtensionRejectsOtherExtensions()
    {
        Assert.False(
            BrushForgeProjectFileFormat.HasSupportedExtension(
                "project.json"));
    }

    [Fact]
    public void EnsureExtensionReturnsSupportedPath()
    {
        Assert.Equal(
            "project.brushforge",
            BrushForgeProjectFileFormat.EnsureExtension(
                "project"));

        Assert.Equal(
            "project.brushforge",
            BrushForgeProjectFileFormat.EnsureExtension(
                "project.json"));

        Assert.Equal(
            "project.BRUSHFORGE",
            BrushForgeProjectFileFormat.EnsureExtension(
                "project.BRUSHFORGE"));
    }
}
