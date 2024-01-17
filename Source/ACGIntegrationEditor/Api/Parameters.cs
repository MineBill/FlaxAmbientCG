#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine.Utilities;

namespace ACGIntegrationEditor.Api;

public class Parameters
{
    public string? Query { get; set; } = null;
    public AssetType? Type { get; set; } = null;
    public CaptureMethod? Method { get; set; } = null;
    public string? Id { get; set; } = null;
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 5;
    public IncludeData[]? AdditionalData { get; set; } = new[] { IncludeData.ImageData, IncludeData.DownloadData };

    public string ToParameters()
    {
        var retval = new Dictionary<string, string>();

        if (Query is not null)
            retval.Add("q", Query);
        if (Type is not null)
            retval.Add("type", Type.Value.ToString().TrimStart('_'));
        if (Method is not null)
            retval.Add("method", Method.ToString()!);
        if (Id is not null)
            retval.Add("id", Id);
        if (AdditionalData is not null)
        {
            var data = string.Join(',', AdditionalData.Select(x =>
            {
                var hm = x.ToString();
                return char.ToLowerInvariant(hm[0]) + hm[1..];
            }));
            retval.Add("include", data);
        }
        retval.Add("offset", Offset.ToString());
        retval.Add("limit", Limit.ToString());

        return string.Join('&', retval.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}

public enum IncludeData
{
    StatisticsData,
    TagData,
    DisplayData,
    DimensionsData,
    RelationshipData,
    NeighbourData,
    DownloadData,
    PreviewData,
    MapData,
    UsdData,
    ImageData,
}