using System.Reflection;

namespace LabSyncBackbone.Helpers
{
    public static class FieldFilter
    {
        // Takes any object + a comma-separated fields string
        // Returns a dictionary with only the requested properties
        // If fields is null/empty → returns null (caller should return the full object)
        public static Dictionary<string, object?>? Apply(object source, string? fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
                return null;

            // Split "RecordId,ExternalStatus,Payload" → ["RecordId", "ExternalStatus", "Payload"]
            var requested = fields
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object?>();

            // Loop over all public properties of the object
            foreach (var prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // If caller asked for this property (case-insensitive), include it
                if (requested.Contains(prop.Name))
                {
                    result[prop.Name] = prop.GetValue(source);
                }
            }

            return result;
        }
    }
}
