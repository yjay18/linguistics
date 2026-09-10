using Avalonia.Media.Imaging;
using Linguistics.Core.Content;

namespace Linguistics.App.Content;

public sealed class ContentImageCache : IDisposable
{
    public const int DefaultMaximumDecodedImages = 32;
    public const long DefaultMaximumDecodedBytes = 32L * 1024 * 1024;

    private readonly object _gate = new();
    private readonly IReadOnlyDictionary<string, ValidatedContentAsset> _assetsById;
    private readonly Dictionary<string, Bitmap> _decodedByVersionedKey = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failedVersionedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rejectedVersionedKeys = new(StringComparer.Ordinal);
    private readonly DecodedImageBudget _budget;
    private bool _disposed;

    public ContentImageCache(
        IEnumerable<ValidatedContentAsset> assets,
        int maximumDecodedImages = DefaultMaximumDecodedImages,
        long maximumDecodedBytes = DefaultMaximumDecodedBytes)
    {
        ArgumentNullException.ThrowIfNull(assets);
        var materialized = assets.OrderBy(asset => asset.Record.Id, StringComparer.Ordinal).ToArray();
        _assetsById = materialized.ToDictionary(asset => asset.Record.Id, StringComparer.Ordinal);
        _budget = new DecodedImageBudget(maximumDecodedImages, maximumDecodedBytes);
        Assets = materialized;
    }

    public IReadOnlyList<ValidatedContentAsset> Assets { get; }

    public int MaximumDecodedImages => _budget.MaximumImages;

    public long MaximumDecodedBytes => _budget.MaximumBytes;

    public int DecodedImageCount
    {
        get
        {
            lock (_gate)
            {
                return _budget.Count;
            }
        }
    }

    public long EstimatedDecodedBytes
    {
        get
        {
            lock (_gate)
            {
                return _budget.EstimatedBytes;
            }
        }
    }

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

            if (_failedVersionedKeys.Contains(asset.CacheKey) ||
                _rejectedVersionedKeys.Contains(asset.CacheKey))
            {
                return false;
            }

            try
            {
                using var stream = File.OpenRead(asset.AbsoluteFilePath);
                bitmap = new Bitmap(stream);
                if (!_budget.TryReserve(bitmap.PixelSize.Width, bitmap.PixelSize.Height))
                {
                    bitmap.Dispose();
                    bitmap = null;
                    _rejectedVersionedKeys.Add(asset.CacheKey);
                    return false;
                }

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
            _rejectedVersionedKeys.Clear();
            _budget.Clear();
            _disposed = true;
        }
    }
}

internal sealed class DecodedImageBudget
{
    public DecodedImageBudget(int maximumImages, long maximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumImages);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        MaximumImages = maximumImages;
        MaximumBytes = maximumBytes;
    }

    public int MaximumImages { get; }

    public long MaximumBytes { get; }

    public int Count { get; private set; }

    public long EstimatedBytes { get; private set; }

    public bool TryReserve(int pixelWidth, int pixelHeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pixelHeight);

        var estimatedBytes = checked((long)pixelWidth * pixelHeight * 4);
        if (Count >= MaximumImages || estimatedBytes > MaximumBytes - EstimatedBytes)
        {
            return false;
        }

        Count++;
        EstimatedBytes += estimatedBytes;
        return true;
    }

    public void Clear()
    {
        Count = 0;
        EstimatedBytes = 0;
    }
}
