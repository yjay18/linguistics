using Avalonia.Media.Imaging;
using Linguistics.Core.Content;

namespace Linguistics.App.Content;

public sealed class ContentImageCache : IDisposable
{
    private readonly object _gate = new();
    private readonly IReadOnlyDictionary<string, ValidatedContentAsset> _assetsById;
    private readonly Dictionary<string, Bitmap> _decodedByVersionedKey = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failedVersionedKeys = new(StringComparer.Ordinal);
    private bool _disposed;

    public ContentImageCache(IEnumerable<ValidatedContentAsset> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        var materialized = assets.OrderBy(asset => asset.Record.Id, StringComparer.Ordinal).ToArray();
        _assetsById = materialized.ToDictionary(asset => asset.Record.Id, StringComparer.Ordinal);
        Assets = materialized;
    }

    public IReadOnlyList<ValidatedContentAsset> Assets { get; }

    public bool TryGetAsset(string? assetId, out ValidatedContentAsset? asset)
    {
        asset = null;
        return !string.IsNullOrWhiteSpace(assetId) && _assetsById.TryGetValue(assetId, out asset);
    }

    public bool TryGetBitmap(string? assetId, out Bitmap? bitmap)
    {
        bitmap = null;
        if (!TryGetAsset(assetId, out var asset) || asset is null)
        {
            return false;
        }

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_decodedByVersionedKey.TryGetValue(asset.CacheKey, out bitmap))
            {
                return true;
            }

            if (_failedVersionedKeys.Contains(asset.CacheKey))
            {
                return false;
            }

            try
            {
                using var stream = File.OpenRead(asset.AbsoluteFilePath);
                bitmap = new Bitmap(stream);
                _decodedByVersionedKey.Add(asset.CacheKey, bitmap);
                return true;
            }
            catch (Exception exception) when (exception is
                IOException or
                UnauthorizedAccessException or
                ArgumentException or
                InvalidOperationException)
            {
                _failedVersionedKeys.Add(asset.CacheKey);
                bitmap = null;
                return false;
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var bitmap in _decodedByVersionedKey.Values)
            {
                bitmap.Dispose();
            }

            _decodedByVersionedKey.Clear();
            _failedVersionedKeys.Clear();
            _disposed = true;
        }
    }
}
