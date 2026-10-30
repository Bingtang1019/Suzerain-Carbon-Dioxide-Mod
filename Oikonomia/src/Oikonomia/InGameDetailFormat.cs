using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static partial class InGameDetailFormat
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        
        
        
        
        public const int StyleRizia = 0;

        
        
        
        
        
        
        
        public const int StyleDetailed = 1;

        
        public const string StyleNameRizia = "Rizia";

        
        public const string StyleNameDetailed = "Detailed";

        
        
        
        
        
        
        
        
        
        public static int ParseStyle(string s)
        {
            return string.Equals((s ?? "").Trim(), StyleNameDetailed, StringComparison.OrdinalIgnoreCase)
                ? StyleDetailed : StyleRizia;
        }

        
        
        public const int KindPerTurn = 0;

        
        public const int KindOneTime = 1;

        
        public const string OneTimeSuffix = "(一次性)";

        
        public const string PerTurnSuffix = "(每回合)";

        
        
        
        
        
        
        
        
        public const string SectionLabelPerTurn = "【未来】";

        
        
        
        
        public const string SectionLabelOneTime = "【当前】";

        
        public const string SubtotalLabel = "小计";

        
        
        
        
        
        
        
        
        
        
        
        
        public const string TotalLabel = "Total:";

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public const string PreviewTitle = "当前预测下的财政状态";

        
        
        
        
        
        
        
        
        
        
        
        public const string TotalDivider = "──────";

        
        public const string NoChangeText = "本回合无变动";

        
        public const string GroupExtraWelfare = "额外福利支出";

        
        
        
        

        
        public const string PositiveColor = "#6FBF52";

        
        public const string NegativeColor = "#D9534F";

        
        public const string ZeroColor = "#9A9A9A";

        
        public const char LineSeparator = '\n';

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        public const string SubtotalLead = "── ";

        
        public const string BoldOpen = "<b>";

        
        public const string BoldClose = "</b>";

        
        public const string UnderlineOpen = "<u>";

        
        public const string UnderlineClose = "</u>";

        
        public const string AlignCenterOpen = "<align=center>";

        
        public const string AlignCenterClose = "</align>";

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        public const char FrameHorizontal = '-';

        
        public const char FrameVertical = '|';

        
        
        
        
        
        
        
        
        public const char FrameCorner = '+';

        
        public const int FramePadLeft = 1;

        
        public const int FramePadRight = 1;

        
        
        
        
        
        public const int FrameBodyIndent = 2;

        
        
        
        
        public struct Entry
        {
            
            public string Text;

            
            public int Delta;

            
            public int Kind;

            
            public string Group;

            
            
            
            
            
            
            
            
            
            public Entry(string text, int delta) : this(text, delta, InferKind(text), null) { }

            
            public Entry(string text, int delta, int kind, string group)
            {
                Text = text ?? "";
                Delta = delta;
                Kind = kind == KindOneTime ? KindOneTime : KindPerTurn;
                Group = group ?? "";
            }

            
            private static int InferKind(string text)
            {
                return EndsWith(text, OneTimeSuffix) ? KindOneTime : KindPerTurn;
            }
        }

        
        
        
        private static readonly Dictionary<string, string> LabelMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            
            { "ED=危险",        "经济状态濒临崩溃" },
            { "ED=不稳定",      "经济状态动荡不安" },
            { "ED=稳定",        "经济状态保持稳定" },
            { "ED=增长",        "经济状态日益增长" },
            { "ED=极快增长",    "经济状态高速增长" },
            { "旅游业=蓬勃发展", "全国旅游业蓬勃发展" },
            { "旅游业=平均水平", "全国旅游业平稳发展" },
            { "旅游业=严重衰退", "全国旅游业持续衰退" },
            { "贸易量=增加",    "对外贸易额显著增加" },
            { "贸易量=恢复",    "对外贸易额逐步恢复" },
            { "贸易量=减少",    "对外贸易额出现萎缩" },
            { "税效率",         "税收征管效率提升" },
            { "税务效率",       "税收征管效率提升" },
            { "逃税",           "逃税现象造成财政损失" },
            { "腐败",           "腐败侵蚀行政效率" },
            
            { "免费处方药",     GroupExtraWelfare },
            { "福利支出",       GroupExtraWelfare },

            
            { "避税",           "资本避税问题" },   
            { "避税(一次性)",   "资本避税问题" },   
            { "高就业",         "就业率显著提高" },
            { "产量增加",       "工业产量增加" },
            { "小企业成长",     "中小企业蓬勃发展" },
            { "大企业创造就业", "大型企业创造就业" },
            { "军工复合体",     "军工复合体扩张" },
            { "阿卡西亚投资",   "阿卡西亚投资涌入" },
            { "康塔纳投资",     "康塔纳投资涌入" },
        };

        
        private static readonly Dictionary<string, string> TradeSpecialMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "韦伦·石油自由贸易",   "与韦伦达成石油自由贸易" },
            { "阿格诺利亚·农业市场", "与阿格诺利亚开放农业市场" },
            { "伦堡·天然气管道",     "与伦堡签署天然气管道协议" },
        };

        
        private static readonly string[] TradeTopicNoSuffix = { "自由贸易区", "管道", "市场", "石油出售" };

        
        public static bool IsKnownGroup(string group)
        {
            return !string.IsNullOrEmpty(group) && string.Equals(group, GroupExtraWelfare, StringComparison.Ordinal);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        
        
        
        
        public const string DetailGap = "\n\n";

        
        
        
        
        public static readonly string[] VisibleMarkerTokens = { "<!--", "-->", "OI-DETAIL" };

        
        
        
        
        
        
        
        
        public static string MergeDetail(string original, string detail)
        {
            var keep = original ?? "";
            if (string.IsNullOrEmpty(detail)) return keep;
            return keep + DetailGap + detail;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        public sealed class TruncationRecord
        {
            public string CapName;      
            public int CapValue;        
            public int Written;         
            public int Dropped;         
            public string Reason;       
            public string ObservedAt;   
            public string FixHint;      
        }

        private static readonly Dictionary<string, TruncationRecord> _truncations =
            new Dictionary<string, TruncationRecord>(StringComparer.Ordinal);

        
        public static TruncationRecord GetTruncation(string capName)
        {
            TruncationRecord r;
            return (capName != null && _truncations.TryGetValue(capName, out r)) ? r : null;
        }

        
        public static int TruncationCount { get { return _truncations.Count; } }

        
        public static void ClearTruncations() { _truncations.Clear(); }

        
        
        
        
        
        
        
        public static void NoteTruncation(string capName, int capValue, int written, int dropped,
            string reason, string fixHint)
        {
            if (string.IsNullOrEmpty(capName)) return;
            if (dropped <= 0) return;   

            var r = new TruncationRecord
            {
                CapName = capName,
                CapValue = capValue,
                Written = written,
                Dropped = dropped,
                Reason = reason ?? "",
                ObservedAt = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                FixHint = fixHint ?? ""
            };
            _truncations[capName] = r;   
        }

        
        
        
        
        public static string DescribeTruncations()
        {
            if (_truncations.Count == 0) return "";
            var keys = new List<string>(_truncations.Keys);
            keys.Sort(StringComparer.Ordinal);   

            var sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                var r = _truncations[keys[i]];
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("[OI][上限] 上限名=").Append(r.CapName)
                  .Append(" 上限值=").Append(r.CapValue.ToString(CultureInfo.InvariantCulture))
                  .Append(" 实际写入数=").Append(r.Written.ToString(CultureInfo.InvariantCulture))
                  .Append(" 丢弃数=").Append(r.Dropped.ToString(CultureInfo.InvariantCulture))
                  .Append(" 丢弃原因=").Append(r.Reason)
                  .Append(" 时点=").Append(r.ObservedAt);
                if (!string.IsNullOrEmpty(r.FixHint)) sb.Append(" 调整处=").Append(r.FixHint);
            }

            
            
            
            string why;
            if (ContainsVisibleMarker(sb.ToString(), out why)) return "";
            return sb.ToString();
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        
        
        
        public const int MaxDiagNames = 3;

        
        
        
        
        
        
        
        public const string NoDiagReportedLine = "模组自检：本会话未上报任何诊断点";

        public static string BuildDiagLine(int total, int ok, IList<string> problemNames)
        {
            
            
            
            
            
            if (total == 0) return NoDiagReportedLine;
            
            if (ok >= total) return "";

            var sb = new StringBuilder();
            sb.Append("模组自检 ").Append(ok).Append("/").Append(total).Append(" 挂载成功");

            int shown = 0;
            if (problemNames != null)
            {
                
                
                
                int real = 0;
                for (int i = 0; i < problemNames.Count; i++)
                    if (!string.IsNullOrEmpty(problemNames[i])) real++;

                for (int i = 0; i < problemNames.Count && shown < MaxDiagNames; i++)
                {
                    string nm = problemNames[i];
                    if (string.IsNullOrEmpty(nm)) continue;
                    sb.Append(shown == 0 ? " ｜ 未挂：" : "、").Append(nm);
                    shown++;
                }
                int rest = real - shown;      
                if (rest > 0) sb.Append(" 等 ").Append(rest).Append(" 项");
            }

            
            
            string why;
            if (ContainsVisibleMarker(sb.ToString(), out why)) return "";
            return sb.ToString();
        }
        
        
        
        
        
        
        
        public static bool ContainsVisibleMarker(string s, out string why)
        {
            why = null;
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < VisibleMarkerTokens.Length; i++)
            {
                var t = VisibleMarkerTokens[i];
                int at = s.IndexOf(t, StringComparison.Ordinal);
                if (at >= 0) { why = "位置 " + at + " 出现可见标记 " + t; return true; }
            }
            return false;
        }

        
        public static int CountOf(string s, string needle)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
            int n = 0, i = 0;
            while (true)
            {
                int hit = s.IndexOf(needle, i, StringComparison.Ordinal);
                if (hit < 0) return n;
                n++;
                i = hit + needle.Length;
            }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public sealed class DetailMergeMemory
        {
            
            public const int Capacity = 8;

            private readonly string[] _bases = new string[Capacity];
            private readonly string[] _mergeds = new string[Capacity];
            private object _owner;
            private int _count;
            private int _next;
            private int _merges;
            private int _hits;

            
            public object Owner { get { return _owner; } }

            
            public string LastBase { get; private set; }

            
            public string LastMerged { get; private set; }

            
            public int Merges { get { return _merges; } }

            
            public int FingerprintHits { get { return _hits; } }

            
            public int HistoryCount { get { return _count; } }

            
            public void Reset(object owner)
            {
                _owner = owner;
                _count = 0;
                _next = 0;
                LastBase = null;
                LastMerged = null;
                for (int i = 0; i < Capacity; i++) { _bases[i] = null; _mergeds[i] = null; }
            }

            
            public bool MatchesMerged(string s)
            {
                if (s == null) return false;
                for (int i = 0; i < _count; i++)
                    if (string.Equals(_mergeds[i], s, StringComparison.Ordinal)) return true;
                return false;
            }

            
            
            
            
            public string BaseOf(string s)
            {
                if (s == null) return "";
                for (int i = 0; i < _count; i++)
                    if (string.Equals(_mergeds[i], s, StringComparison.Ordinal)) return _bases[i] ?? "";
                return s;
            }

            
            
            
            
            
            
            
            
            
            
            
            public string Merge(object owner, string incoming, string detail, out bool fingerprintHit)
            {
                incoming = incoming ?? "";
                if (!ReferenceEquals(_owner, owner)) Reset(owner);

                if (string.IsNullOrEmpty(detail))
                {
                    fingerprintHit = false;
                    return incoming;
                }

                fingerprintHit = MatchesMerged(incoming);
                var baseText = fingerprintHit ? BaseOf(incoming) : incoming;
                var merged = MergeDetail(baseText, detail);

                LastBase = baseText;
                LastMerged = merged;
                _merges++;
                if (fingerprintHit) _hits++;

                _bases[_next] = baseText;
                _mergeds[_next] = merged;
                _next = (_next + 1) % Capacity;
                if (_count < Capacity) _count++;

                return merged;
            }
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public struct DetailCacheKey
        {
            
            public int Rev;
            
            public int Style;
            
            public bool Frame;
            
            public bool Preview;
            
            public bool HasSettle;
            
            public string PosHex;
            
            public string NegHex;

            
            public bool Matches(DetailCacheKey o)
            {
                return Rev == o.Rev && Style == o.Style && Frame == o.Frame
                    && Preview == o.Preview && HasSettle == o.HasSettle
                    && string.Equals(PosHex, o.PosHex, StringComparison.Ordinal)
                    && string.Equals(NegHex, o.NegHex, StringComparison.Ordinal);
            }
        }

        
        
        
        
        public static bool IsMarkerFree(string s, out string why)
        {
            if (ContainsVisibleMarker(s, out why)) return false;
            why = "整段文本零标记字符";
            return true;
        }

        
        
        
        
        
        
        
        
        
        
        public static bool RichTagsClosed(string s, out string why)
        {
            why = null;
            if (string.IsNullOrEmpty(s)) return true;

            var stack = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                int lt = s.IndexOf('<', i);
                if (lt < 0) break;

                int gt = s.IndexOf('>', lt + 1);
                if (gt < 0) { why = "位置 " + lt + " 的 '<' 没有配对的 '>'"; return false; }

                var tag = s.Substring(lt, gt - lt + 1);

                bool closing;
                var name = TagName(tag, out closing);
                if (name == null) { why = "未识别的标签 " + tag; return false; }

                if (closing)
                {
                    if (stack.Count == 0) { why = "多余的闭标签 " + tag; return false; }
                    var open = stack[stack.Count - 1];
                    if (!string.Equals(open, name, StringComparison.Ordinal))
                    {
                        why = "标签交错：<" + open + "> 未闭合就出现 " + tag;
                        return false;
                    }
                    stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    stack.Add(name);
                }

                i = gt + 1;
            }

            if (stack.Count != 0)
            {
                why = stack.Count + " 个标签未闭合（最内层 <" + stack[stack.Count - 1] + ">）";
                return false;
            }
            return true;
        }

        
        
        
        
        
        
        
        
        
        
        private static string TagName(string tag, out bool closing)
        {
            closing = false;
            if (string.IsNullOrEmpty(tag) || tag[0] != '<' || tag[tag.Length - 1] != '>') return null;

            var body = tag.Substring(1, tag.Length - 2);

            if (body.StartsWith("/", StringComparison.Ordinal))
            {
                closing = true;
                body = body.Substring(1);
                if (body == "color" || body == "b" || body == "u" || body == "align" || body == "indent")
                    return body;
                return null;
            }

            if (body.StartsWith("color=", StringComparison.Ordinal)) return "color";
            if (body == "b") return "b";
            if (body == "u") return "u";
            if (body == "align=center") return "align";
            if (body.StartsWith("indent=", StringComparison.Ordinal)) return "indent";
            return null;
        }

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string Compose(IList<Entry> entries, int total, bool hasSettleData)
        {
            return Compose(entries, total, hasSettleData, null, null, false);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string Compose(IList<Entry> entries, int total, bool hasSettleData,
                                     string title, string totalLabel)
        {
            return Compose(entries, total, hasSettleData, title, totalLabel, false);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string Compose(IList<Entry> entries, int total, bool hasSettleData,
                                     string title, string totalLabel, bool frame)
        {
            return Compose(entries, total, hasSettleData, title, totalLabel, frame, StyleDetailed);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string Compose(IList<Entry> entries, int total, bool hasSettleData,
                                     string title, string totalLabel, bool frame, int style)
        {
            return Compose(entries, total, hasSettleData, title, totalLabel, frame, style, null, null);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string Compose(IList<Entry> entries, int total, bool hasSettleData,
                                     string title, string totalLabel, bool frame, int style,
                                     string positiveHex, string negativeHex)
        {
            
            if (!hasSettleData) return "";

            var rows = BuildRows(entries);

            var perTurn = new List<Row>();
            var oneTime = new List<Row>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Kind == KindOneTime) oneTime.Add(rows[i]);
                else perTurn.Add(rows[i]);
            }

            
            
            
            
            if (perTurn.Count == 0 && oneTime.Count == 0 && total == 0)
                return Tint(ZeroColor, NoChangeText);

            
            
            
            
            
            
            
            var lines = new List<FrameRow>();

            if (style == StyleRizia)
            {
                
                
                
                
                
                
                
                
                
                
                
                
                
                var rizia = EmitRiziaGroups(perTurn, oneTime, RiziaPalette.Of(positiveHex, negativeHex), title);
                if (rizia != null) return rizia;
                return Tint(ZeroColor, NoChangeText);
            }

            
            
            
            if (!string.IsNullOrEmpty(title))
            {
                lines.Add(MakeTitleRow(title));
                lines.Add(MakeRuleRow());
            }

            
            
            
            AppendSection(lines, SectionLabelPerTurn, perTurn, false);
            AppendSection(lines, SectionLabelOneTime, oneTime, perTurn.Count > 0);

            
            
            
            
            

            return frame ? EmitFramed(lines) : EmitFlat(lines);
        }

        
        public static List<Row> BuildRows(IList<Entry> entries)
        {
            
            var kept = new List<Entry>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (string.IsNullOrEmpty(e.Text)) continue;      
                    kept.Add(e);
                }
            }

            
            var firstIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            var byGroup = new Dictionary<string, Row>(StringComparer.Ordinal);
            var ungrouped = new Row?[kept.Count];

            for (int i = 0; i < kept.Count; i++)
            {
                var e = kept[i];
                if (string.IsNullOrEmpty(e.Group))
                {
                    ungrouped[i] = MakeRow(e.Text, e.Delta, e.Kind, null);
                    continue;
                }

                Row g;
                if (byGroup.TryGetValue(e.Group, out g))
                {
                    g.Delta += e.Delta;
                    byGroup[e.Group] = g;
                }
                else
                {
                    byGroup[e.Group] = MakeRow(e.Group, e.Delta, e.Kind, e.Group);
                    firstIndex[e.Group] = i;
                }
            }

            
            var merged = new List<Row>();
            for (int i = 0; i < kept.Count; i++)
            {
                var e = kept[i];
                if (string.IsNullOrEmpty(e.Group))
                {
                    merged.Add(ungrouped[i].Value);
                    continue;
                }
                if (firstIndex[e.Group] != i) continue;              
                merged.Add(byGroup[e.Group]);
            }

            
            
            var rows = new List<Row>();
            for (int i = 0; i < merged.Count; i++)
            {
                var r = merged[i];
                if (r.Delta == 0 && (!string.IsNullOrEmpty(r.Group) || r.Kind == KindOneTime)) continue;
                rows.Add(r);
            }

            return rows;
        }

        
        public struct Row
        {
            
            public string Label;
            
            public int Delta;
            
            public int Kind;
            
            public string Group;
        }

        private static Row MakeRow(string text, int delta, int kind, string group)
        {
            var r = new Row();
            r.Label = Display(text);
            r.Delta = delta;
            r.Kind = kind;
            r.Group = group ?? "";
            return r;
        }

        

        
        
        
        
        

        
        private const int RowTitle = 0;
        
        private const int RowRule = 1;
        
        private const int RowHeader = 2;
        
        private const int RowBody = 3;
        
        private const int RowBlank = 4;

        
        
        
        
        
        
        
        
        
        
        
        
        private struct FrameRow
        {
            
            public int Kind;
            
            public string Plain;
            
            public string Inner;
            
            public string Color;
            
            public string Indent;
        }

        private static FrameRow MakeTitleRow(string title)
        {
            var r = new FrameRow();
            r.Kind = RowTitle;
            r.Plain = title ?? "";
            r.Inner = Escape(title);
            r.Color = ZeroColor;
            r.Indent = "";
            return r;
        }

        private static FrameRow MakeRuleRow()
        {
            var r = new FrameRow();
            r.Kind = RowRule;
            r.Plain = TotalDivider;
            r.Inner = TotalDivider;
            r.Color = ZeroColor;
            r.Indent = "";
            return r;
        }

        private static FrameRow MakeBlankRow()
        {
            var r = new FrameRow();
            r.Kind = RowBlank;
            r.Plain = "";
            r.Inner = "";
            r.Color = ZeroColor;
            r.Indent = "";
            return r;
        }

        private static FrameRow MakeHeaderRow(string header)
        {
            var r = new FrameRow();
            r.Kind = RowHeader;
            r.Plain = header ?? "";
            r.Inner = Escape(header);
            r.Color = ZeroColor;
            r.Indent = "";
            return r;
        }

        
        private static FrameRow MakeBodyRow(string indent, string plain, string inner, string color)
        {
            var r = new FrameRow();
            r.Kind = RowBody;
            r.Plain = indent + plain;
            r.Inner = inner;
            r.Color = color;
            r.Indent = indent;
            return r;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static void AppendSection(List<FrameRow> lines, string header, List<Row> rows, bool leadingBlank)
        {
            if (rows.Count == 0) return;      

            
            
            if (leadingBlank) lines.Add(MakeBlankRow());

            lines.Add(MakeHeaderRow(header));

            AppendBodyRows(lines, rows);
            AppendSubtotalRow(lines, rows);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        private static void AppendBodyRows(List<FrameRow> lines, List<Row> rows)
        {
            var indent = new string(' ', FrameBodyIndent);

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];

                string val;
                string plainTxt;              
                string richTxt;               
                string color;
                if (r.Delta == 0)
                {
                    
                    
                    
                    
                    
                    val = Signed(0);
                    plainTxt = r.Label + " " + NoChangeText;
                    richTxt = Escape(r.Label) + " " + Escape(NoChangeText);
                    color = ZeroColor;
                }
                else
                {
                    val = Signed(r.Delta);
                    plainTxt = r.Label;
                    richTxt = Escape(r.Label);
                    color = DeltaColor(r.Delta);
                }

                
                lines.Add(MakeBodyRow(indent,
                    ParenValue(val, plainTxt),
                    ParenValue(val, richTxt),
                    color));
            }
        }

        
        
        
        
        private static void AppendSubtotalRow(List<FrameRow> lines, List<Row> rows)
        {
            var indent = new string(' ', FrameBodyIndent);

            int sub = 0;
            for (int i = 0; i < rows.Count; i++) sub += rows[i].Delta;   

            lines.Add(MakeBodyRow(indent,
                SubtotalLead + ParenValue(Signed(sub), SubtotalLabel),
                SubtotalLead + ParenValue(Signed(sub), Underline(Escape(SubtotalLabel))),
                DeltaColor(sub)));
        }

        

        
        
        
        
        
        
        
        
        private static string EmitFlat(List<FrameRow> lines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                var r = lines[i];
                if (r.Kind == RowBlank)
                {
                    sb.Append(LineSeparator);
                    continue;
                }

                var rich = ColorTag(r.Color, r.Inner);
                if (r.Kind == RowTitle) sb.Append(AlignCenter(Bold(rich)));
                else if (r.Kind == RowHeader) sb.Append(Bold(rich));
                else sb.Append(rich);
                sb.Append(LineSeparator);
            }
            return sb.ToString();
        }

        
        
        
        
        

        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private static string EmitFramed(List<FrameRow> lines)
        {
            
            int inner = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Kind == RowRule || lines[i].Kind == RowBlank) continue;
                int w = DisplayWidth(lines[i].Plain) + FramePadLeft + FramePadRight;
                if (w > inner) inner = w;
            }

            var sb = new StringBuilder();

            AppendFrameRule(sb, inner);                     
            for (int i = 0; i < lines.Count; i++)
            {
                var r = lines[i];

                if (r.Kind == RowRule || r.Kind == RowBlank)  
                {
                    AppendFrameRule(sb, inner);
                    continue;
                }

                int room = inner - FramePadLeft - FramePadRight - DisplayWidth(r.Plain);
                if (room < 0) room = 0;                     
                int leftExtra = (r.Kind == RowTitle) ? room / 2 : 0;
                int rightExtra = room - leftExtra;

                var rich = ColorTag(r.Color, r.Indent + r.Inner);
                if (r.Kind == RowTitle || r.Kind == RowHeader) rich = Bold(rich);

                sb.Append(ColorTag(ZeroColor, FrameVertical + Spaces(FramePadLeft + leftExtra)));
                sb.Append(rich);
                sb.Append(ColorTag(ZeroColor, Spaces(rightExtra + FramePadRight) + FrameVertical));
                sb.Append(LineSeparator);
            }
            AppendFrameRule(sb, inner);                     

            return sb.ToString();
        }

        
        private static void AppendFrameRule(StringBuilder sb, int inner)
        {
            sb.Append(ColorTag(ZeroColor,
                FrameCorner + new string(FrameHorizontal, inner) + FrameCorner));
            sb.Append(LineSeparator);
        }

        
        private static string Spaces(int n)
        {
            return n > 0 ? new string(' ', n) : "";
        }

        

        
        public static string Display(string text)
        {
            var label = StripKindSuffix(text);
            if (label.Length == 0) return "";

            string mapped;
            if (LabelMap.TryGetValue(label, out mapped)) return mapped;

            string region;
            if (TryRegion(label, out region)) return region;

            string trade;
            if (TryTrade(label, out trade)) return trade;

            return label;
        }

        
        public static string StripKindSuffix(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var s = text.Trim();
            if (EndsWith(s, OneTimeSuffix)) s = s.Substring(0, s.Length - OneTimeSuffix.Length).Trim();
            if (EndsWith(s, PerTurnSuffix)) s = s.Substring(0, s.Length - PerTurnSuffix.Length).Trim();
            return s;
        }

        
        
        
        
        public static bool TryRegion(string label, out string display)
        {
            display = null;
            if (string.IsNullOrEmpty(label) || label.Length < 4) return false;

            int up = label.IndexOf('↑');
            int down = label.IndexOf('↓');
            int arrow = up >= 0 ? up : down;
            if (arrow <= 0) return false;
            if (!EndsWith(label, "档")) return false;

            int steps;
            if (!int.TryParse(label.Substring(arrow + 1, label.Length - arrow - 2), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out steps) || steps <= 0)
                return false;

            var name = label.Substring(0, arrow).Trim();
            if (name.Length == 0) return false;
            if (!EndsWith(name, "地区")) name += "地区";      

            string core = up >= 0
                ? (steps >= 2 ? "发展大幅提速（" + steps + " 档）" : "发展提速")
                : (steps >= 2 ? "发展大幅倒退（" + steps + " 档）" : "发展倒退");
            display = name + core;
            return true;
        }

        
        
        
        
        
        
        
        
        
        public static bool TryTrade(string label, out string display)
        {
            display = null;
            if (string.IsNullOrEmpty(label)) return false;

            string sp;
            if (TradeSpecialMap.TryGetValue(label, out sp)) { display = sp; return true; }

            
            const string ftz = "自由贸易区";
            if (EndsWith(label, ftz))
            {
                var who = label.Substring(0, label.Length - ftz.Length).Trim();
                
                int d0 = who.IndexOf('·');
                if (d0 > 0) who = who.Substring(0, d0).Trim();
                if (who.Length == 0) return false;
                display = "与" + who + "建立自由贸易区";
                return true;
            }

            
            
            var core = label;
            const string tradeWord = "贸易";
            if (EndsWith(core, tradeWord)) core = core.Substring(0, core.Length - tradeWord.Length).Trim();
            if (core.Length == 0) return false;

            int dot = core.IndexOf('·');
            if (dot <= 0 || dot >= core.Length - 1) return false;

            var who2 = core.Substring(0, dot).Trim();
            var what = core.Substring(dot + 1).Trim();
            if (who2.Length == 0 || what.Length == 0) return false;

            bool hasTopic = false;
            for (int i = 0; i < TradeTopicNoSuffix.Length; i++)
                if (string.Equals(what, TradeTopicNoSuffix[i], StringComparison.Ordinal)) { hasTopic = true; break; }

            display = "与" + who2 + "达成" + what + (hasTopic ? "" : "贸易");
            return true;
        }

        
        
        public static int SumDeltas(IList<Entry> entries)
        {
            int sum = 0;
            if (entries == null) return 0;
            for (int i = 0; i < entries.Count; i++) sum += entries[i].Delta;
            return sum;
        }

        
        public static int SumRows(IList<Row> rows)
        {
            int sum = 0;
            if (rows == null) return 0;
            for (int i = 0; i < rows.Count; i++) sum += rows[i].Delta;
            return sum;
        }

        
        
        
        
        
        
        
        
        
        public static int? ParseLineValue(string rendered, string label)
        {
            if (string.IsNullOrEmpty(rendered) || string.IsNullOrEmpty(label)) return null;
            var lines = rendered.Split(LineSeparator);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(label, StringComparison.Ordinal) < 0) continue;
                return LineValue(lines[i]);
            }
            return null;
        }

        
        private static int? LineValue(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;

            
            
            
            
            int open = line.IndexOf('(');
            if (open < 0) return null;

            int i = open + 1;
            int sign = 1;
            if (i < line.Length && (line[i] == '+' || line[i] == '-'))
            {
                if (line[i] == '-') sign = -1;
                i++;
            }
            int start = i;
            while (i < line.Length && line[i] >= '0' && line[i] <= '9') i++;
            if (i == start) return null;                 

            int v;
            if (!int.TryParse(line.Substring(start, i - start), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out v))
                return null;
            return sign < 0 ? -v : v;
        }

        
        
        
        
        
        
        public static string Signed(int v)
        {
            if (v == 0) return "0";
            return (v > 0 ? "+" : "") + v.ToString(CultureInfo.InvariantCulture);
        }

        
        public static string DeltaColor(int delta)
        {
            return delta > 0 ? PositiveColor : delta < 0 ? NegativeColor : ZeroColor;
        }

        
        public static string Tint(string hex, string inner)
        {
            return ColorTag(hex, inner);
        }

        
        public static string Bold(string inner)
        {
            return BoldOpen + (inner ?? "") + BoldClose;
        }

        
        public static string Underline(string inner)
        {
            return UnderlineOpen + (inner ?? "") + UnderlineClose;
        }

        
        public static string AlignCenter(string inner)
        {
            return AlignCenterOpen + (inner ?? "") + AlignCenterClose;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static string ParenValue(string value, string label)
        {
            var v = value ?? "";
            if (string.IsNullOrEmpty(label)) return "(" + v + ")";
            return "(" + v + " " + label + ")";
        }

        private static string ColorTag(string hex, string inner)
        {
            return "<color=" + hex + ">" + inner + "</color>";
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        public static int DisplayWidth(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int w = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                
                if (c == '\u200B' || c == '\u200D' || c == '\uFE0E' || c == '\uFE0F') continue;

                
                if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    w += 2;
                    i++;                      
                    continue;
                }

                
                if (c >= 0x1100 && (
                        c <= 0x115F ||                          
                        (c >= 0x2E80 && c <= 0xA4CF && c != 0x303F) ||   
                        (c >= 0xAC00 && c <= 0xD7A3) ||          
                        (c >= 0xF900 && c <= 0xFAFF) ||          
                        (c >= 0xFE30 && c <= 0xFE6F) ||          
                        (c >= 0xFF00 && c <= 0xFF60) ||          
                        (c >= 0xFFE0 && c <= 0xFFE6)))
                    w += 2;
                else w += 1;
            }
            return w;
        }

        
        
        
        
        
        
        
        
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '&') sb.Append("&amp;");
                else if (c == '<') sb.Append("&lt;");
                else if (c == '>') sb.Append("&gt;");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool EndsWith(string s, string suffix)
        {
            return s != null && suffix != null && s.Length >= suffix.Length
                && string.Equals(s.Substring(s.Length - suffix.Length), suffix, StringComparison.Ordinal);
        }
    }
}
