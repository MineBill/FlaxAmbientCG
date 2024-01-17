#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACGIntegrationEditor.Api;

public class FullJson
{
    [JsonProperty("searchQuery")]
    public SearchQuery? SearchQuery { get; set; }

    [JsonProperty("foundAssets")]
    public FoundAsset[]? FoundAssets { get; set; }

    [JsonProperty("numberOfResults")]
    public int NumberOfResults { get; set; }
}

public class SearchQuery
{
    [JsonProperty("forceSpecificAssetId")]
    public bool ForceSpecificAssetId { get; set; }
    [JsonProperty("limit")]
    public string? Limit { get; set; }
    [JsonProperty("offset")]
    public string? Offset { get; set; }
}

public class FoundAsset
{
    [JsonProperty("assetId")]
    public string? AssetId { get; set; }

    [JsonProperty("releaseDate")]
    public DateTime ReleaseDate { get; set; }

    [JsonProperty("dataType")]
    public AssetType DataType { get; set; }

    [JsonProperty("previewImage")]
    public Dictionary<string, string>? PreviewImage { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }

    public List<Download> Downloads { get; set; } = new();
}

public enum AssetType
{
    _3DModel,
    Atlas,
    Brush,
    Decal,
    HDRI,
    Material,
    PlainTexture,
    Substance,
    Terrain,
}

public class Download
{
    /// <summary>
    /// Size in bytes.
    /// </summary>
    [JsonProperty("size")]
    public long Size { get; init; }

    public long SizeMb => Size / (1024 * 1024);

    /// <summary>
    /// The asset type. Includes the name of the resolution and filetype(PNG or JPEG).
    /// </summary>
    [JsonProperty("attribute")]
    public string Attribute { get; init; } = string.Empty;

    /// <summary>
    /// The download link recommended by the api docs. It is use to track downloads.
    /// </summary>
    [JsonProperty("downloadLink")]
    public string DownloadLink { get; init; } = string.Empty;

    /// <summary>
    /// The full download path bypassing any tracking. It is provided in case the client
    /// cannot handle redirects.
    /// </summary>
    [JsonProperty("fullDownloadPath")]
    public string FullDownloadPath { get; init; } = string.Empty;

    /// <summary>
    /// Filetype of the download, usually zip.
    /// </summary>
    [JsonProperty("fileType")]
    public string FileType { get; init; } = string.Empty;

    /// <summary>
    /// The names of the files contained inside the zip.
    /// </summary>
    public string[] ZipContent { get; init; }
}