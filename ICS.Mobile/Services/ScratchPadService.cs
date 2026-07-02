using ICS.Portal.Data.Custom;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ICS.Mobile.Services
{
    /// <summary>
    /// Singleton service for managing scratchpad data globally across all pages.
    /// Data is persisted to IndexedDB and survives page navigation.
    /// 
    /// Architecture:
    /// - ScratchPadData store: text, metadata, flags (fast saves)
    /// - ScratchPadBlob store: images only (separate, lazy-loaded)
    /// </summary>
    public class ScratchPadService
    {
        private readonly IWorkflowData dataService;
        private readonly IDXDBService _idxdb;
        private readonly IJSRuntime _js;

        private ScratchPadData? _data = null;
        private bool _isLoaded = false;
        private bool _isVisible = false;
        private string? _loadedBlobsTopicId = null; // Track which topic has blobs loaded

        public int CurrentUserId { set; get; }

        // Event for notifying components of state changes
        public event Action? OnChange;
        public event Action? OnVisibilityChanged;

        public ScratchPadService(IWorkflowData dataService, IDXDBService idxdb, IJSRuntime js)
        {
            this.dataService = dataService;
            _idxdb = idxdb;
            _js = js;
        }

        #region Visibility Control

        public bool IsVisible => _isVisible;

        public void Show()
        {
            _isVisible = true;
            OnVisibilityChanged?.Invoke();
        }

        public void Hide()
        {
            _isVisible = false;
            OnVisibilityChanged?.Invoke();
        }

        public void Toggle()
        {
            _isVisible = !_isVisible;
            OnVisibilityChanged?.Invoke();
        }

        #endregion

        #region Data Access

        public ScratchPadData? Data => _data;
        public bool IsLoaded => _isLoaded;

        public List<ScratchPadTopic> Topics => _data?.Topics ?? new List<ScratchPadTopic>();

        public ScratchPadTopic? ActiveTopic =>
            _data?.Topics.FirstOrDefault(t => t.Id == _data.ActiveTopicId)
            ?? _data?.Topics.FirstOrDefault();

        public string? ActiveTopicId
        {
            get => _data?.ActiveTopicId;
            set
            {
                if (_data == null) return;
                _data.ActiveTopicId = value;
                // Load blobs for new active topic
                _ = LoadBlobsForTopicAsync(value);
                NotifyStateChanged();
            }
        }

        #endregion

        #region Data Loading & Persistence

        public async Task LoadAsync(int userId)
        {
            if (_isLoaded) return;
            if (userId <= 0) return; 


            CurrentUserId = userId;

            try
            {
                var stored = await _idxdb.GetValueByKey<ScratchPadData>($"ScratchPadData-{userId}");
                if (stored != null)
                {
                    _data = stored;
                }
                else
                {
                    await dataService.GetScratchPad();
                    stored = await _idxdb.GetValueByKey<ScratchPadData>($"ScratchPadData-{userId}");
                    if (stored != null)
                    {
                        _data = stored;
                    }
                    else
                    {
                        // Initialize with default topics
                        _data = new ScratchPadData();
                        _data.Key = $"ScratchPadData-{userId}";
                        _data.Topics.Add(new ScratchPadTopic
                        {
                            Title = "QuickNote",
                            Icon = "📝",
                            Color = "#7FCDE6"
                        });
                        _data.Topics.Add(new ScratchPadTopic
                        {
                            Title = "Job Info",
                            Icon = "🔧",
                            Color = "#FF9200"
                        });
                        _data.Topics.Add(new ScratchPadTopic
                        {
                            Title = "Parts & Models",
                            Icon = "⚙️",
                            Color = "#7CB13C"
                        });

                        _data.ActiveTopicId = _data.Topics.First().Id;
                        await SaveAsync();
                    }
                }

                _isLoaded = true;

                // Load blobs for active topic
                if (_data.ActiveTopicId != null)
                {
                    await LoadBlobsForTopicAsync(_data.ActiveTopicId);
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;

                Console.WriteLine($"ScratchPadService.LoadAsync error: {ex.Message}");
                _data = null;
                _isLoaded = false;
            }
        }

        /// <summary>
        /// Save main data only (no images) - FAST!
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                _data.LastModified = DateTime.UtcNow;
                await _idxdb.Save(_data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ScratchPadService.SaveAsync error: {ex.Message}");
            }
        }

        #endregion

        #region Blob Storage (Images)

        /// <summary>
        /// Load all image blobs for a specific topic
        /// </summary>
        public async Task LoadBlobsForTopicAsync(string? topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return;

            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null) return;

            // Find items that have stored images but no image data in memory
            var itemsNeedingBlobs = topic.Items
                .Where(i => i.HasStoredImage && string.IsNullOrEmpty(i.ImageBase64))
                .ToList();

            // Skip if same topic and all images already loaded
            if (_loadedBlobsTopicId == topicId && itemsNeedingBlobs.Count == 0)
            {
                return;
            }

            try
            {
                foreach (var item in itemsNeedingBlobs)
                {
                    var blobKey = ScratchPadBlob.MakeKey(CurrentUserId, topicId, item.Id);
                    var blob = await _idxdb.GetValueByKey<ScratchPadBlob>(blobKey);
                    if (blob != null && !string.IsNullOrEmpty(blob.ImageBase64))
                    {
                        item.SetImageData(blob.ImageBase64, blob.ImageMimeType);
                        item.ImageLoadFailed = false;
                    }
                    else
                    {
                        // Blob not found - mark as failed so UI can show appropriate message
                        item.ImageLoadFailed = true;
                        Console.WriteLine($"Blob not found for item {item.Id} with key {blobKey}");
                    }
                }

                _loadedBlobsTopicId = topicId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadBlobsForTopicAsync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save an image blob to separate store
        /// </summary>
        private async Task SaveBlobAsync(int currentUserId, string topicId, string itemId, string imageBase64, string imageMimeType)
        {
            try
            {
                var blob = new ScratchPadBlob
                {
                    Key = ScratchPadBlob.MakeKey(CurrentUserId, topicId, itemId),
                    ImageBase64 = imageBase64,
                    ImageMimeType = imageMimeType,
                    Created = DateTime.UtcNow
                };
                await _idxdb.Save(blob);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveBlobAsync error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete an image blob
        /// </summary>
        private async Task DeleteBlobAsync(string topicId, string itemId)
        {
            try
            {
                var blobKey = ScratchPadBlob.MakeKey(CurrentUserId, topicId, itemId);
                await _idxdb.DeleteValue<ScratchPadBlob>(blobKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteBlobAsync error: {ex.Message}");
            }
        }

        #endregion

        #region Topic Operations

        public async Task<string?> FindTopicIdAsync(string? title = null,bool createIfMissing=false, string icon = "📋", string color = "#7FCDE6")
        {
            if (_data is not null && !string.IsNullOrWhiteSpace(title))
            {
                ScratchPadTopic? topic = null;
                topic = _data.Topics.Find(t => t.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase));
                
                if (topic is null) 
                    topic = _data.Topics.Find(t => t.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase));

                if (topic is null && !createIfMissing)
                    return null;

                if (topic is null && createIfMissing)
                    topic = await AddTopicAsync(title,icon,color);

                // okay mister
                if (!string.IsNullOrWhiteSpace(topic?.Id))
                    return topic.Id;

            }
            return null;
        }

        public async Task<ScratchPadTopic> AddTopicAsync(string title, string icon = "📋", string color = "#7FCDE6")
        {
            var topic = new ScratchPadTopic
            {
                Title = title,
                Icon = icon,
                Color = color,
                SortOrder = _data.Topics.Count
            };

            _data.Topics.Add(topic);
            _data.ActiveTopicId = topic.Id;

            await SaveAsync();
            NotifyStateChanged();

            return topic;
        }

        public async Task UpdateTopicAsync(string topicId, string? title = null, string? icon = null, string? color = null)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null) return;

            if (title != null) topic.Title = title;
            if (icon != null) topic.Icon = icon;
            if (color != null) topic.Color = color;
            topic.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        public async Task DeleteTopicAsync(string topicId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            if (topic == null) return;

            // Delete all blobs for items in this topic
            foreach (var item in topic.Items.Where(i => i.HasStoredImage))
            {
                await DeleteBlobAsync(topicId, item.Id);
            }

            _data.Topics.Remove(topic);

            // Select another topic if we deleted the active one
            if (_data.ActiveTopicId == topicId)
            {
                _data.ActiveTopicId = _data.Topics.FirstOrDefault()?.Id;
                _loadedBlobsTopicId = null; // Force reload
            }

            await SaveAsync();
            NotifyStateChanged();
        }

        public async Task ReorderTopicsAsync(List<string> topicIds)
        {
            for (int i = 0; i < topicIds.Count; i++)
            {
                var topic = _data.Topics.FirstOrDefault(t => t.Id == topicIds[i]);
                if (topic != null)
                {
                    topic.SortOrder = i;
                }
            }

            _data.Topics = _data.Topics.OrderBy(t => t.SortOrder).ToList();
            await SaveAsync();
            NotifyStateChanged();
        }

        #endregion

        #region Item Operations

        public async Task<ScratchPadItem> AddItemAsync(string? topicId = null, string content = "", ScratchPadItemType itemType = ScratchPadItemType.Note)
        {
            topicId ??= _data.ActiveTopicId ?? _data.Topics.FirstOrDefault()?.Id;
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);

            if (topic == null)
            {
                topic = await AddTopicAsync("Quick Notes");
            }

            var item = new ScratchPadItem
            {
                Content = content,
                ItemType = itemType,
                SortOrder = topic.Items.Count
            };

            topic.Items.Insert(0, item);
            topic.Modified = DateTime.UtcNow;

            for (int i = 0; i < topic.Items.Count; i++)
            {
                topic.Items[i].SortOrder = i;
            }

            await SaveAsync();
            NotifyStateChanged();

            return item;
        }

        /// <summary>
        /// Add a new item with an image - saves image to blob store separately
        /// </summary>
        public async Task<ScratchPadItem> AddItemWithImageAsync(string? topicId, string content, string imageBase64, string imageMimeType)
        {
            topicId ??= _data.ActiveTopicId ?? _data.Topics.FirstOrDefault()?.Id;
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);

            if (topic == null)
            {
                topic = await AddTopicAsync("Quick Notes");
                topicId = topic.Id;
            }

            var item = new ScratchPadItem
            {
                Content = content,
                HasStoredImage = true, // Flag for blob store
                ItemType = ScratchPadItemType.Note,
                SortOrder = topic.Items.Count
            };
            item.SetImageData(imageBase64, imageMimeType); // In-memory for immediate display

            topic.Items.Insert(0, item);
            topic.Modified = DateTime.UtcNow;

            for (int i = 0; i < topic.Items.Count; i++)
            {
                topic.Items[i].SortOrder = i;
            }

            // Save main data first (fast, no image)
            await SaveAsync();

            // Save image blob separately
            await SaveBlobAsync(CurrentUserId, topicId!, item.Id, imageBase64, imageMimeType);

            NotifyStateChanged();

            return item;
        }

        public async Task UpdateItemAsync(string topicId, string itemId, string? content = null, bool? isCompleted = null, bool? isPinned = null)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            if (content != null) item.Content = content;
            if (isCompleted.HasValue) item.IsCompleted = isCompleted.Value;
            if (isPinned.HasValue) item.IsPinned = isPinned.Value;
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            await SaveAsync(); // Fast - no image data
            // NO NotifyStateChanged - UI already shows user's changes
        }

        /// <summary>
        /// Update the item type (Note/Todo) without losing other data
        /// </summary>
        public async Task UpdateItemTypeAsync(string topicId, string itemId, ScratchPadItemType newType)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            item.ItemType = newType;
            if (newType == ScratchPadItemType.Note)
            {
                item.IsCompleted = false;
            }
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        /// <summary>
        /// Set or update the image on an existing item - saves to blob store
        /// </summary>
        public async Task SetItemImageAsync(string topicId, string itemId, string imageBase64, string imageMimeType)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            // Update in-memory
            item.SetImageData(imageBase64, imageMimeType);
            item.HasStoredImage = true;
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            // Save main data (just the flag)
            await SaveAsync();

            // Save blob separately
            await SaveBlobAsync(CurrentUserId, topicId, itemId, imageBase64, imageMimeType);

            NotifyStateChanged();
        }

        /// <summary>
        /// Remove the image from an item - deletes from blob store
        /// </summary>
        public async Task RemoveItemImageAsync(string topicId, string itemId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            // Clear in-memory
            item.ClearImageData();
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            // Save main data
            await SaveAsync();

            // Delete blob
            await DeleteBlobAsync(topicId, itemId);

            NotifyStateChanged();
        }

        public async Task DeleteItemAsync(string topicId, string itemId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            // Delete blob if exists
            if (item.HasStoredImage)
            {
                await DeleteBlobAsync(topicId, itemId);
            }

            topic!.Items.Remove(item);
            topic.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        public async Task ToggleItemCompletedAsync(string topicId, string itemId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            item.IsCompleted = !item.IsCompleted;
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        public async Task ToggleItemPinnedAsync(string topicId, string itemId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            item.IsPinned = !item.IsPinned;
            item.Modified = DateTime.UtcNow;
            topic!.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        public async Task MoveItemToTopicAsync(string sourceTopicId, string itemId, string targetTopicId)
        {
            var sourceTopic = _data.Topics.FirstOrDefault(t => t.Id == sourceTopicId);
            var targetTopic = _data.Topics.FirstOrDefault(t => t.Id == targetTopicId);
            var item = sourceTopic?.Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null || targetTopic == null) return;

            // If item has image, we need to move the blob too
            if (item.HasStoredImage && !string.IsNullOrEmpty(item.ImageBase64))
            {
                // Save to new location
                await SaveBlobAsync(CurrentUserId, targetTopicId, itemId, item.ImageBase64, item.ImageMimeType!);
                // Delete from old location
                await DeleteBlobAsync(sourceTopicId, itemId);
            }

            sourceTopic!.Items.Remove(item);
            targetTopic.Items.Insert(0, item);

            sourceTopic.Modified = DateTime.UtcNow;
            targetTopic.Modified = DateTime.UtcNow;

            await SaveAsync();
            NotifyStateChanged();
        }

        #endregion

        #region Clipboard Operations - Text

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                await _js.InvokeVoidAsync("navigator.clipboard.writeText", text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CopyToClipboard error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear the clipboard
        /// </summary>
        public async Task ClearClipboardAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("navigator.clipboard.writeText", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClearClipboard error: {ex.Message}");
            }
        }

        public async Task<string> GetClipboardTextAsync()
        {
            try
            {
                return await _js.InvokeAsync<string>("navigator.clipboard.readText");
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task PasteFromClipboardAsync(string? topicId = null)
        {
            var text = await GetClipboardTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                await AddItemAsync(topicId, text);
            }
        }

        #endregion

        #region Clipboard Operations - Images

        /// <summary>
        /// Read clipboard and return any text and/or image content
        /// </summary>
        public async Task<ClipboardContent> ReadClipboardAsync()
        {
            try
            {
                var result = await _js.InvokeAsync<ClipboardContent>("scratchPadClipboard.read");
                return result ?? new ClipboardContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReadClipboardAsync error: {ex.Message}");
                var text = await GetClipboardTextAsync();
                return new ClipboardContent { Text = text };
            }
        }

        /// <summary>
        /// Paste from clipboard - creates new item with text and/or image
        /// </summary>
        public async Task<ScratchPadItem?> PasteFromClipboardWithImageAsync(string? topicId = null)
        {
            var content = await ReadClipboardAsync();

            if (!content.HasText && !content.HasImage)
            {
                return null;
            }

            if (content.HasImage)
            {
                return await AddItemWithImageAsync(
                    topicId,
                    content.Text ?? "",
                    content.ImageBase64!,
                    content.ImageMimeType!
                );
            }
            else if (content.HasText)
            {
                return await AddItemAsync(topicId, content.Text!);
            }

            return null;
        }

        /// <summary>
        /// Paste image from clipboard into an existing item
        /// </summary>
        public async Task<bool> PasteImageIntoItemAsync(string topicId, string itemId)
        {
            var content = await ReadClipboardAsync();

            if (!content.HasImage)
            {
                return false;
            }

            await SetItemImageAsync(topicId, itemId, content.ImageBase64!, content.ImageMimeType!);
            return true;
        }

        /// <summary>
        /// Copy an image to the clipboard (returns "copied", "shared", or null on failure)
        /// </summary>
        public async Task<string?> CopyImageToClipboardAsync(string imageBase64, string imageMimeType)
        {
            try
            {
                var result = await _js.InvokeAsync<string>("scratchPadClipboard.writeImage", imageBase64, imageMimeType);
                return result == "failed" ? null : result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CopyImageToClipboard error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Copy item's image to clipboard (returns "copied", "shared", or null on failure)
        /// </summary>
        public async Task<string?> CopyItemImageAsync(string topicId, string itemId)
        {
            var topic = _data.Topics.FirstOrDefault(t => t.Id == topicId);
            var item = topic?.Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null) return null;

            // If image not in memory, load it
            if (item.HasStoredImage && string.IsNullOrEmpty(item.ImageBase64))
            {
                var blobKey = ScratchPadBlob.MakeKey(CurrentUserId, topicId, itemId);
                var blob = await _idxdb.GetValueByKey<ScratchPadBlob>(blobKey);
                if (blob != null)
                {
                    item.SetImageData(blob.ImageBase64, blob.ImageMimeType);
                }
            }

            if (string.IsNullOrEmpty(item.ImageBase64))
            {
                return null;
            }

            return await CopyImageToClipboardAsync(item.ImageBase64, item.ImageMimeType!);
        }

        #endregion

        #region Utility

        public async Task ClearAllDataAsync()
        {
            if (_data is null)
                return;

            // Delete all blobs
            foreach (var topic in _data.Topics)
            {
                foreach (var item in topic.Items.Where(i => i.HasStoredImage))
                {
                    await DeleteBlobAsync(topic.Id, item.Id);
                }
            }

            _data = new ScratchPadData();
            _loadedBlobsTopicId = null;
            await SaveAsync();
            NotifyStateChanged();
        }

        public int GetTotalItemCount()
        {
            if (_data is null)
                return 0;
            return _data.Topics.Sum(t => t.Items.Count);
        }

        public int GetPendingItemCount()
        {
            if (_data is null)
                return 0;

            return _data.Topics.Sum(t => t.Items.Count(i => !i.IsCompleted && i.ItemType == ScratchPadItemType.Todo));
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        #endregion
    }
}
