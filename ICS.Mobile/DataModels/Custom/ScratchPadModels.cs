namespace ICS.Portal.Data.Custom;

/// <summary>
/// Root container for all scratchpad data - persisted to IndexedDB on Mobile or SQL on portal
/// </summary>
public class ScratchPadData
{
    public string Key { get; set; } = ""; // User-tuned for IndexedDB
    public List<ScratchPadTopic> Topics { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string? ActiveTopicId { get; set; }
    public long Size => 0;
}

/// <summary>
/// A topic/category in the scratchpad (like a folder or notebook)
/// </summary>
public class ScratchPadTopic
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New Topic";
    public string Icon { get; set; } = "📋"; // Emoji icon for the topic
    public string Color { get; set; } = "#7FCDE6"; // Theme color
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public List<ScratchPadItem> Items { get; set; } = new();
    public int SortOrder { get; set; } = 0;
    public bool IsExpanded { get; set; } = true;
}

/// <summary>
/// A single item/note within a topic
/// </summary>
public class ScratchPadItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public int SortOrder { get; set; } = 0;
    
    // Persisted flag - indicates image exists in ScratchPadBlob store
    public bool HasStoredImage { get; set; } = false;
    
    // In-memory only - tracks if blob load failed (not found in IndexedDB)
    [System.Text.Json.Serialization.JsonIgnore]
    public bool ImageLoadFailed { get; set; } = false;
    
    // In-memory only - NOT persisted in main store, loaded from ScratchPadBlob
    [System.Text.Json.Serialization.JsonIgnore]
    private string? _imageBase64;
    
    [System.Text.Json.Serialization.JsonIgnore]
    private string? _imageMimeType;
    
    [System.Text.Json.Serialization.JsonIgnore]
    private string? _cachedImageSrc;
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ImageBase64
    {
        get => _imageBase64;
        set
        {
            _imageBase64 = value;
            _cachedImageSrc = null; // Clear cache when image changes
        }
    }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ImageMimeType
    {
        get => _imageMimeType;
        set
        {
            _imageMimeType = value;
            _cachedImageSrc = null; // Clear cache when mime type changes
        }
    }
    
    // Helper to check if item has an image (either loaded or stored)
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasImage => HasStoredImage || !string.IsNullOrEmpty(_imageBase64);
    
    // Helper to get displayable image source - CACHED to avoid rebuilding large string
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ImageSrc
    {
        get
        {
            if (_cachedImageSrc != null) return _cachedImageSrc;
            
            if (!string.IsNullOrEmpty(_imageBase64) && !string.IsNullOrEmpty(_imageMimeType))
            {
                _cachedImageSrc = $"data:{_imageMimeType};base64,{_imageBase64}";
            }
            return _cachedImageSrc;
        }
    }
    
    // Item type for future expansion
    public ScratchPadItemType ItemType { get; set; } = ScratchPadItemType.Note;
    
    // Helper to set image data efficiently (avoids double cache clear)
    public void SetImageData(string? base64, string? mimeType)
    {
        _imageBase64 = base64;
        _imageMimeType = mimeType;
        _cachedImageSrc = null;
    }
    
    // Helper to clear image data
    public void ClearImageData()
    {
        _imageBase64 = null;
        _imageMimeType = null;
        _cachedImageSrc = null;
        HasStoredImage = false;
        ImageLoadFailed = false;
    }
}

/// <summary>
/// Separate blob storage for images - stored in ScratchPadBlob IndexedDB store
/// Key format: {topicId}_{itemId}
/// </summary>
public class ScratchPadBlob
{
    public string Key { get; set; } = string.Empty; // {CurrentUserId}_{topicId}_{itemId}
    public string ImageBase64 { get; set; } = string.Empty;
    public string ImageMimeType { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    // Helper to generate key
    public static string MakeKey(int CurrentUserId, string topicId, string itemId) => $"{CurrentUserId}_{topicId}_{itemId}";

    public long Size => ImageBase64?.Length ?? 0;
}

public enum ScratchPadItemType
{
    Note = 0,
    Todo = 1,
    Image = 2,
    Link = 3
}

/// <summary>
/// Quick copy snippet - for frequently used text
/// </summary>
public class ScratchPadSnippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int UseCount { get; set; } = 0;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result from clipboard read operation
/// </summary>
public class ClipboardContent
{
    public string? Text { get; set; }
    public string? ImageBase64 { get; set; }
    public string? ImageMimeType { get; set; }
    public bool HasText => !string.IsNullOrEmpty(Text);
    public bool HasImage => !string.IsNullOrEmpty(ImageBase64);
}
