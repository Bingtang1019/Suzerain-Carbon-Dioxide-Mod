using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static partial class EconomyPatches
    {
        

    
    
    
        

        private static void PersistSnapshot()
        {
            try
            {
                var dir = Path.GetDirectoryName(SnapshotPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var sb = new StringBuilder();
                sb.Append("{\"turn\":").Append(_snapTurn);
                sb.Append(",\"campaignHash\":").Append(_snapCampaignHash);
                sb.Append(",\"tiers\":[").Append(string.Join(",", Array.ConvertAll(PrevTier, x => x.ToString(CultureInfo.InvariantCulture)))).Append(']');
                sb.Append(",\"taxAvoid\":").Append(PrevTaxAvoid ? "true" : "false");
                sb.Append(",\"edgeVer\":").Append(EdgeLayoutVersion);
                sb.Append(",\"edges\":[");
                for (int i = 0; i < PrevEdge.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(PrevEdge[i] ? "true" : "false");
                }
                sb.Append("]}");
                var tmp = SnapshotPath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(SnapshotPath)) File.Delete(SnapshotPath);
                File.Move(tmp, SnapshotPath);
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][economy] 快照写入失败：" + e.Message);
            }
        }

        private static void LoadSnapshot()
        {
            try
            {
                if (!File.Exists(SnapshotPath)) return;
                var raw = File.ReadAllText(SnapshotPath, Encoding.UTF8);
                _snapTurn = ParseInt(raw, "turn", -1);
                _snapCampaignHash = ParseInt(raw, "campaignHash", 0);
                var tiers = ParseInts(raw, "tiers", 4);
                if (tiers != null) for (int i = 0; i < 4 && i < tiers.Length; i++) PrevTier[i] = tiers[i];
                PrevTaxAvoid = ParseBool(raw, "taxAvoid", false);
                
                
                int edgeVer = ParseInt(raw, "edgeVer", -1);
                var edges = ParseBools(raw, "edges", PrevEdge.Length);
                bool legacyGrowth = false;
                if (edges == null) { edges = ParseBools(raw, "growth", PrevEdge.Length); legacyGrowth = true; }
                if (edges != null && edgeVer == EdgeLayoutVersion)
                {
                    
                    for (int i = 0; i < PrevEdge.Length; i++) PrevEdge[i] = edges[i];
                    _edgeLoaded = PrevEdge.Length;
                    _edgeShort = false;
                }
                else if (edges != null && legacyGrowth)
                {
                    
                    
                    for (int i = 0; i < 7 && i < PrevEdge.Length; i++) PrevEdge[i] = edges[i];
                    _edgeLoaded = 7;
                    _edgeShort = true;
                }
                else
                {
                    
                    _edgeLoaded = 0;
                    _edgeShort = true;
                }
                _hasSnapshot = _snapTurn >= 0;
                _lastSettledTurn = _snapTurn;
                _log.LogInfo("[OI][economy] 已载入词条快照：turn=" + _snapTurn
                    + "，地区=" + TierString(PrevTier) + "，避税=" + PrevTaxAvoid
                    + "，一次性词条=" + CountTrue(PrevEdge) + "/" + PrevEdge.Length + " 个"
                    + (_edgeShort ? "（布局已变更或为旧快照 → 本期只建基线，不结算）" : ""));
            }
            catch (Exception e)
            {
                _log.LogWarning("[OI][economy] 快照读取失败（将重建基线）：" + e.Message);
                _hasSnapshot = false;
            }
        }

        private static int CountTrue(bool[] arr)
        {
            int n = 0;
            foreach (var b in arr) if (b) n++;
            return n;
        }

        private static int ParseInt(string raw, string key, int dflt)
        {
            var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out var v) ? v : dflt;
        }

        private static int[] ParseInts(string raw, string key, int count)
        {
            var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!m.Success) return null;
            var parts = m.Groups[1].Value.Split(',');
            var arr = new int[count];
            for (int i = 0; i < count; i++)
                arr[i] = i < parts.Length && int.TryParse(parts[i].Trim(), out var v) ? v : -1;
            return arr;
        }

        private static bool ParseBool(string raw, string key, bool dflt)
        {
            var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*(true|false)");
            return m.Success ? m.Groups[1].Value == "true" : dflt;
        }

        private static bool[] ParseBools(string raw, string key, int count)
        {
            var m = Regex.Match(raw, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!m.Success) return null;
            var parts = m.Groups[1].Value.Split(',');
            var arr = new bool[count];
            for (int i = 0; i < count; i++)
                arr[i] = i < parts.Length && parts[i].Trim() == "true";
            return arr;
    }
}
}
