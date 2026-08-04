using System.Text;
using BrushForge.Core.Randomness;
using BrushForge.ProjectModel.Projects;
using BrushForge.ProjectModel.Serialization;
using BrushForge.ProjectModel.Storage;

namespace BrushForge.Tests.ProjectModel;

public sealed class BrushForgeProjectFileStoreTests
{
    private static readonly DateTimeOffset CreatedUtc =
        new(
            2026,
            8,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void SaveCreatesMissingDirectoryAndWritesCanonicalProject()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "nested",
                "project.brushforge");

        BrushForgeProject project =
            CreateProject(
                "Nested Project",
                10UL);

        try {
            BrushForgeProjectFileStore.Save(
                path,
                project);

            Assert.True(
                File.Exists(path));

            string actual =
                File.ReadAllText(
                    path,
                    new UTF8Encoding(
                        encoderShouldEmitUTF8Identifier: false,
                        throwOnInvalidBytes: true));

            Assert.Equal(
                BrushForgeProjectJsonSerializer.Serialize(
                    project),
                actual);
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void SaveWritesUtf8WithoutBom()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "project.brushforge");

        try {
            BrushForgeProjectFileStore.Save(
                path,
                CreateProject());

            byte[] bytes =
                File.ReadAllBytes(path);

            byte[] preamble =
                Encoding.UTF8.GetPreamble();

            Assert.False(
                bytes.Length >= preamble.Length &&
                bytes
                    .Take(preamble.Length)
                    .SequenceEqual(preamble));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void SaveOverwritesExistingProject()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "project.brushforge");

        BrushForgeProject replacement =
            CreateProject(
                "Replacement",
                20UL);

        try {
            BrushForgeProjectFileStore.Save(
                path,
                CreateProject(
                    "Original",
                    10UL));

            BrushForgeProjectFileStore.Save(
                path,
                replacement);

            Assert.Equal(
                replacement,
                BrushForgeProjectFileStore.Load(
                    path));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void SaveLeavesNoTemporaryFiles()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "project.brushforge");

        try {
            BrushForgeProjectFileStore.Save(
                path,
                CreateProject());

            string[] files =
                Directory.GetFiles(
                    directory);

            Assert.Single(files);
            Assert.Equal(
                path,
                files[0]);
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void LoadReconstructsSavedProject()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "project.brushforge");

        BrushForgeProject source =
            CreateProject(
                "Round Trip",
                ulong.MaxValue);

        try {
            BrushForgeProjectFileStore.Save(
                path,
                source);

            BrushForgeProject loaded =
                BrushForgeProjectFileStore.Load(
                    path);

            Assert.Equal(
                source,
                loaded);
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void LoadAcceptsUtf8Bom()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "bom.brushforge");

        BrushForgeProject source =
            CreateProject();

        try {
            File.WriteAllText(
                path,
                BrushForgeProjectJsonSerializer.Serialize(
                    source),
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: true,
                    throwOnInvalidBytes: true));

            Assert.Equal(
                source,
                BrushForgeProjectFileStore.Load(
                    path));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void LoadRejectsInvalidUtf8()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "invalid.brushforge");

        try {
            byte[] invalidUtf8 =
            [
                0xC3,
                0x28
            ];

            File.WriteAllBytes(
                path,
                invalidUtf8);

            Assert.Throws<InvalidDataException>(
                () =>
                    BrushForgeProjectFileStore.Load(
                        path));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void LoadRejectsInvalidProjectJson()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "invalid.brushforge");

        try {
            File.WriteAllText(
                path,
                "{]",
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true));

            Assert.Throws<InvalidDataException>(
                () =>
                    BrushForgeProjectFileStore.Load(
                        path));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void LoadMissingFileThrowsFileNotFoundException()
    {
        string directory =
            CreateTemporaryDirectory();

        string path =
            Path.Combine(
                directory,
                "missing.brushforge");

        try {
            Assert.Throws<FileNotFoundException>(
                () =>
                    BrushForgeProjectFileStore.Load(
                        path));
        }
        finally {
            DeleteTemporaryDirectory(
                directory);
        }
    }

    [Fact]
    public void PathsAreValidated()
    {
        BrushForgeProject project =
            CreateProject();

        Assert.Throws<ArgumentException>(
            () =>
                BrushForgeProjectFileStore.Save(
                    " ",
                    project));

        Assert.Throws<ArgumentException>(
            () =>
                BrushForgeProjectFileStore.Load(
                    " "));
    }

    [Fact]
    public void SaveRejectsNullProject()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
                BrushForgeProjectFileStore.Save(
                    "project.brushforge",
                    null!));
    }

    private static BrushForgeProject CreateProject(
        string name = "Project",
        ulong seed = 1UL)
    {
        return BrushForgeProject.CreateNew(
            Guid.Parse(
                "5039f3e7-9f0a-49d4-bbe4-89d85d09c01e"),
            name,
            CreatedUtc,
            new GenerationSeed(
                seed));
    }

    private static string CreateTemporaryDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "BrushForge.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            directory);

        return directory;
    }

    private static void DeleteTemporaryDirectory(
        string directory)
    {
        if (Directory.Exists(directory)) {
            Directory.Delete(
                directory,
                recursive: true);
        }
    }
}
