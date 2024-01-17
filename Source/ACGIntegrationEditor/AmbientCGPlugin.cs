#if FLAX_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using ACGIntegrationEditor.Api;
using FlaxEditor;
using FlaxEditor.Content;
using FlaxEditor.GUI.ContextMenu;
using FlaxEngine;
using FlaxEngine.Json;

namespace ACGIntegrationEditor;

/// <summary>
/// AmbientCgPlugin Script.
/// </summary>
public class AmbientCGPlugin : EditorPlugin
{
    internal class AmbientCGSettings
    {
        [Tooltip("The material to use for creating material instances.")]
        public Material? BasePbrMaterial { get; set; }

        [Tooltip("The folder to import and create the downloaded textures.")]
        public string ImportFolder { get; set; } = "AmbientCG";

        [EditorDisplay("Material Parameter Names")]
        public string AmbientOcclusion { get; set; } = "AmbientOcclusion";
        [EditorDisplay("Material Parameter Names")]
        public string Color { get; set; } = "Color";
        [EditorDisplay("Material Parameter Names")]
        public string Roughness { get; set; } = "Roughness";
        [EditorDisplay("Material Parameter Names")]
        public string Metalness { get; set; } = "Metalness";
        [EditorDisplay("Material Parameter Names")]
        public string Normal { get; set; } = "Normal";
        [EditorDisplay("Material Parameter Names")]
        public string Displacement { get; set; } = "Displacement";
    }

    internal record ImportBatch(List<string> Files, FoundAsset Asset);

    private const string BaseAddress = "https://ambientcg.com/api/v2";
    private ContextMenuButton? _button;
    private HttpClient? _client;
    private FullJson? _lastApiResponse;

    private List<ImportBatch> _importBatches = new();

    public override void InitializeEditor()
    {
        base.InitializeEditor();
        // DEBUG
        // var type = GetType();
        // if (FlaxEngine.Scripting.IsTypeFromGameScripts(type))
        //     Debug.Log("Type is from game scripts");

        _client = new HttpClient();
        _button = Editor.UI.MenuTools.ContextMenu.AddButton("AmbientCG");
        _button.Clicked += () => new AmbientCGWindow().Show();
        Editor.Options.AddCustomSettings("AmbientCG", () => new AmbientCGSettings());
        Editor.ContentImporting.ImportFileEnd += ContentImportingOnImportFileEnd;
    }

    public override void DeinitializeEditor()
    {
        if (_button is not null)
        {
            _button.Dispose();
            _button = null;
        }
        Editor.Options.RemoveCustomSettings("AmbientCG");
        Editor.ContentImporting.ImportFileEnd -= ContentImportingOnImportFileEnd;

        base.DeinitializeEditor();
    }

    public override void Initialize()
    {
        base.Initialize();
        _description = new PluginDescription
        {
            Author = "MineBill",
            Category = "Assets",
            Name = "AmbientCG Integration",
            IsAlpha = false,
            RepositoryUrl = "N/A",
            Description = "This plugin provides a nice integration with ambientCG, allowing you to quickly download and import material to your projects.",
            AuthorUrl = "https://minebill.github.io",
            Version = new Version(0, 1),
            IsBeta = true,
            HomepageUrl = "https://ambientcg.com"
        };
    }

    public async Task<FullJson?> FullJson(Parameters parameters)
    {
        try
        {
            if (_client is null)
            {
                Debug.LogError("HttpClient wasn't initialized");
                return null;
            }
            var encodedParams = parameters.ToParameters();
            var uri = new Uri($"{BaseAddress}/full_json?{encodedParams}");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var apiResponse = await _client.SendAsync(request);
            if (apiResponse.StatusCode != HttpStatusCode.OK)
            {
                Debug.LogWarning("AmbientCG API didn't respond with OK");
                return null;
            }

            _lastApiResponse = JsonSerializer.Deserialize<FullJson>(await apiResponse.Content.ReadAsStringAsync());

            foreach (var foundAsset in _lastApiResponse.FoundAssets!)
            {
                if (!foundAsset.AdditionalData!.TryGetValue("downloadFolders", out var value)) continue;

                var downloads = value?["default"]?["downloadFiletypeCategories"]?
                    ["zip"]?["downloads"];
                if (downloads is null)
                    continue;

                var download = downloads.ToObject<List<Download>>();
                foundAsset.Downloads = download!;
            }

            Editor.CustomData["AmbientCG_FullJson"] = JsonSerializer.Serialize(_lastApiResponse);
            return _lastApiResponse;
        }
        catch (Exception e)
        {
            Debug.LogError("Caught an exception while fetch API data from ambientCG.");
            Debug.LogException(e);
            return null;
        }
    }

    public async Task Download(FoundAsset asset, Download download, IProgress<float> progress)
    {
        var targetFolder = Path.Combine(Globals.ProjectCacheFolder, "Plugins", "AmbientCG", asset.AssetId);
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var temp = Path.GetTempFileName();
        await using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await _client.DownloadDataAsync(download.DownloadLink, file, progress);

        }
        await ZipHelpers.ExtractToDirectoryAsync(temp, targetFolder, null);

