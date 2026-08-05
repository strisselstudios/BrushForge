using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
    private const string DefaultGenerationSeed = "1";
    private const double DefaultOverallHeight = 256.0;
    private const double DefaultTrunkWidth = 32.0;
    private const double DefaultCanopyWidth = 160.0;
    private const double DefaultCanopyHeight = 128.0;
    private const int DefaultCanopyLayerCount = 3;
    private const double DefaultGridSpacing = 8.0;
    private const string DefaultTrunkTexture = "WOOD";
    private const string DefaultCanopyTexture = "LEAF";

    private static readonly TimeSpan LiveRegenerationDelay =
        TimeSpan.FromMilliseconds(
            100.0);

    private TreeGenerationResult? _currentResult;
    private readonly OrbitCameraController _previewCameraController;
    private readonly DispatcherTimer _liveRegenerationTimer;
    private bool _suppressLiveRegeneration;

    public MainWindow()
    {
        InitializeComponent();

        _liveRegenerationTimer =
            new DispatcherTimer
            {
                Interval =
                    LiveRegenerationDelay
            };

        _liveRegenerationTimer.Tick +=
            OnLiveRegenerationTimerTick;

        InitializeConstrainedControls();

        _previewCameraController =
            new OrbitCameraController(
                TreePreviewViewport);

        GenerateTree(
            resetCamera: true,
            isAutomatic: false);
    }

    private void InitializeConstrainedControls()
    {
        ConfigureSlider(
            OverallHeightSlider,
            minimum: 256.0,
            maximum: 1024.0,
            step: 16.0,
            value: DefaultOverallHeight);

        ConfigureSlider(
            TrunkWidthSlider,
            minimum: 16.0,
            maximum: 96.0,
            step: 16.0,
            value: DefaultTrunkWidth);

        ConfigureSlider(
            CanopyWidthSlider,
            minimum: 128.0,
            maximum: 512.0,
            step: 16.0,
            value: DefaultCanopyWidth);

        ConfigureSlider(
            CanopyHeightSlider,
            minimum: 128.0,
            maximum: 240.0,
            step: 16.0,
            value: DefaultCanopyHeight);

        for (
            int layerCount =
                TreeGenerationSettings.MinimumCanopyLayerCount;
            layerCount <=
                TreeGenerationSettings.MaximumCanopyLayerCount;
            layerCount++
        ) {
            CanopyLayerCountComboBox.Items.Add(
                layerCount);
        }

        CanopyLayerCountComboBox.SelectedItem =
            DefaultCanopyLayerCount;

        foreach (
            double gridSpacing in
            new[]
            {
                4.0,
                8.0,
                16.0
            }
        ) {
            GridSpacingComboBox.Items.Add(
                gridSpacing);
        }

        GridSpacingComboBox.SelectedItem =
            DefaultGridSpacing;

        OverallHeightSlider.ValueChanged +=
            OnDimensionSliderValueChanged;
        TrunkWidthSlider.ValueChanged +=
            OnDimensionSliderValueChanged;
        CanopyWidthSlider.ValueChanged +=
            OnDimensionSliderValueChanged;
        CanopyHeightSlider.ValueChanged +=
            OnDimensionSliderValueChanged;

        CanopyLayerCountComboBox.SelectionChanged +=
            OnGenerationSelectionChanged;
        GridSpacingComboBox.SelectionChanged +=
            OnGenerationSelectionChanged;

        ValidateSafeControlEnvelope();
        UpdateDimensionValueLabels();
    }

    private static void ConfigureSlider(
        Slider slider,
        double minimum,
        double maximum,
        double step,
        double value)
    {
        slider.Minimum = minimum;
        slider.Maximum = maximum;
        slider.TickFrequency = step;
        slider.SmallChange = step;
        slider.LargeChange = step * 4.0;
        slider.Value = value;
    }

    private static void ValidateSafeControlEnvelope()
    {
        const double smallestOverallHeight = 256.0;
        const double largestTrunkWidth = 96.0;
        const double smallestCanopyWidth = 128.0;
        const double smallestCanopyHeight = 128.0;
        const double largestCanopyHeight = 240.0;
        const double largestGridSpacing = 16.0;

        if (
            largestCanopyHeight >=
            smallestOverallHeight
        ) {
            throw new InvalidOperationException(
                "The canopy-height control range must remain below the minimum overall height.");
        }

        if (
            smallestOverallHeight -
            largestCanopyHeight <
            largestGridSpacing
        ) {
            throw new InvalidOperationException(
                "The control ranges must leave at least one maximum-size grid unit below the canopy.");
        }

        if (
            largestTrunkWidth >
            smallestCanopyWidth
        ) {
            throw new InvalidOperationException(
                "The trunk-width control range cannot exceed the minimum canopy width.");
        }

        if (
            smallestCanopyHeight <
            largestGridSpacing *
            TreeGenerationSettings.MaximumCanopyLayerCount
        ) {
            throw new InvalidOperationException(
                "The canopy-height control range must support every layer at the largest grid spacing.");
        }
    }

    private void OnDimensionSliderValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateDimensionValueLabels();
        ScheduleLiveRegeneration();
    }

    private void OnGenerationSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ScheduleLiveRegeneration();
    }

    private void ScheduleLiveRegeneration()
    {
        if (_suppressLiveRegeneration) {
            return;
        }

        _liveRegenerationTimer.Stop();
        _liveRegenerationTimer.Start();

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightBlue;
        StatusTextBlock.Text =
            "Updating the preview from the current controls...";
    }

    private void OnLiveRegenerationTimerTick(
        object? sender,
        EventArgs e)
    {
        _liveRegenerationTimer.Stop();

        GenerateTree(
            resetCamera: false,
            isAutomatic: true);
    }

    private void UpdateDimensionValueLabels()
    {
        OverallHeightValueTextBlock.Text =
            FormatUnits(
                OverallHeightSlider.Value);

        TrunkWidthValueTextBlock.Text =
            FormatUnits(
                TrunkWidthSlider.Value);

        CanopyWidthValueTextBlock.Text =
            FormatUnits(
                CanopyWidthSlider.Value);

        CanopyHeightValueTextBlock.Text =
            FormatUnits(
                CanopyHeightSlider.Value);
    }

    private static string FormatUnits(
        double value)
    {
        return
            $"{value.ToString("0", CultureInfo.InvariantCulture)} units";
    }

    private static string FormatControlValue(
        double value)
    {
        return value.ToString(
            "0",
            CultureInfo.InvariantCulture);
    }

    private static double SelectRandomSliderTick(
        Slider slider,
        DeterministicRandom random)
    {
        if (
            slider.TickFrequency <= 0.0 ||
            slider.Maximum < slider.Minimum
        ) {
            throw new InvalidOperationException(
                "A randomized slider must have a valid positive tick range.");
        }

        double tickSpan =
            (slider.Maximum - slider.Minimum) /
            slider.TickFrequency;

        int maximumTickIndex =
            checked(
                (int)Math.Round(
                    tickSpan,
                    MidpointRounding.AwayFromZero));

        int selectedTickIndex =
            random.NextInt32(
                maximumTickIndex + 1);

        return
            slider.Minimum +
            (selectedTickIndex * slider.TickFrequency);
    }

    private static T ReadSelectedValue<T>(
        ComboBox comboBox,
        string displayName)
    {
        if (comboBox.SelectedItem is not T value) {
            throw new InvalidOperationException(
                $"{displayName} does not have a valid selection.");
        }

        return value;
    }

    private void OnApplySeedClick(
        object sender,
        RoutedEventArgs e)
    {
        ApplySeed();
    }

    private void OnGenerationSeedKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter) {
            return;
        }

        e.Handled = true;
        ApplySeed();
    }

    private void ApplySeed()
    {
        _liveRegenerationTimer.Stop();

        if (
            !GenerateTree(
                resetCamera: true,
                isAutomatic: false)
        ) {
            return;
        }

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightGreen;
        StatusTextBlock.Text =
            "Seed applied and tree regenerated.";
    }

    private void OnApplyTextureNamesClick(
        object sender,
        RoutedEventArgs e)
    {
        ApplyTextureNames();
    }

    private void OnTextureNameKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter) {
            return;
        }

        e.Handled = true;
        ApplyTextureNames();
    }

    private void ApplyTextureNames()
    {
        _liveRegenerationTimer.Stop();

        if (
            !GenerateTree(
                resetCamera: false,
                isAutomatic: false)
        ) {
            return;
        }

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightGreen;
        StatusTextBlock.Text =
            "Texture names applied to the current tree and future .map exports.";
    }

    private void OnRandomizeSeedClick(
        object sender,
        RoutedEventArgs e)
    {
        _liveRegenerationTimer.Stop();

        GenerationSeedTextBox.Text =
            GenerationSeed
                .CreateRandom()
                .ToString();

        if (
            !GenerateTree(
                resetCamera: true,
                isAutomatic: false)
        ) {
            return;
        }

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightGreen;
        StatusTextBlock.Text =
            "New variation generated from the current tree settings.";
    }

    private void OnRandomTreeClick(
        object sender,
        RoutedEventArgs e)
    {
        _liveRegenerationTimer.Stop();

        GenerationSeed randomSeed =
            GenerationSeed.CreateRandom();

        DeterministicRandom random =
            new(
                randomSeed);

        _suppressLiveRegeneration = true;

        try {
            GenerationSeedTextBox.Text =
                randomSeed.ToString();
            OverallHeightSlider.Value =
                SelectRandomSliderTick(
                    OverallHeightSlider,
                    random);
            TrunkWidthSlider.Value =
                SelectRandomSliderTick(
                    TrunkWidthSlider,
                    random);
            CanopyWidthSlider.Value =
                SelectRandomSliderTick(
                    CanopyWidthSlider,
                    random);
            CanopyHeightSlider.Value =
                SelectRandomSliderTick(
                    CanopyHeightSlider,
                    random);

            UpdateDimensionValueLabels();
        }
        finally {
            _suppressLiveRegeneration = false;
        }

        if (
            !GenerateTree(
                resetCamera: true,
                isAutomatic: false)
        ) {
            return;
        }

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightGreen;
        StatusTextBlock.Text =
            "Random tree generated from a new seed and randomized safe dimensions.";
    }

    private void OnResetToDefaultsClick(
        object sender,
        RoutedEventArgs e)
    {
        _liveRegenerationTimer.Stop();
        _suppressLiveRegeneration = true;

        try {
            GenerationSeedTextBox.Text =
                DefaultGenerationSeed;
            OverallHeightSlider.Value =
                DefaultOverallHeight;
            TrunkWidthSlider.Value =
                DefaultTrunkWidth;
            CanopyWidthSlider.Value =
                DefaultCanopyWidth;
            CanopyHeightSlider.Value =
                DefaultCanopyHeight;
            CanopyLayerCountComboBox.SelectedItem =
                DefaultCanopyLayerCount;
            GridSpacingComboBox.SelectedItem =
                DefaultGridSpacing;
            TrunkTextureTextBox.Text =
                DefaultTrunkTexture;
            CanopyTextureTextBox.Text =
                DefaultCanopyTexture;

            UpdateDimensionValueLabels();
        }
        finally {
            _suppressLiveRegeneration = false;
        }

        if (
            !GenerateTree(
                resetCamera: true,
                isAutomatic: false)
        ) {
            return;
        }

        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightGreen;
        StatusTextBlock.Text =
            "Default tree settings restored.";
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

    private bool GenerateTree(
        bool resetCamera,
        bool isAutomatic)
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
            if (resetCamera) {
                _previewCameraController.Reset(
                    result.Bounds);
            }

            PreviewSummaryTextBlock.Text =
                $"{result.BrushCount.ToString(CultureInfo.InvariantCulture)} brushes | " +
                $"{result.Bounds.Width.ToString("0.##", CultureInfo.InvariantCulture)} × " +
                $"{result.Bounds.Depth.ToString("0.##", CultureInfo.InvariantCulture)} × " +
                $"{result.Bounds.Height.ToString("0.##", CultureInfo.InvariantCulture)} units";

            StatusTextBlock.Foreground =
                System.Windows.Media.Brushes.LightGreen;

            StatusTextBlock.Text =
                isAutomatic
                    ? "Preview updated automatically from the current controls."
                    : "Tree generated from the current parameters. The preview uses the actual generated brush faces.";

            return true;
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

        return false;
    }

    private TreeGenerationInput ReadInput()
    {
        int canopyLayerCount =
            ReadSelectedValue<int>(
                CanopyLayerCountComboBox,
                "Canopy layer count");

        double gridSpacing =
            ReadSelectedValue<double>(
                GridSpacingComboBox,
                "Grid spacing");

        return new TreeGenerationInput(
            GenerationSeedTextBox.Text,
            FormatControlValue(
                OverallHeightSlider.Value),
            FormatControlValue(
                TrunkWidthSlider.Value),
            FormatControlValue(
                CanopyWidthSlider.Value),
            FormatControlValue(
                CanopyHeightSlider.Value),
            canopyLayerCount.ToString(
                CultureInfo.InvariantCulture),
            FormatControlValue(
                gridSpacing),
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

    protected override void OnClosed(
        EventArgs e)
    {
        _liveRegenerationTimer.Stop();
        base.OnClosed(e);
    }

    private void SetError(
        string message)
    {
        StatusTextBlock.Foreground =
            System.Windows.Media.Brushes.LightSalmon;

        StatusTextBlock.Text = message;
    }
}
