using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACGIntegrationEditor.Api;
using FlaxEditor;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Elements;
using FlaxEngine;
using FlaxEngine.GUI;
using FlaxEngine.Json;

namespace ACGIntegrationEditor;

/// <summary>
/// AmbientCGWindow Script.
/// </summary>
public class AmbientCGWindow : CustomEditorWindow
{
    private CustomElementsContainer<TilesPanel> _tilesPanel;
    private TextBoxElement _textBox;

    // NOTE(minebill): This is only use to fetch the thumbnails.
    // Maybe use the Plugin to fetch them.
    private HttpClient _client;
    private LabelElement _resultsLabel;
    private string _thumbnailCacheFolder;
    private readonly Parameters _parameters = new();

    public override void Initialize(LayoutElementsContainer layout)
    {
        _thumbnailCacheFolder = Path.Combine(Globals.ProjectCacheFolder, "Plugins", "AmbientCG", "Thumbnails");
        if (!Directory.Exists(_thumbnailCacheFolder))
        {
            Directory.CreateDirectory(_thumbnailCacheFolder);
        }
        _client = new HttpClient();

        var panelElement = layout.VerticalPanel();
        panelElement.Panel.Margin = new Margin(5);

        // Search box
        var panel = panelElement;

        panel.Label("Enter search term(multiple search terms can be separated with commas):");
        _textBox = panel.TextBox();
        _textBox.TextBox.WatermarkText = "e.g. 'grass'";
        _textBox.TextBox.Height = 30;
        _textBox.TextBox.Font.Size = 14;
        panel.Space(3);

        var gridElement = panel.CustomContainer<GridPanel>();
        var grid = gridElement.CustomControl;
        grid.RowFill = new[] { 1.0f };
        grid.ColumnFill = new[] { 0.35f, 0.30f, 0.35f };
        // grid.CustomControl
        gridElement.Space(0);
        var btn = gridElement.Button("Search ");
        btn.Button.Clicked += () => { Update(force: true); };
        gridElement.Space(0);

        panelElement.Space(10);

        var fg = panelElement.Group("Filters");
        fg.Panel.EnableContainmentLines = false;
        fg.Panel.EnableDropDownIcon = false;

        var b = fg.HorizontalPanel();
        b.Panel.AutoSize = true;
        var filters = b.VerticalPanel();
        filters.Panel.Size = new Float2(200, 0);
        filters.Panel.AutoSize = true;

        var assetType = filters.Enum(typeof(AssetType));
        assetType.ComboBox.EnumValueChanged += box =>
        {
            var type = (AssetType)box.EnumTypeValue;
            _parameters.Type = type;
        };
        assetType.ComboBox.EnumTypeValue = AssetType.Material;
        assetType.ComboBox.Enabled = false;
        assetType.ComboBox.TooltipText = "Allows changing the asset type. Only Material is supported for now.";
        var intBox = filters.IntegerValue("Results per page:", "How many results to show per page");
        intBox.IntValue.ValueChanged += () =>
        {
            _parameters.Limit = intBox.Value;
        };
        intBox.IntValue.MinValue = 1;
        intBox.IntValue.MaxValue = 20;
        intBox.IntValue.Value = 20;

        // Result label and next/prev buttons
        {
            var h = panelElement.HorizontalPanel();
            h.Panel.Height = 20;
            h.Panel.AutoSize = true;
            _resultsLabel = h.Label(string.Empty);
            _resultsLabel.Label.AutoWidth = true;
            h.Space(10);
            h.Button("< Previous").Button.Width = 75;
            h.Button("Next >").Button.Width = 75;
        }

        _tilesPanel = panelElement.CustomContainer<TilesPanel>();
        _tilesPanel.Control.BackgroundColor = FlaxEngine.GUI.Style.Current.CollectionBackgroundColor;
        _tilesPanel.CustomControl.AutoResize = true;
        _tilesPanel.CustomControl.TileSize = new Float2(150, 200);
        _tilesPanel.CustomControl.TileMargin = new Margin(3);

        // MAIN-NOTE
        // NOTE(minebill): Ideally we would call Update()
        // here to make sure the window gets populated with
        // the data from the latest request.
        // Useful for when docking/undocking.
        // The problem is that there are certain bugs that
        // i can't figure out, such us newly created elements
        // being null or containers not resizing.
    }

    protected override void Deinitialize()
    {
        _client.Dispose();
        _resultsLabel.Label.Dispose();
        _tilesPanel.CustomControl.Dispose();
        _textBox.TextBox.Dispose();

        _resultsLabel = null;
        _tilesPanel = null;
        _textBox = null;
        base.Deinitialize();
    }

