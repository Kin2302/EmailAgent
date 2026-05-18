using System.Collections.Concurrent;

namespace EmailAgent.Core.Services
{
    /// <summary>
    /// Bộ nhớ tạm lưu nội dung lớn (HTML, raw data) tránh chảy qua LLM.
    /// Plugin lưu data → trả ID ngắn cho AI → Plugin khác lấy data bằng ID.
    /// </summary>
    public static class ContentStore
    {
        private static readonly ConcurrentDictionary<string, StoredContent> _store = new();

        /// <summary>
        /// Lưu nội dung, trả về ID ngắn.
        /// </summary>
        public static string Save(string content, string tag = "content")
        {
            var id = $"{tag}_{DateTime.Now:HHmmss}_{Guid.NewGuid().ToString()[..4]}";
            _store[id] = new StoredContent
            {
                Content = content,
                CreatedAt = DateTime.Now,
                Tag = tag
            };
            return id;
        }

        /// <summary>
        /// Lấy nội dung theo ID.
        /// </summary>
        public static string? Get(string id)
        {
            return _store.TryGetValue(id, out var item) ? item.Content : null;
        }

        /// <summary>
        /// Lấy nội dung mới nhất có tag bắt đầu bằng prefix.
        /// VD: GetLatestByPrefix("csv_raw") → lấy raw CSV data mới nhất.
        /// </summary>
        public static string? GetLatestByPrefix(string prefix)
        {
            var latest = _store
                .Where(kv => kv.Value.Tag == prefix)
                .OrderByDescending(kv => kv.Value.CreatedAt)
                .FirstOrDefault();

            return latest.Value?.Content;
        }

        /// <summary>
        /// Xóa nội dung theo ID.
        /// </summary>
        public static void Remove(string id)
        {
            _store.TryRemove(id, out _);
        }

        private class StoredContent
        {
            public string Content { get; set; } = "";
            public string Tag { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}
