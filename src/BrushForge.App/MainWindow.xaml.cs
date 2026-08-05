using System.Globalization;
using System.IO;
using System.Windows;
using BrushForge.App.Preview;
using BrushForge.Core.Randomness;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;
using BrushForge.MapFormat.Serialization;
using Microsoft.Win32;

namespace BrushForge.App;

/// <summary>
/// Hosts the first complete foliage-generation and map-export workflow.
/// </summary>
public partial class MainWindow : Window
{
    private TreeGenerationResult? _currentResult;
    private readonly OrbitCameraController _previewCameraController;

    public MainWindow()
    {
        InitializeComponent();
        _previewCameraController =
            new OrbitCameraController(
                TreePreviewViewport);
        GenerateTree();
    }

    private void OnGenerateClick(
        object sender,
        RoutedEventArgs e)
    {
        GenerateTree();
    }

    private void OnRandomizeSeedClick(
        object sender,
        RoutedEventArgs e)
    {
        GenerationSeedTextBox.Text =
            GenerationSeed
                .CreateRandom()
                .ToString();

        GenerateTree();
    }

    private void OnExportClick(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentResult is null) {
            SetError(
                "Generate a valid tree before exporting.");
            return;
        }

        SaveFileDialog dialog = new()
        {
            Title = "Export BrushForge Tree",
            Filter = "Valve 220 map (*.map)|*.map|All files (*.*)|*.*",
            DefaultExt = ".map",
            AddExtension = true,
            FileName = "brushforge-tree.map",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true) {
            return;
        }

        try {
            Valve220MapWriter.WriteFile(
                dialog.FileName,
                _currentResult.Document);

            StatusTextBlock.Foreground =
                System.Windows.Media.Brushes.LightGreen;

            StatusTextBlock.Text =
                $"Exported {Path.GetFileName(dialog.FileName)} with {_currentResult.BrushCount.ToString(CultureInfo.InvariantCulture)} brushes.";
        }
        catch (IOException exception) {
            SetError(exception.Message);
        }
        catch (UnauthorizedAccessException exception) {
            SetError(exception.Message);
        }
        catch (InvalidOperationException exception) {
            SetError(exception.Message);
        }
    }

    private void GenerateTree()
    {
        try {
            TreeGenerationSettings settings =
                TreeGenerationInputParser.Parse(
                    ReadInput());

            TreeGenerationResult result =
                TreeGenerator.Generate(
                    settings);

            _currentResult = result;
            ExportButton.IsEnabled = true;

            TreePreviewRenderer.Render(
                TreePreviewViewport,
                result);
            _previewCameraController.Reset(
                result.Bounds);

            PreviewSummaryTextBlock.Text =
                $"{result.BrushCount.ToString(CultureInfo.InvariantCulture)} brushes | " +
                $"{result.Bounds.Width.ToString("0.##", CultureInfo.InvariantCulture)} × " +
                $"{result.Bounds.Depth.ToString("0.##", CultureInfo.InvariantCulture)} × " +
                $"{result.Bounds.Height.ToString("0.##", CultureInfo.InvariantCulture)} units";

            StatusTextBlock.Foreground =
                System.Windows.Media.Brushes.LightGreen;

            StatusTextBlock.Text =
                "Tree generated from the current parameters. The preview uses the actual generated brush faces.";
        }
        catch (FormatException exception) {
            ClearResult(exception.Message);
        }
        catch (ArgumentException exception) {
            ClearResult(exception.Message);
        }
        catch (OverflowException exception) {
            ClearResult(exception.Message);
        }
        catch (InvalidOperationException exception) {
            ClearResult(exception.Message);
        }
    }

    private TreeGenerationInput ReadInput()
    {
        return new TreeGenerationInput(
            GenerationSeedTextBox.Text,
            OverallHeightTextBox.Text,
            TrunkWidthTextBox.Text,
            CanopyWidthTextBox.Text,
            CanopyHeightTextBox.Text,
            CanopyLayerCountTextBox.Text,
            GridSpacingTextBox.Text,
            TrunkTextureTextBox.Text,
            CanopyTextureTextBox.Text);
    }

    private void ClearResult(
        string message)
    {
        _currentResult = null;
        ExportButton.IsEnabled = false;
        TreePreviewViewport.Children.Clear();
        PreviewSummaryTextBlock.Text =
            "No valid tree is currently available.";

        SetError(message);
    }

    private void SetError(
        string message)
    {
        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightSalmon;

        StatusTextBlock.Text = message;
    }
}