    /// <summary>
    /// Updates the entries by either using a cached response (saved in <see cref="Editor.CustomData"/>)
    /// or fetching a new one from ambientCG.
    /// </summary>
    /// <param name="force">Where to force update by fetching new data from the API.</param>
    private async void Update(bool force = false)
    {
        // NOTE(minebill): This will currently do nothing since
        // the update method is always called with force = true.
        // Check if there is any available response. See MAIN-NOTE.
        if (!force && Editor.Instance.CustomData.TryGetValue("AmbientCG_FullJson", out string value))
        {
            var data = JsonSerializer.Deserialize<FullJson>(value);
            PopulateEntries(data);
            return;
        }

        _parameters.Query = _textBox.Text;
        var plugin = PluginManager.GetPlugin<AmbientCGPlugin>();
        PopulateEntries(await plugin.FullJson(_parameters));
    }

    private void PopulateEntries(FullJson apiData)
    {
        if (apiData == null)
            return;

        _tilesPanel.CustomControl.DisposeChildren();
        // See MAIN-NOTE
        // NOTE(minebill): When this is called from `Initialize`
        // The _tilesPanel.CustomControl will have it's layout locked.
        // This means, any new children added to it will not update its size
        // or cause it to update.
        // _tilesPanel.CustomControl.IsLayoutLocked = false;
        foreach(var asset in apiData.FoundAssets)
        {
            var verticalPanel = _tilesPanel.VerticalPanel();
            verticalPanel.Panel.AutoSize = true;
            verticalPanel.Panel.Margin = new Margin(5);
            verticalPanel.Panel.BackgroundColor = FlaxEngine.GUI.Style.Current.Background;
            var open = verticalPanel.ClickableLabel("View in browser").CustomControl;
            open.LeftClick += () =>
            {
                Platform.OpenUrl($"https://ambientcg.com/view?id={asset.AssetId}");
            };

            var image = verticalPanel.Image(Editor.Instance.Icons.Add32).Image;
            image.Size = new Float2(128, 128);
            var file = Path.Combine(_thumbnailCacheFolder, $"{asset.AssetId}_thumbnail.png");
            if (File.Exists(file))
            {
                var texture = Texture.FromFile(file);
                image.Brush = new TextureBrush(texture);
            }
            else
            {
                if (asset.PreviewImage is not null)
                {
                    Task.Run(async () =>
                    {
                        var url = asset.PreviewImage["128-PNG"];
                        var bytes = await _client.GetByteArrayAsync(url);
                        // Since Flax doesn't support loading from memory, we have to save it to a file first
                        await File.WriteAllBytesAsync(file, bytes);
                        var texture = Texture.FromFile(file);
                        image.Brush = new TextureBrush(texture);
                    });
                }
            }

            verticalPanel.Label(asset.AssetId, TextAlignment.Center);
            var optionsGroup = verticalPanel.ComboBox("Options");
            asset.Downloads.Sort((a, b) => (int)(a.Size - b.Size));

            foreach (var download in asset.Downloads)
            {
                optionsGroup.ComboBox.AddItem($"{download.Attribute} ({download.SizeMb} MB)");
            }

            optionsGroup.ComboBox.SelectedIndex = 0;

            var progressBar = verticalPanel.Custom<ProgressBar>().CustomControl;
            progressBar.Visible = false;
            progressBar.Minimum = 0;
            progressBar.Maximum = 1.0f;
            progressBar.BarMargin = Margin.Zero;

            var button = verticalPanel.Button("Download").Button;
            progressBar.Size = button.Size;

            var progress = new Progress<float>();
            progress.ProgressChanged += (_, value) =>
            {
                progressBar.Value = value;
                if (Mathf.NearEqual(value, progressBar.Maximum))
                {
                    button.Visible = true;
                    // Disable the smoothing temporarily to force Value to zero.
                    progressBar.SmoothingScale = 0.0f;
                    progressBar.Value = 0;
                    progressBar.SmoothingScale = 1.0f;

                    progressBar.Visible = false;
                }
            };

            button.Clicked += () =>
            {
                var plugin = PluginManager.GetPlugin<AmbientCGPlugin>();
                Task.Run(async () =>
                {
                    button.Visible = false;

                    progressBar.Visible = true;
                    await plugin.Download(asset, asset.Downloads[optionsGroup.ComboBox.SelectedIndex], progress);
                });
            };
            Debug.Log($"Setting TileSize to: {verticalPanel.Panel.Size}");
            _tilesPanel.CustomControl.TileSize = verticalPanel.Panel.Size;
            Debug.Log($"_tilePanel.TileSize is: {_tilesPanel.CustomControl.TileSize}");
        }

        var limit = int.Parse(apiData.SearchQuery.Limit);
        var offset = int.Parse(apiData.SearchQuery.Offset);
        var shown = apiData.NumberOfResults > (limit + offset) ? limit + offset : apiData.NumberOfResults;
        _resultsLabel.Label.Text = $"Showing {shown} out of {apiData.NumberOfResults} results";
    }
}