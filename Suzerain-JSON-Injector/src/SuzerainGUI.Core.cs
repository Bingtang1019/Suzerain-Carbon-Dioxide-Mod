using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SuzerainTrainer
{
    
    class TrainerCore
    {
        public string curFile = null;
        public string json = null;
        public bool loaded = false;
        
        public int storyMode = 0;
        public string storyPack = null;

        
        public class ChangeRec
        {
            public string Kind;    
            public string Key;
            public string Field;
            public string OldValue;
            public string NewValue;
        }
        public List<ChangeRec> Changes = new List<ChangeRec>();
        
        public bool AllowEntityWrites = false;
        public bool HasEntityChanges
        {
            get
            {
                foreach (var c in Changes) if (c.Kind == "entity") return true;
                return false;
            }
        }
        bool suppressLog = false;

        void LogChange(string kind, string key, string field, string oldValue, string newValue)
        {
            if (suppressLog || oldValue == newValue) return;
            Changes.Add(new ChangeRec { Kind = kind, Key = key, Field = field, OldValue = oldValue, NewValue = newValue });
        }

        public void ClearChanges() { Changes.Clear(); }
        public int ChangeCount { get { return Changes.Count; } }

        public bool UndoLast()
        {
            if (Changes.Count == 0) return false;
            ChangeRec c = Changes[Changes.Count - 1];
            Changes.RemoveAt(Changes.Count - 1);
            suppressLog = true;
            bool ok = false;
            if (c.Kind == "var") ok = SetVar(c.Key, c.OldValue);
            else if (c.Kind == "bool") ok = SetBoolVar(c.Key, c.OldValue);
            else if (c.Kind == "string") ok = SetStringVar(c.Key, c.OldValue);
            else if (c.Kind == "entity") ok = SetEntityValue(c.Key, c.Field, c.OldValue);
            suppressLog = false;
            return ok;
        }

        public static string SaveDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData/LocalLow/Torpor Games/Suzerain");
        }

        public bool LoadFile(string path)
        {
            try
            {
                string text = File.ReadAllText(path);
                
                
                
                if (string.IsNullOrWhiteSpace(text))
                { Console.WriteLine("加载失败：文件为空（" + path + "）"); json = null; loaded = false; return false; }
                if (text.IndexOf("\"variables\"", StringComparison.Ordinal) < 0 &&
                    text.IndexOf("\"currentStoryPack\"", StringComparison.Ordinal) < 0)
                { Console.WriteLine("加载失败：不像 Suzerain 存档（既无 variables 也无 currentStoryPack 字段）"); json = null; loaded = false; return false; }
                json = text; curFile = path; loaded = true; storyMode = DetectStoryMode();
                Changes.Clear(); suppressLog = false; AllowEntityWrites = false;
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.Message); json = null; loaded = false; return false; }
        }

        public int DetectStoryMode()
        {
            storyPack = null;
            Match m = Regex.Match(json, "\"currentStoryPack\": \"([^\"]+)\"");
            
            
            
            if (m.Success)
            {
                storyPack = m.Groups[1].Value;
                return storyPack.IndexOf("Rizia", StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : 1;
            }
            
            bool hasBase = GetVar("BaseGame.GovernmentBudget") != null || GetVar("BaseGameSetup.CurrentTurn") != null;
            bool hasDlc = GetVar("RiziaDLC.Resources_Authority") != null || GetVar("RiziaDLCSetup.CurrentTurn") != null;
            if (hasBase && hasDlc) return 3;
            if (hasBase) return 1;
            if (hasDlc) return 2;
            return 0;
        }

        public string GetVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetVar(string key, string value)
        {
            string old = GetVar(key);
            string pattern = @"\[\\""" + Regex.Escape(key) + @"\\""\]=-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "[\\\"" + key + "\\\"]=" + value;
            json = Regex.Replace(json, pattern, repl);
            if (old != null) LogChange("var", key, null, old, value);
            return true;
        }

        public string GetBoolVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=(true|false)");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetBoolVar(string key, string value)
        {
            string old = GetBoolVar(key);
            string pattern = @"(\[\\""" + Regex.Escape(key) + @"\\""\]=)(true|false)";
            if (!Regex.IsMatch(json, pattern)) return false;
            json = Regex.Replace(json, pattern, "$1" + value);
            if (old != null) LogChange("bool", key, null, old, value);
            return true;
        }

        public string GetStringVar(string key)
        {
            Match m = Regex.Match(json, @"\[\\""" + Regex.Escape(key) + @"\\""\]=\""([^\""]*)\""");
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetStringVar(string key, string value)
        {
            string old = GetStringVar(key);
            string pattern = @"\[\\""" + Regex.Escape(key) + @"\\""\]=\""[^\""]*\""";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "[\\\"" + key + "\\\"]=\"" + value + "\"";
            json = Regex.Replace(json, pattern, repl);
            if (old != null) LogChange("string", key, null, old, value);
            return true;
        }

        public List<string> ScanVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanBoolVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=(true|false)");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanStringVars(string prefix)
        {
            List<string> list = new List<string>();
            Regex re = new Regex(@"\[\\""(" + Regex.Escape(prefix) + @"[A-Za-z0-9_\.]+)\\""\]=\""[^\""]*\""");
            foreach (Match m in re.Matches(json))
            {
                string k = m.Groups[1].Value;
                if (!list.Contains(k)) list.Add(k);
            }
            return list;
        }

        public List<string> ScanEntities()
        {
            List<string> list = new List<string>();
            Regex re = new Regex("\"nameInDatabase\": \"([A-Za-z0-9_ ]+)\"");
            foreach (Match m in re.Matches(json))
                if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
            return list;
        }

        public List<string> ScanEntityFields(string entity)
        {
            List<string> list = new List<string>();
            Regex re = new Regex("\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"([^\"]+)\"");
            foreach (Match m in re.Matches(json))
                if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
            return list;
        }

        public string GetEntityValue(string entity, string field)
        {
            string pattern = "\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"" + Regex.Escape(field) + "\",\\s*\"fieldValue\": \"([^\"]*)\"";
            Match m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        public bool SetEntityValue(string entity, string field, string value)
        {
            string old = GetEntityValue(entity, field);
            string pattern = "\"nameInDatabase\": \"" + Regex.Escape(entity) + "\",\\s*\"fieldName\": \"" + Regex.Escape(field) + "\",\\s*\"fieldValue\": \"([^\"]*)\"";
            if (!Regex.IsMatch(json, pattern)) return false;
            string repl = "\"nameInDatabase\": \"" + entity + "\",\n            \"fieldName\": \"" + field + "\",\n            \"fieldValue\": \"" + value + "\"";
            json = Regex.Replace(json, pattern, delegate(Match m2) { return repl; });
            if (old != null) LogChange("entity", entity, field, old, value);
            return true;
        }

        
        public bool SetOrAddEntityValue(string entity, string field, string value)
        {
            if (SetEntityValue(entity, field, value)) return true;
            string marker = "\n    ]\n}";
            int idx = json.LastIndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return false;
            string entry = ",\n        {\n            \"nameInDatabase\": \"" + entity + "\",\n            \"fieldName\": \"" + field + "\",\n            \"fieldValue\": \"" + value + "\"\n        }";
            json = json.Insert(idx, entry);
            return true;
        }

        public bool Save()
        {
            if (!loaded || curFile == null) return false;
            if (!AllowEntityWrites && HasEntityChanges)
            {
                Console.WriteLine("保存已阻止：包含 entityUpdates 修改；请显式启用实体写入。");
                return false;
            }
            try
            {
                File.Copy(curFile, curFile + ".bak", true);
                File.WriteAllText(curFile, json, new UTF8Encoding(false));
                return true;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return false; }
        }

        
        public bool RestoreBackup()
        {
            if (!loaded || curFile == null) return false;
            string bak = curFile + ".bak";
            try
            {
                if (!File.Exists(bak)) return false;
                File.Copy(bak, curFile, true);
                return LoadFile(curFile);
            }
            catch (Exception e) { Console.WriteLine(e.Message); return false; }
        }
    }
}
