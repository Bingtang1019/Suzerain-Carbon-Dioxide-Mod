using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace OiVerify
{
    internal sealed class RequestArgs
    {
        private readonly Dictionary<string, JsonElement> _values =
            new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        internal static RequestArgs From(JsonElement root)
        {
            var args = new RequestArgs();
            JsonElement obj;
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("args", out obj)
                && obj.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in obj.EnumerateObject())
                    args._values[p.Name] = p.Value.Clone();
            }
            return args;
        }

        internal string GetString(string name, string fallback = "")
        {
            JsonElement e;
            if (!_values.TryGetValue(name, out e)) return fallback;
            if (e.ValueKind == JsonValueKind.String) return e.GetString();
            if (e.ValueKind == JsonValueKind.Null) return fallback;
            return e.GetRawText();
        }

        internal int GetInt(string name, int fallback)
        {
            JsonElement e;
            if (!_values.TryGetValue(name, out e)) return fallback;
            int value;
            if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out value)) return value;
            if (e.ValueKind == JsonValueKind.String
                && int.TryParse(e.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
            return fallback;
        }

        internal bool GetBool(string name, bool fallback)
        {
            JsonElement e;
            if (!_values.TryGetValue(name, out e)) return fallback;
            if (e.ValueKind == JsonValueKind.True) return true;
            if (e.ValueKind == JsonValueKind.False) return false;
            bool value;
            if (e.ValueKind == JsonValueKind.String
                && bool.TryParse(e.GetString(), out value))
                return value;
            return fallback;
        }

        internal string[] GetStringArray(string name)
        {
            JsonElement e;
            if (!_values.TryGetValue(name, out e) || e.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            var list = new List<string>();
            foreach (var item in e.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    list.Add(item.GetString());
            }
            return list.ToArray();
        }
    }
}
