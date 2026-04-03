using System.Text.Json;
using Android.Content;
using FoodStreet.Client.DTOs;

namespace FoodStreet.Mobile.Platforms.Android.Maps;

internal static class NativeMapContracts
{
    public const string RequestJsonExtra = "foodstreet.native_map.request_json";
    public const string ResultJsonExtra = "foodstreet.native_map.result_json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteRequest(Intent intent, MobileNativeMapRequest request)
    {
        intent.PutExtra(RequestJsonExtra, JsonSerializer.Serialize(request, JsonOptions));
    }

    public static MobileNativeMapRequest? ReadRequest(Intent? intent)
    {
        var json = intent?.GetStringExtra(RequestJsonExtra);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MobileNativeMapRequest>(json, JsonOptions);
    }

    public static void WriteResult(Intent intent, MobileNativeMapResult result)
    {
        intent.PutExtra(ResultJsonExtra, JsonSerializer.Serialize(result, JsonOptions));
    }

    public static MobileNativeMapResult? ReadResult(Intent? intent)
    {
        var json = intent?.GetStringExtra(ResultJsonExtra);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MobileNativeMapResult>(json, JsonOptions);
    }
}
