using System;
using System.Collections.Generic;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class CompanyNodeMap
    {
        private static readonly Dictionary<string, string> Map =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                
                { "0x010000000000C3A3", "H-3" },
                { "0x010000000000C3C4", "H-3" },
                { "0x0100000100000F2C", "H-3" },
                { "0x0100000100000F4A", "H-3" },

                
                { "0x010000000000BBCA", "L-1" },
                { "0x010000000000BBEE", "L-1" },
                { "0x010000000000BEB7", "L-1" },
                { "0x0100000100000F64", "L-1" },
                { "0x0100000100000F6A", "L-1" },
                { "0x0100000100000F74", "L-1" },
            };

        internal static int Count { get { return Map.Count; } }

        internal static bool TryGet(string nodeId, out string cluster)
        {
            cluster = null;
            if (string.IsNullOrEmpty(nodeId)) return false;
            return Map.TryGetValue(nodeId, out cluster);
        }
    }
}
