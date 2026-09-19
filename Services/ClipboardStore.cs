using System.Text.RegularExpressions;
using AvaloniaDCT.Models;

namespace AvaloniaDCT.Services;

public sealed class ClipboardStore
{
    private static readonly Regex WebUrlRegex = new(
        @"^(http|http(s)?://)?([\w-]+\.)+[\w-]+[.com|.in|.org]+(\[\?%&=]*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly List<TagItemValue> _items = [];

    public IReadOnlyList<TagItemValue> Items => _items;

    public void Load(IEnumerable<TagItemValue> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public IReadOnlyList<string> GetTags()
    {
        return _items
            .Select(item => item.Tag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetItemsForTag(string tag)
    {
        return _items
            .Where(item => item.Tag == tag && !string.IsNullOrEmpty(item.Item))
            .Select(item => item.Item)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string GetValue(string tag, string item)
    {
        return _items.FirstOrDefault(row => row.Tag == tag && row.Item == item)?.Value ?? "";
    }

    public bool TagExists(string tag) =>
        _items.Any(item => item.Tag == tag);

    public bool ItemExists(string tag, string item) =>
        _items.Any(row => row.Tag == tag && row.Item == item);

    public void AddTag(string tag, int ownerId)
    {
        if (TagExists(tag))
        {
            return;
        }

        _items.Add(new TagItemValue
        {
            Tag = tag,
            Item = "",
            Value = "",
            OwnerId = ownerId
        });
    }

    public void RenameTag(string oldTag, string newTag)
    {
        foreach (var item in _items.Where(row => row.Tag == oldTag))
        {
            item.Tag = newTag;
        }
    }

    public void DeleteTag(string tag)
    {
        _items.RemoveAll(item => item.Tag == tag);
    }

    public void AddItem(string tag, string item, string value, int ownerId)
    {
        var placeholder = _items.FirstOrDefault(row => row.Tag == tag && string.IsNullOrEmpty(row.Item));
        if (placeholder is not null)
        {
            placeholder.Item = item;
            placeholder.Value = value;
            return;
        }

        _items.Add(new TagItemValue
        {
            Tag = tag,
            Item = item,
            Value = value,
            OwnerId = ownerId
        });
    }

    public void RenameItem(string tag, string oldItem, string newItem)
    {
        var row = _items.FirstOrDefault(item => item.Tag == tag && item.Item == oldItem);
        if (row is not null)
        {
            row.Item = newItem;
        }
    }

    public void UpdateValue(string tag, string item, string value)
    {
        var row = _items.FirstOrDefault(entry => entry.Tag == tag && entry.Item == item);
        if (row is not null)
        {
            row.Value = value;
        }
    }

    public void DeleteItem(string tag, string item)
    {
        var ownerId = _items.FirstOrDefault(row => row.Tag == tag)?.OwnerId ?? 0;
        _items.RemoveAll(row => row.Tag == tag && row.Item == item);
        if (!_items.Any(row => row.Tag == tag))
        {
            _items.Add(new TagItemValue { Tag = tag, Item = "", Value = "", OwnerId = ownerId });
        }
    }

    public static bool IsWebUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return WebUrlRegex.IsMatch(value.Trim());
    }

    public static string ToBrowsableUrl(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return "https://" + trimmed;
    }
}