        var files = new List<string>(Directory.GetFiles(targetFolder));
        files.RemoveAll(x =>
            Path.GetFileName(x).Equals($"{asset.AssetId}.png") || x.EndsWith("mtlx") ||
            x.EndsWith("usdc") || x.Contains("NormalGL"));

        // TODO: Delete unwanted files

        var assetFolder = CreateAssetFolder(asset.AssetId);
        _importBatches.Add(new ImportBatch(files, asset));
        Editor.ContentImporting.Import(files, assetFolder, skipSettingsDialog: true);
    }

    private void ContentImportingOnImportFileEnd(IFileEntryAction entry, bool failed)
    {
        for(var i = _importBatches.Count - 1; i >= 0; i--)
        {
            var batch = _importBatches[i];
            if (batch.Files.Contains(entry.SourceUrl))
            {
                batch.Files.Remove(entry.SourceUrl);
                if (batch.Files.Count == 0)
                {
                    _importBatches.RemoveAt(i);
                    Debug.Log(JsonSerializer.Serialize(batch.Files));

                    CreateMaterialForAsset(batch.Asset);
                }
            }
        }
    }

    private void CreateMaterialForAsset(FoundAsset asset)
    {
        var settings = Editor.Options.Options.GetCustomSettings<AmbientCGSettings>("AmbientCG");
        if (settings?.BasePbrMaterial is null)
        {
            Debug.LogError("Settings object is null or contains null data");
            return;
        }

        var path = Path.Combine(GetOrCreateImportFolder().Path, asset.AssetId, $"M_{asset.AssetId}.flax");
        if (Editor.CreateAsset(Editor.NewAssetType.MaterialInstance, path))
        {
            Debug.LogWarning("Failed to create material instance");
            return;
        }

        var material = Content.LoadAsync<MaterialInstance>(path);
        material.WaitForLoaded();
        material.BaseMaterial = settings.BasePbrMaterial;

        var contentFolder = (ContentFolder)Editor.ContentDatabase.Find(Path.Combine(GetOrCreateImportFolder().Path, asset.AssetId));
        if (contentFolder is null)
        {
            Debug.LogWarning("contentFolder is null");
            return;
        }

        // NOTE(minebill): It's possible that the content folder chilren will be modified
        // while iterating but I cannot come up with a reason why that could happen.
        // In that case I just log the exception to the console to prevent the editor
        // from crashing. Might be a good idea to also notify the use from within
        // the custom editor window.
        try {
            foreach (var item in contentFolder.Children)
            {
                if (!item.IsAsset) continue;
                if (item is not TextureAssetItem) continue;
                var texture = Content.Load<Texture>(item.Path);
                texture.WaitForLoaded();

                if (item.ShortName.Contains("Color"))
                    material.SetParameterValue(settings.Color, texture);
                else if (item.ShortName.Contains("Metalness"))
                    material.SetParameterValue(settings.Metalness, texture);
                else if (item.ShortName.Contains("Normal"))
                    material.SetParameterValue(settings.Normal, texture);
                else if (item.ShortName.Contains("Roughness"))
                    material.SetParameterValue(settings.Roughness, texture);
                else if (item.ShortName.Contains("Displacement"))
                    material.SetParameterValue(settings.Displacement, texture);
                else if (item.ShortName.Contains("AmbientOcclusion"))
                {
                    material.SetParameterValue(settings.AmbientOcclusion, texture);
                    var parameter = material.GetParameter("UseAmbientOcclusion");
                    if (parameter is not null)
                    {
                        parameter.Value = true;
                    }
                }
            }
        }catch (Exception e)
        {
            Debug.LogError("Exception caught while iterating over the content folder of the asset:");
            Debug.LogException(e);
        }

        material.Save(path);
    }

    private ContentFolder GetOrCreateImportFolder()
    {
        var settings = Editor.Options.Options.GetCustomSettings<AmbientCGSettings>("AmbientCG");
        var importFolderPath = Path.Combine(Globals.ProjectContentFolder, settings.ImportFolder);
        var contentFolder = (ContentFolder)Editor.ContentDatabase.Find(importFolderPath);
        if (contentFolder is not null) return contentFolder;

        Directory.CreateDirectory(importFolderPath);
        Editor.ContentDatabase.RefreshFolder(Editor.ContentDatabase.Find(Globals.ProjectContentFolder), false);
        contentFolder = (ContentFolder)Editor.ContentDatabase.Find(importFolderPath);

        return contentFolder;
    }

    private ContentFolder CreateAssetFolder(string name)
    {
        var settings = Editor.Options.Options.GetCustomSettings<AmbientCGSettings>("AmbientCG");
        var path = Path.Combine(Globals.ProjectContentFolder, settings.ImportFolder, name);
        Directory.CreateDirectory(path);
        var importFolder = GetOrCreateImportFolder();
        Editor.ContentDatabase.RefreshFolder(importFolder, true);
        return (ContentFolder)Editor.ContentDatabase.Find(path);
    }

    public override void Deinitialize()
    {
        _client?.Dispose();
        _button?.Dispose();
        base.Deinitialize();
    }
}
#endif