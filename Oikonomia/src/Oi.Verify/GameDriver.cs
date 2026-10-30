using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace OiVerify
{
    
    
    
    
    
    internal sealed class GameDriver
    {
        private static readonly string[] KeyFlags =
        {
            "BaseGame.Turn01_InT_Investment_Highway",
            "BaseGame.Turn01_InT_Investment_Railway",
            "BaseGame.Turn02_InT_Corporation_SordishState",
            "BaseGame.Turn02_InT_Corporation_TaurusHolding",
            "BaseGame.Turn02_InT_Corporation_UnderhallConstruction",
            "BaseGameSupport.Journal_Turn02_ConstructionSSC",
            "BaseGameSupport.Journal_Turn02_ConstructionTaurus",
            "BaseGameSupport.Journal_Turn02_ConstructionUnderhall",
            "GameCondition.Turn03_EnT_BusinessCouncil",
            "BaseGameSupport.Journal_Turn03_BusinessCouncil_PromisedTusk",
            "BaseGameSupport.Journal_Turn03_BusinessCouncil_RejectedTusk",
            "BaseGameSupport.Journal_Turn03_BusinessCouncil_PromisedPrivatizationTusk"
        };

        private readonly ManualLogSource _log;
        private readonly string _artifactsDir;
        private Type _handlerType;
        private Type _panelType;
        private Type _mainMenuType;
        private Type _loadPageType;
        private Type _persistenceType;
        private Type _buttonType;
        private Type _toggleType;
        private Type _tmpTextType;
        private Type _uiTextType;
        private Type _dialogueManagerType;
        private Type _luaHelperType;
        private Type _dialogueLuaType;
        private Type _screenCaptureType;
        private Type _fileInfoType;
        private string _persistentDataPath;
        private object _handler;
        private bool _loadSetupTried;

        internal GameDriver(ManualLogSource log, string artifactsDir)
        {
            _log = log;
            _artifactsDir = artifactsDir;
        }

        internal void Initialize()
        {
            _handlerType = FindType("ConversationHandler");
            _panelType = FindType("ConversationPanel");
            _mainMenuType = FindType("MainMenuPanel");
            _loadPageType = FindType("LoadGamePage");
            _persistenceType = FindType("PersistenceManager");
            _buttonType = FindType("UnityEngine.UI.Button");
            _toggleType = FindType("UnityEngine.UI.Toggle");
            _tmpTextType = FindType("TMPro.TextMeshProUGUI");
            _uiTextType = FindType("UnityEngine.UI.Text");
            _dialogueManagerType = FindType("PixelCrushers.DialogueSystem.DialogueManager");
            _luaHelperType = FindType("ArticyLuaHelper");
            _dialogueLuaType = FindType("PixelCrushers.DialogueSystem.DialogueLua");
            _screenCaptureType = FindType("UnityEngine.ScreenCapture");
            _fileInfoType = FindType("Il2CppSystem.IO.FileInfo");
            try { _persistentDataPath = UnityEngine.Application.persistentDataPath; } catch { }

            _log.LogInfo("[OiVerify] driver types: handler=" + TypeName(_handlerType)
                + " panel=" + TypeName(_panelType)
                + " mainMenu=" + TypeName(_mainMenuType)
                + " loadPage=" + TypeName(_loadPageType)
                + " persistence=" + TypeName(_persistenceType)
                + " button=" + TypeName(_buttonType)
                + " toggle=" + TypeName(_toggleType)
                + " tmpText=" + TypeName(_tmpTextType)
                + " dialogueManager=" + TypeName(_dialogueManagerType)
                + " luaHelper=" + TypeName(_luaHelperType)
                + " dialogueLua=" + TypeName(_dialogueLuaType)
                + " screenCapture=" + TypeName(_screenCaptureType)
                + " fileInfo=" + TypeName(_fileInfoType)
                + " persistentDataPath=" + (_persistentDataPath ?? ""));

            if (_handlerType == null) _log.LogError("[OiVerify] ConversationHandler type not found");
            if (_buttonType == null) _log.LogWarning("[OiVerify] UI Button type not found");
            if (_dialogueLuaType == null) _log.LogWarning("[OiVerify] DialogueLua type not found");
        }

        

        internal Dictionary<string, object> Snapshot()
        {
            var data = new Dictionary<string, object>();
            object handler = FindHandler();
            object mainMenu = FindMainMenu();
            object panel = FindObject(_panelType);

            bool inConversation = handler != null && ToBool(Prop(handler, "isInConversation"));
            bool responsesShown = handler != null && ToBool(Prop(handler, "responsesAreShown"));
            string title = ConversationTitle();

            data["inConversation"] = inConversation;
            data["responsesAreShown"] = responsesShown;
            data["conversationTitle"] = title ?? "";
            data["speaker"] = "";
            data["line"] = "";
            data["entryTitle"] = "";
            data["entryId"] = 0;
            data["conversationId"] = 0;
            data["responses"] = new List<object>();
            data["menuButtons"] = ButtonTexts(60);
            data["menuToggles"] = ToggleTexts(80);
            data["loadPage"] = LoadPageBrief();
            data["persistence"] = PersistenceBrief();
            data["mainMenu"] = mainMenu != null;
            data["conversationPanel"] = panel != null;
            data["keyFlags"] = ReadKeyFlags();

            if (handler != null)
            {
                object subtitle = Prop(handler, "currentSubtitle");
                if (Usable(subtitle))
                {
                    object speakerInfo = Prop(subtitle, "speakerInfo");
                    data["speaker"] = FirstNonEmpty(
                        Str(Prop(speakerInfo, "Name")),
                        Str(Prop(speakerInfo, "nameInDatabase")),
                        Str(Prop(handler, "lastSpeakerName")),
                        "");
                    object formatted = Prop(subtitle, "formattedText");
                    data["line"] = Str(Prop(formatted, "text")) ?? "";
                    object entry = Prop(subtitle, "dialogueEntry");
                    if (Usable(entry))
                    {
                        data["entryTitle"] = Str(Prop(entry, "Title")) ?? "";
                        data["entryId"] = ToInt(Prop(entry, "id"), 0);
                        data["conversationId"] = ToInt(Prop(entry, "conversationID"), 0);
                    }
                }

                if (responsesShown)
                    data["responses"] = ActiveResponses(handler);
            }

            return data;
        }

        internal Dictionary<string, object> WaitProbe(string kind, string flagName)
        {
            var data = Snapshot();
            if (string.Equals(kind, "flag", StringComparison.OrdinalIgnoreCase))
                data["waitFlag"] = ReadVariable(flagName);
            return data;
        }

        internal Dictionary<string, object> Menu()
        {
            object handler = FindHandler();
            var data = new Dictionary<string, object>
            {
                { "conversationTitle", ConversationTitle() ?? "" },
                { "responses", handler == null ? new List<object>() : ActiveResponses(handler) },
                { "buttons", ButtonTexts(100) },
                { "toggles", ToggleTexts(100) },
                { "loadPage", LoadPageBrief() },
                { "mainMenu", FindMainMenu() != null }
            };
            return data;
        }

        internal Dictionary<string, object> Click(string needle, string value = null)
        {
            if (string.IsNullOrWhiteSpace(needle))
                throw new InvalidOperationException("click.text is required");
            string inputNeedle = needle.Trim();

            if (!string.IsNullOrEmpty(value) && TrySetInputValue(inputNeedle, value))
            {
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "ui-input" },
                    { "matchedText", inputNeedle },
                    { "valueLength", value.Length }
                };
            }

            object handler = FindHandler();
            if (handler != null && ToBool(Prop(handler, "responsesAreShown")))
            {
                var responses = ActiveResponseObjects(handler);
                foreach (var item in responses)
                {
                    string text = item.Text ?? "";
                    if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    object response = item.Response;
                    MethodInfo click = handler.GetType().GetMethod(
                        "OnResponseClick",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (click == null)
                        throw new MissingMethodException("ConversationHandler.OnResponseClick");
                    click.Invoke(handler, new[] { response });
                    _log.LogInfo("[OiVerify] click response: " + Short(text));
                    return new Dictionary<string, object>
                    {
                        { "clicked", true },
                        { "route", "conversation-response" },
                        { "matchedText", text }
                    };
                }
            }

            object mainMenuEarly = FindMainMenu();
            if (mainMenuEarly != null && ToBool(CallResult(mainMenuEarly, "IsMainPageShowing")))
            {
                string earlyMethod = MainMenuMethodFor(needle);
                if (earlyMethod != null && Call(mainMenuEarly, earlyMethod))
                {
                    _log.LogInfo("[OiVerify] click main-menu method (early): " + earlyMethod);
                    if (earlyMethod == "OnLoadClick")
                    {
                        try
                        {
                            Call(mainMenuEarly, "SetupSaveLoadPages");
                            object page = FindLoadPage();
                            if (page != null) Call(page, "Setup");
                        }
                        catch { }
                    }
                    return new Dictionary<string, object>
                    {
                        { "clicked", true },
                        { "route", "main-menu-method" },
                        { "matchedText", needle },
                        { "method", earlyMethod }
                    };
                }
            }

            var buttons = ActiveButtons();
            ButtonItem chosen = null;
            foreach (var item in buttons)
                if (string.Equals(item.Text, needle, StringComparison.OrdinalIgnoreCase)) { chosen = item; break; }
            if (chosen == null)
            {
                foreach (var item in buttons)
                    if (item.Text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) { chosen = item; break; }
            }
            if (chosen != null)
            {
                chosen.Button.onClick.Invoke();
                _log.LogInfo("[OiVerify] click UI button: " + Short(chosen.Text));
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "ui-button" },
                    { "matchedText", chosen.Text }
                };
            }

            var toggles = ActiveToggles();
            ToggleItem chosenToggle = null;
            foreach (var item in toggles)
                if (string.Equals(item.Text, needle, StringComparison.OrdinalIgnoreCase)) { chosenToggle = item; break; }
            if (chosenToggle == null)
            {
                foreach (var item in toggles)
                    if (item.Text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) { chosenToggle = item; break; }
            }
            if (chosenToggle != null)
            {
                if (!chosenToggle.Toggle.isOn) chosenToggle.Toggle.isOn = true;
                else chosenToggle.Toggle.onValueChanged.Invoke(true);
                _log.LogInfo("[OiVerify] click UI toggle: " + Short(chosenToggle.Text));
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "ui-toggle" },
                    { "matchedText", chosenToggle.Text }
                };
            }

            string saveName;
            string savePath;
            if (TrySelectSaveFile(needle, out saveName, out savePath))
            {
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "savefile-metadata" },
                    { "matchedText", saveName ?? "" },
                    { "savePath", savePath ?? "" }
                };
            }
            if (TryLoadSaveFromPersistence(needle, out saveName, out savePath))
            {
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "savefile-load" },
                    { "matchedText", saveName ?? "" },
                    { "savePath", savePath ?? "" }
                };
            }
            if (TryLoadSaveFromFileInfo(needle, out saveName, out savePath))
            {
                return new Dictionary<string, object>
                {
                    { "clicked", true },
                    { "route", "savefile-fileinfo-load" },
                    { "matchedText", saveName ?? "" },
                    { "savePath", savePath ?? "" }
                };
            }

            object mainMenu = FindMainMenu();
            if (mainMenu != null)
            {
                string method = MainMenuMethodFor(needle);
                if (method != null && Call(mainMenu, method))
                {
                    _log.LogInfo("[OiVerify] click main-menu method: " + method);
                    if (method == "OnLoadClick")
                    {
                        try
                        {
                            Call(mainMenu, "SetupSaveLoadPages");
                            object page = FindLoadPage();
                            if (page != null) Call(page, "Setup");
                            _log.LogInfo("[OiVerify] requested load-page setup refresh");
                        }
                        catch (Exception e) { _log.LogWarning("[OiVerify] load page refresh failed: " + e.Message); }
                    }
                    return new Dictionary<string, object>
                    {
                        { "clicked", true },
                        { "route", "main-menu-method" },
                        { "matchedText", needle },
                        { "method", method }
                    };
                }
            }

            var available = new List<string>();
            foreach (var r in ActiveResponseObjects(handler)) available.Add("response:" + r.Text);
            foreach (var b in buttons) available.Add("button:" + b.Text);
            foreach (var t in toggles) available.Add("toggle:" + t.Text);
            throw new InvalidOperationException(
                "option_not_found: " + needle + " | available=" + string.Join(" ; ", available.ToArray()));
        }

        private Dictionary<string, object> LoadPageBrief()
        {
            try
            {
                object page = FindLoadPage();
                if (page == null) return null;
                object campaigns = Prop(page, "instantiatedCampaigns");
                int n = Count(campaigns);
                if (n == 0 && !_loadSetupTried)
                {
                    _loadSetupTried = true;
                    object menu = FindMainMenu();
                    if (menu != null) Call(menu, "SetupSaveLoadPages");
                    Call(page, "Setup");
                    campaigns = Prop(page, "instantiatedCampaigns");
                    n = Count(campaigns);
                    _log.LogInfo("[OiVerify] load-page Setup retry => campaignCount=" + n);
                }
                var names = new List<string>();
                for (int i = 0; i < n; i++)
                {
                    object campaign = Item(campaigns, i);
                    string name = Str(Prop(campaign, "CampaignName")) ?? "";
                    int sfCount = Count(Prop(campaign, "CampaignSaveFiles"));
                    names.Add(name + "(" + sfCount + ")");
                }
                object current = Prop(page, "currentlyDisplayedSaveFile");
                object container = Prop(page, "campaignContainer");
                object go = Prop(page, "gameObject");
                return new Dictionary<string, object>
                {
                    { "campaignCount", n },
                    { "campaigns", names },
                    { "gameObject", go == null ? "" : (Str(Prop(go, "name")) ?? "") },
                    { "containerChildren", container == null ? -1 : ToInt(Prop(container, "childCount"), -1) },
                    { "allLoadPages", FindAllObjects(_loadPageType).Count },
                    { "currentSaveFile", current == null ? "" : (Str(Prop(current, "fileName")) ?? "") },
                    { "currentSavePath", current == null ? "" : (Str(Prop(current, "path")) ?? "") }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object> { { "error", e.Message } };
            }
        }

        private Dictionary<string, object> PersistenceBrief()
        {
            try
            {
                if (_persistenceType == null) return null;
                object pm = GetStatic(_persistenceType, "Instance");
                if (!Usable(pm)) pm = FindObject(_persistenceType);
                if (!Usable(pm)) return new Dictionary<string, object> { { "found", false } };
                object saves = Prop(pm, "AllSaveFiles");
                object loadMethod = Prop(pm, "SaveLoadMethod");
                var paths = new Dictionary<string, object>();
                Type enumType = _persistenceType.GetNestedType("SaveFileType");
                if (enumType != null)
                {
                    foreach (object e in Enum.GetValues(enumType))
                    {
                        object path = CallResultWithArg(pm, "GetFilePath", e);
                        paths[Convert.ToString(e)] = path == null ? null : Convert.ToString(path);
                    }
                }
                return new Dictionary<string, object>
                {
                    { "found", true },
                    { "isInitialized", ToBool(Prop(pm, "IsInitialized")) },
                    { "saveCount", Count(saves) },
                    { "loadMethodType", loadMethod == null ? "" : loadMethod.GetType().FullName },
                    { "paths", paths },
                    { "persistentDataPath", _persistentDataPath ?? "" }
                };
            }
            catch (Exception e)
            {
                return new Dictionary<string, object> { { "error", e.Message } };
            }
        }

        private bool TrySelectSaveFile(string needle, out string matchedText, out string matchedPath)
        {
            matchedText = null;
            matchedPath = null;
            try
            {
                object page = FindLoadPage();
                if (page == null) return false;
                object campaigns = Prop(page, "instantiatedCampaigns");
                int n = Count(campaigns);
                for (int i = 0; i < n; i++)
                {
                    object campaign = Item(campaigns, i);
                    object saves = Prop(campaign, "CampaignSaveFiles");
                    int sn = Count(saves);
                    for (int j = 0; j < sn; j++)
                    {
                        object save = Item(saves, j);
                        if (!Usable(save)) continue;
                        string display;
                        string path;
                        if (!SaveMatches(save, needle, out display, out path)) continue;
                        if (!CallWithArg(page, "ShowSaveFileDetails", save)) continue;
                        matchedText = display;
                        matchedPath = path;
                        _log.LogInfo("[OiVerify] select save file: " + Short(display) + " | " + Short(path));
                        Call(page, "LoadSaveFile");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OiVerify] select save failed: " + e.Message);
            }
            return false;
        }

        
        
        
        
        private static bool SaveMatches(object save, string needle,
            out string display, out string path)
        {
            display = SaveDisplayName(save);
            path = Str(Prop(save, "path")) ?? "";
            string fileName = Str(Prop(save, "fileName")) ?? "";
            string baseName = "";
            try { baseName = Path.GetFileNameWithoutExtension(path); } catch { }
            string lastModified = SaveLastModified(save);
            string[] fields = { display, fileName, lastModified, path, baseName };
            foreach (string field in fields)
            {
                if (!string.IsNullOrEmpty(field)
                    && field.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (string.IsNullOrEmpty(display))
                        display = FirstNonEmpty(fileName, path);
                    return true;
                }
            }
            display = FirstNonEmpty(display, fileName, path);
            return false;
        }

        private static string SaveDisplayName(object save)
        {
            return FirstNonEmpty(
                Str(Prop(save, "saveFileName")),
                Str(Prop(save, "SaveFileName")),
                Str(Prop(save, "displayName")),
                Str(Prop(save, "DisplayName")),
                "");
        }

        private static string SaveLastModified(object save)
        {
            return FirstNonEmpty(
                Str(Prop(save, "lastModified")),
                Str(Prop(save, "LastModified")),
                "");
        }

        private bool TryLoadSaveFromPersistence(string needle, out string matchedText, out string matchedPath)
        {
            matchedText = null;
            matchedPath = null;
            try
            {
                if (_persistenceType == null) return false;
                object pm = GetStatic(_persistenceType, "Instance");
                if (!Usable(pm)) pm = FindObject(_persistenceType);
                if (pm == null)
                {
                    _log.LogWarning("[OiVerify] PersistenceManager instance not found");
                    return false;
                }
                object saves = Prop(pm, "AllSaveFiles");
                int n = Count(saves);
                if (n == 0)
                {
                    Call(pm, "InitializeSaveFiles");
                    saves = Prop(pm, "AllSaveFiles");
                    n = Count(saves);
                    _log.LogInfo("[OiVerify] PersistenceManager.InitializeSaveFiles => " + n + " save files");
                }
                _log.LogInfo("[OiVerify] persistence save candidates=" + n + " needle=" + Short(needle));
                for (int i = 0; i < n; i++)
                {
                    object save = Item(saves, i);
                    if (!Usable(save)) continue;
                    string display;
                    string path;
                    if (!SaveMatches(save, needle, out display, out path)) continue;
                    if (!CallWithArg(pm, "LoadSaveFile", save)) continue;
                    matchedText = display;
                    matchedPath = path;
                    _log.LogInfo("[OiVerify] load save via PersistenceManager: "
                        + Short(display) + " | " + Short(path));
                    return true;
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OiVerify] persistence load failed: " + e.Message);
            }
            return false;
        }

        private bool TryLoadSaveFromFileInfo(string needle, out string matchedText, out string matchedPath)
        {
            matchedText = null;
            matchedPath = null;
            try
            {
                if (_persistenceType == null || _fileInfoType == null
                    || string.IsNullOrEmpty(_persistentDataPath) || !Directory.Exists(_persistentDataPath))
                    return false;
                object pm = GetStatic(_persistenceType, "Instance");
                if (!Usable(pm)) pm = FindObject(_persistenceType);
                if (!Usable(pm)) return false;

                string exact = null;
                string contains = null;
                foreach (string path in Directory.GetFiles(_persistentDataPath, "*.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (name.Equals(needle, StringComparison.OrdinalIgnoreCase))
                    {
                        exact = path;
                        break;
                    }
                    if (contains == null && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        contains = path;
                }
                string hit = exact ?? contains;
                if (hit == null) return false;
                if (!(Path.GetFileName(hit).StartsWith("TurnSave_", StringComparison.Ordinal)
                      || Path.GetFileName(hit).StartsWith("ManualSave_", StringComparison.Ordinal)
                      || Path.GetFileName(hit).StartsWith("Active_", StringComparison.Ordinal)
                      || Path.GetFileName(hit).StartsWith("FinalSave_", StringComparison.Ordinal)
                      || Path.GetFileName(hit).StartsWith("Autosave_", StringComparison.Ordinal)))
                    return false;

                ConstructorInfo ctor = _fileInfoType.GetConstructor(new[] { typeof(string) });
                if (ctor == null) return false;
                object fileInfo = ctor.Invoke(new object[] { hit });
                object save = CallResultWithArg(pm, "GetSaveFileFromFileInfo", fileInfo);
                if (!Usable(save))
                {
                    _log.LogWarning("[OiVerify] GetSaveFileFromFileInfo returned null for " + hit);
                    return false;
                }
                if (!CallWithArg(pm, "LoadSaveFile", save)) return false;
                matchedText = Path.GetFileName(hit);
                matchedPath = hit;
                _log.LogInfo("[OiVerify] load save via FileInfo: " + Short(matchedText));
                return true;
            }
            catch (Exception e)
            {
                _log.LogWarning("[OiVerify] FileInfo save load failed: " + e.Message);
            }
            return false;
        }

        internal Dictionary<string, object> Advance()
        {
            object handler = FindHandler();
            if (handler == null) throw new InvalidOperationException("ConversationHandler not found");
            if (ToBool(Prop(handler, "responsesAreShown")))
                return new Dictionary<string, object>
                {
                    { "advanced", false },
                    { "reason", "response_menu_is_open" }
                };
            if (!Call(handler, "OnContinue"))
                throw new InvalidOperationException("ConversationHandler.OnContinue not found");
            _log.LogInfo("[OiVerify] advance: ConversationHandler.OnContinue");
            return new Dictionary<string, object> { { "advanced", true }, { "route", "OnContinue" } };
        }

        internal Dictionary<string, object> ReadVariableChecked(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("read.name is required");
            if (name.Length > 200) throw new InvalidOperationException("read.name too long");
            if (!IsAllowedVariableName(name))
                throw new InvalidOperationException("read.name is not in the verification whitelist: " + name);
            var value = ReadVariable(name);
            if (value == null) throw new InvalidOperationException("variable not found: " + name);
            return value;
        }

        internal Dictionary<string, object> Screenshot(string requestedName)
        {
            if (_screenCaptureType == null)
                throw new InvalidOperationException("UnityEngine.ScreenCapture type not found");
            if (string.IsNullOrWhiteSpace(_artifactsDir))
                throw new InvalidOperationException("ArtifactsDir is empty");

            string safe = SanitizeName(requestedName);
            if (string.IsNullOrWhiteSpace(safe)) safe = "shot-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            if (!safe.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) safe += ".png";
            string full = Path.GetFullPath(Path.Combine(_artifactsDir, safe));
            string root = Path.GetFullPath(_artifactsDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("screenshot path escapes ArtifactsDir");

            if (_screenCaptureType == null)
                throw new MissingMethodException("UnityEngine.ScreenCapture");
            try { Application.runInBackground = true; } catch { }
            var watch = Stopwatch.StartNew();
            Texture2D texture = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
                throw new InvalidOperationException("CaptureScreenshotAsTexture returned null");
            int width = texture.width;
            int height = texture.height;
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);
            if (png == null || png.Length == 0)
                throw new InvalidOperationException("EncodeToPNG returned empty");
            File.WriteAllBytes(full, png);
            watch.Stop();
            string sha;
            using (var sha256 = SHA256.Create())
                sha = BitConverter.ToString(sha256.ComputeHash(png)).Replace("-", "").ToLowerInvariant();
            _log.LogInfo("[OiVerify] screenshot written -> " + full
                + " bytes=" + png.Length + " " + width + "x" + height + " sha=" + sha);
            return new Dictionary<string, object>
            {
                { "path", full },
                { "name", safe },
                { "sha256", sha },
                { "width", width },
                { "height", height },
                { "bytes", png.Length },
                { "frameCount", Time.frameCount },
                { "captureMs", watch.ElapsedMilliseconds },
                { "mainMenu", FindMainMenu() != null },
                { "requestedAtUtc", DateTime.UtcNow.ToString("o") }
            };
        }

        

        private string ConversationTitle()
        {
            if (_dialogueManagerType == null) return null;
            object model = GetStatic(_dialogueManagerType, "conversationModel");
            if (!Usable(model))
            {
                object controller = GetStatic(_dialogueManagerType, "conversationController");
                if (Usable(controller)) model = Prop(controller, "conversationModel");
            }
            if (!Usable(model)) return null;
            return Str(Prop(model, "conversationTitle"));
        }

        private List<object> ActiveResponses(object handler)
        {
            var list = new List<object>();
            foreach (var item in ActiveResponseObjects(handler))
            {
                list.Add(new Dictionary<string, object>
                {
                    { "text", item.Text ?? "" },
                    { "index", item.Index },
                    { "articyId", item.ArticyId ?? "" }
                });
            }
            return list;
        }

        private sealed class ResponseItem
        {
            public object Response;
            public string Text;
            public string ArticyId;
            public int Index;
        }

        private List<ResponseItem> ActiveResponseObjects(object handler)
        {
            var output = new List<ResponseItem>();
            if (handler == null) return output;
            object list = Prop(handler, "instantiatedResponses");
            int n = Count(list);
            for (int i = 0; i < n; i++)
            {
                object item = Item(list, i);
                if (!Usable(item)) continue;
                object button = Prop(item, "responseButton");
                if (button != null && !IsActive(button)) continue;
                string text = Str(Prop(item, "unformattedText"));
                if (string.IsNullOrWhiteSpace(text))
                {
                    object textComp = Prop(item, "text");
                    text = Str(Prop(textComp, "text"));
                }
                if (string.IsNullOrWhiteSpace(text)) continue;
                output.Add(new ResponseItem
                {
                    Response = Prop(item, "response"),
                    Text = text.Trim(),
                    ArticyId = Str(Prop(item, "articyId")) ?? "",
                    Index = i
                });
            }
            return output;
        }

        private sealed class ButtonItem
        {
            public UnityEngine.UI.Button Button;
            public string Text;
        }

        private static bool IsUiVisible(UnityEngine.Component component)
        {
            try
            {
                if (component == null || !component.gameObject.activeInHierarchy) return false;
                UnityEngine.Transform t = component.transform;
                while (t != null)
                {
                    UnityEngine.CanvasGroup group = t.GetComponent<UnityEngine.CanvasGroup>();
                    if (group != null && group.alpha <= 0.01f) return false;
                    t = t.parent;
                }
                return true;
            }
            catch { return false; }
        }

        private List<ButtonItem> ActiveButtons()
        {
            var output = new List<ButtonItem>();
            UnityEngine.UI.Button[] buttons =
                UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
            foreach (UnityEngine.UI.Button button in buttons)
            {
                if (button == null || !IsUiVisible(button) || !button.interactable) continue;
                string text = ButtonText(button);
                if (string.IsNullOrWhiteSpace(text)) continue;
                output.Add(new ButtonItem { Button = button, Text = text.Trim() });
            }
            return output;
        }

        private List<string> ButtonTexts(int max)
        {
            var output = new List<string>();
            foreach (var item in ActiveButtons())
            {
                if (output.Count >= max) break;
                if (!output.Contains(item.Text)) output.Add(item.Text);
            }
            return output;
        }

        private string ButtonText(UnityEngine.UI.Button button)
        {
            try
            {
                if (button == null) return null;
                TMPro.TextMeshProUGUI tmp = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text)) return tmp.text.Trim();
                UnityEngine.UI.Text legacy = button.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacy != null && !string.IsNullOrWhiteSpace(legacy.text)) return legacy.text.Trim();
                return button.gameObject.name;
            }
            catch { return null; }
        }

        private sealed class ToggleItem
        {
            public UnityEngine.UI.Toggle Toggle;
            public string Text;
        }

        private List<ToggleItem> ActiveToggles()
        {
            var output = new List<ToggleItem>();
            UnityEngine.UI.Toggle[] toggles =
                UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Toggle>();
            foreach (UnityEngine.UI.Toggle toggle in toggles)
            {
                if (toggle == null || !IsUiVisible(toggle) || !toggle.interactable) continue;
                string text = ToggleText(toggle);
                if (string.IsNullOrWhiteSpace(text)) continue;
                output.Add(new ToggleItem { Toggle = toggle, Text = text.Trim() });
            }
            return output;
        }

        private List<string> ToggleTexts(int max)
        {
            var output = new List<string>();
            foreach (var item in ActiveToggles())
            {
                if (output.Count >= max) break;
                if (!output.Contains(item.Text)) output.Add(item.Text);
            }
            return output;
        }

        private string ToggleText(UnityEngine.UI.Toggle toggle)
        {
            try
            {
                if (toggle == null) return null;
                TMPro.TextMeshProUGUI tmp = toggle.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text)) return tmp.text.Trim();
                UnityEngine.UI.Text legacy = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
                if (legacy != null && !string.IsNullOrWhiteSpace(legacy.text)) return legacy.text.Trim();
                return toggle.gameObject.name;
            }
            catch { return null; }
        }

        
        private bool TrySetInputValue(string needle, string value)
        {
            try
            {
                TMPro.TMP_InputField[] fields =
                    UnityEngine.Object.FindObjectsOfType<TMPro.TMP_InputField>();
                foreach (TMPro.TMP_InputField field in fields)
                {
                    if (field == null || !field.gameObject.activeInHierarchy) continue;
                    string placeholder = "";
                    try
                    {
                        TMPro.TMP_Text tmp = field.placeholder as TMPro.TMP_Text;
                        if (tmp != null) placeholder = tmp.text ?? "";
                    }
                    catch { }
                    string parent = "";
                    try { parent = field.transform.parent == null ? "" : field.transform.parent.name; } catch { }
                    string haystack = (field.gameObject.name ?? "") + " | " + placeholder + " | " + parent;
                    if (haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    field.ActivateInputField();
                    field.text = value;
                    try { field.onValueChanged.Invoke(value); } catch { }
                    try { field.onEndEdit.Invoke(value); } catch { }
                    try { field.caretPosition = value.Length; } catch { }
                    _log.LogInfo("[OiVerify] set UI input field: " + Short(haystack)
                        + " valueLen=" + value.Length);
                    return true;
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OiVerify] set UI input failed: " + e.Message);
            }
            return false;
        }

        internal Dictionary<string, object> UiDump(int max)
        {
            if (max <= 0) max = 200;
            if (max > 1000) max = 1000;
            var items = new List<object>();
            try
            {
                foreach (UnityEngine.UI.Button b in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>())
                {
                    if (items.Count >= max) break;
                    if (b == null || !b.gameObject.activeInHierarchy) continue;
                    AddUiItem(items, "Button", b, ButtonText(b), b.interactable);
                }
                foreach (UnityEngine.UI.Toggle t in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Toggle>())
                {
                    if (items.Count >= max) break;
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    AddUiItem(items, "Toggle", t, ToggleText(t), t.interactable);
                }
                foreach (TMPro.TextMeshProUGUI tmp in UnityEngine.Object.FindObjectsOfType<TMPro.TextMeshProUGUI>())
                {
                    if (items.Count >= max) break;
                    if (tmp == null || !tmp.gameObject.activeInHierarchy) continue;
                    AddUiItem(items, "TMPText", tmp, tmp.text, true);
                }
                foreach (UnityEngine.UI.Text text in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>())
                {
                    if (items.Count >= max) break;
                    if (text == null || !text.gameObject.activeInHierarchy) continue;
                    AddUiItem(items, "Text", text, text.text, true);
                }
            }
            catch (Exception e)
            {
                return new Dictionary<string, object> { { "error", e.Message } };
            }
            return new Dictionary<string, object> { { "count", items.Count }, { "items", items } };
        }

        private static void AddUiItem(List<object> items, string kind, Component component,
            string text, bool interactive)
        {
            try
            {
                if (component == null) return;
                string parent = "";
                try { parent = component.transform.parent == null ? "" : component.transform.parent.name; } catch { }
                UnityEngine.Vector3 pos = default(UnityEngine.Vector3);
                try { pos = component.transform.position; } catch { }
                items.Add(new Dictionary<string, object>
                {
                    { "kind", kind },
                    { "name", component.gameObject.name },
                    { "text", text ?? "" },
                    { "active", component.gameObject.activeInHierarchy },
                    { "interactive", interactive },
                    { "parent", parent },
                    { "x", pos.x },
                    { "y", pos.y },
                    { "z", pos.z }
                });
            }
            catch { }
        }

        private object FindHandler()
        {
            if (Usable(_handler)) return _handler;
            if (_handlerType == null) return null;
            var all = FindAllObjects(_handlerType);
            object fallback = null;
            foreach (object candidate in all)
            {
                if (!Usable(candidate)) continue;
                if (fallback == null) fallback = candidate;
                if (ToBool(Prop(candidate, "isInConversation"))) { _handler = candidate; return candidate; }
            }
            _handler = fallback;
            return fallback;
        }

        private object FindMainMenu()
        {
            if (_mainMenuType == null) return null;
            foreach (object candidate in FindAllObjects(_mainMenuType))
            {
                if (!Usable(candidate) || !IsActive(candidate)) continue;
                object showing = CallResult(candidate, "IsShowing");
                if (showing == null || ToBool(showing)) return candidate;
            }
            return FindObject(_mainMenuType);
        }

        private object FindLoadPage()
        {
            object menu = FindMainMenu();
            if (menu != null)
            {
                object page = Prop(menu, "loadGamePage");
                if (Usable(page)) return page;
            }
            return FindObject(_loadPageType);
        }

        private object FindObject(Type type)
        {
            if (type == null) return null;
            var all = FindAllObjects(type);
            return all.Count == 0 ? null : all[0];
        }

        private List<object> FindAllObjects(Type type)
        {
            var output = new List<object>();
            if (type == null) return output;
            try
            {
                var arr = UnityEngine.Object.FindObjectsOfType(
                    Il2CppInterop.Runtime.Il2CppType.From(type));
                for (int i = 0; i < arr.Length; i++)
                {
                    object item = arr[i];
                    if (item != null) output.Add(item);
                }
            }
            catch (Exception e)
            {
                _log.LogWarning("[OiVerify] FindObjectsOfType(" + type.FullName + ") failed: " + e.Message);
            }
            if (output.Count == 0)
            {
                try
                {
                    var arr = UnityEngine.Resources.FindObjectsOfTypeAll(
                        Il2CppInterop.Runtime.Il2CppType.From(type));
                    for (int i = 0; i < arr.Length; i++)
                    {
                        object item = arr[i];
                        if (item != null) output.Add(item);
                    }
                }
                catch (Exception e)
                {
                    _log.LogWarning("[OiVerify] FindObjectsOfTypeAll(" + type.FullName + ") failed: " + e.Message);
                }
            }
            return output;
        }

        private static IEnumerable<object> Enumerate(object arr)
        {
            if (arr == null) yield break;
            var enumerable = arr as IEnumerable;
            if (enumerable != null && !(arr is string))
            {
                foreach (object item in enumerable) yield return item;
                yield break;
            }
            int count = ToInt(Prop(arr, "Length"), -1);
            if (count < 0) count = ToInt(Prop(arr, "Count"), -1);
            MethodInfo getItem = arr.GetType().GetMethod("get_Item", new[] { typeof(int) });
            if (getItem == null) yield break;
            for (int i = 0; i < count; i++)
            {
                object item = null;
                try { item = getItem.Invoke(arr, new object[] { i }); } catch { }
                if (item != null) yield return item;
            }
        }

        private static List<object> ComponentsInChildren(object gameObject, Type componentType)
        {
            var output = new List<object>();
            if (!Usable(gameObject) || componentType == null) return output;
            foreach (var m in gameObject.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (m.Name != "GetComponentsInChildren") continue;
                var ps = m.GetParameters();
                if (ps.Length < 1 || ps[0].ParameterType != typeof(Type)) continue;
                try
                {
                    object arr = ps.Length == 1
                        ? m.Invoke(gameObject, new object[] { componentType })
                        : m.Invoke(gameObject, new object[] { componentType, false });
                    foreach (object item in Enumerate(arr)) output.Add(item);
                    if (output.Count > 0) return output;
                }
                catch { }
            }
            return output;
        }

        

        private static string MainMenuMethodFor(string needle)
        {
            string s = needle.ToLowerInvariant();
            if (s.Contains("继续") || s.Contains("continue")) return "OnContinueClick";
            if (s.Contains("加载") || s.Contains("load")) return "OnLoadClick";
            if (s.Contains("新游戏") || s.Contains("new game")) return "OnNewGameClick";
            if (s.Contains("退出") || s.Contains("exit")) return "OnExitClick";
            if (s.Contains("返回") || s.Contains("back")) return "OnHeaderBackClick";
            return null;
        }

        

        private Dictionary<string, object> ReadKeyFlags()
        {
            var flags = new Dictionary<string, object>();
            foreach (string name in KeyFlags)
                flags[name] = ReadVariable(name);
            return flags;
        }

        private Dictionary<string, object> ReadVariable(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_dialogueLuaType != null)
            {
                object exists = CallStatic(_dialogueLuaType, "DoesVariableExist", name);
                bool doesExist = exists is bool && (bool)exists;
                object result = CallStatic(_dialogueLuaType, "GetVariable", name);
                if (Usable(result))
                {
                    bool has = ToBool(Prop(result, "hasReturnValue"));
                    if (has || doesExist)
                    {
                        object value;
                        string kind;
                        if (ToBool(Prop(result, "isBool")) || ToBool(Prop(result, "IsBool")))
                        {
                            value = Prop(result, "asBool") ?? Prop(result, "AsBool");
                            kind = "bool";
                        }
                        else if (ToBool(Prop(result, "isNumber")) || ToBool(Prop(result, "IsNumber")))
                        {
                            value = Prop(result, "asFloat") ?? Prop(result, "AsFloat");
                            kind = "number";
                        }
                        else
                        {
                            value = Prop(result, "asString") ?? Prop(result, "AsString");
                            kind = "string";
                            if (value == null || string.Equals(Convert.ToString(value), "nil", StringComparison.OrdinalIgnoreCase))
                                return null;
                        }
                        return new Dictionary<string, object>
                        {
                            { "name", name }, { "exists", true }, { "kind", kind }, { "value", value }
                        };
                    }
                }
            }

            if (_luaHelperType != null)
            {
                try
                {
                    string expr = "Variable[\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"] == true";
                    MethodInfo m = _luaHelperType.GetMethod(
                        "IsTrue", BindingFlags.Public | BindingFlags.Static, null,
                        new[] { typeof(string) }, null);
                    if (m == null) throw new InvalidOperationException("IsTrue not found");
                    object r = m.Invoke(null, new object[] { expr });
                    return new Dictionary<string, object>
                    {
                        { "name", name }, { "exists", true }, { "kind", "bool" }, { "value", r != null && (bool)r }
                    };
                }
                catch { }
            }
            return null;
        }

        private static bool IsAllowedVariableName(string name)
        {
            if (name.IndexOfAny(new[] { '"', '\'', '\n', '\r', ';', '(', ')', '[', ']', '=', ' ' }) >= 0)
                return false;
            string[] prefixes =
            {
                "BaseGame.", "BaseGameSupport.", "BaseGameIsolated.", "GameCondition.",
                "RiziaSupport.", "RiziaDLCSupport.", "SharedSupport.", "Journal."
            };
            foreach (string p in prefixes)
                if (name.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        

        private static Type FindType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in asms)
                {
                    try
                    {
                        Type direct = asm.GetType(name);
                        if (direct != null) return direct;
                    }
                    catch { }
                }
                foreach (Assembly asm in asms)
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types; } catch { continue; }
                    if (types == null) continue;
                    foreach (Type t in types)
                    {
                        if (t == null) continue;
                        if (string.Equals(t.Name, name, StringComparison.Ordinal)
                            || string.Equals(t.FullName, name, StringComparison.Ordinal))
                            return t;
                    }
                }
            }
            catch { }
            return null;
        }

        private static bool Usable(object obj)
        {
            if (obj == null) return false;
            try
            {
                var b = obj as Il2CppObjectBase;
                if (b != null && b.Pointer == IntPtr.Zero) return false;
            }
            catch { return false; }
            return true;
        }

        private static object Prop(object obj, string name)
        {
            if (!Usable(obj)) return null;
            try
            {
                Type t = obj.GetType();
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanRead) return p.GetValue(obj);
                var m = t.GetMethod("get_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) return m.Invoke(obj, null);
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) return f.GetValue(obj);
            }
            catch { }
            return null;
        }

        private static object GetStatic(Type type, string name)
        {
            if (type == null) return null;
            try
            {
                var p = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanRead) return p.GetValue(null);
                var m = type.GetMethod("get_" + name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null) return m.Invoke(null, null);
                var f = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) return f.GetValue(null);
            }
            catch { }
            return null;
        }

        private static object CallStatic(Type type, string name, object arg)
        {
            if (type == null) return null;
            try
            {
                foreach (var m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != name || m.GetParameters().Length != 1) continue;
                    try { return m.Invoke(null, new[] { arg }); } catch { }
                }
            }
            catch { }
            return null;
        }

        private static object CallResult(object obj, string name)
        {
            if (!Usable(obj)) return null;
            try
            {
                var m = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                return m == null ? null : m.Invoke(obj, null);
            }
            catch { return null; }
        }

        private static object CallResultWithArg(object obj, string name, object arg)
        {
            if (!Usable(obj)) return null;
            try
            {
                foreach (var m in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != name || m.GetParameters().Length != 1) continue;
                    try { return m.Invoke(obj, new[] { arg }); } catch { }
                }
            }
            catch { }
            return null;
        }

        private static bool CallWithArg(object obj, string name, object arg)
        {
            if (!Usable(obj)) return false;
            try
            {
                foreach (var m in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != name || m.GetParameters().Length != 1) continue;
                    try { m.Invoke(obj, new[] { arg }); return true; } catch { }
                }
            }
            catch { }
            return false;
        }

        private static bool Call(object obj, string name)
        {
            if (!Usable(obj)) return false;
            try
            {
                MethodInfo m = FindMethod(obj, name, 0);
                if (m == null) return false;
                m.Invoke(obj, null);
                return true;
            }
            catch { return false; }
        }

        private static MethodInfo FindMethod(object obj, string name, int argCount)
        {
            if (!Usable(obj)) return null;
            try
            {
                foreach (var m in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    if (m.Name == name && m.GetParameters().Length == argCount) return m;
            }
            catch { }
            return null;
        }

        private static MethodInfo FindMethodByFirstArg(Type type, string name, Type first)
        {
            if (type == null) return null;
            try
            {
                foreach (var m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var ps = m.GetParameters();
                    if (m.Name == name && ps.Length >= 1 && ps[0].ParameterType == first) return m;
                }
            }
            catch { }
            return null;
        }

        private static bool IsActive(object component)
        {
            try
            {
                object go = Prop(component, "gameObject");
                if (!Usable(go)) return false;
                object active = Prop(go, "activeInHierarchy");
                if (active == null) active = Prop(go, "activeSelf");
                return active == null || ToBool(active);
            }
            catch { return false; }
        }

        private static int Count(object list)
        {
            if (!Usable(list)) return 0;
            object c = Prop(list, "Count");
            if (c == null) c = Prop(list, "Length");
            return ToInt(c, 0);
        }

        private static object Item(object list, int index)
        {
            if (!Usable(list)) return null;
            try
            {
                MethodInfo m = list.GetType().GetMethod("get_Item", new[] { typeof(int) });
                return m == null ? null : m.Invoke(list, new object[] { index });
            }
            catch { return null; }
        }

        private static int ToInt(object value, int fallback)
        {
            try { return value == null ? fallback : Convert.ToInt32(value); }
            catch { return fallback; }
        }

        private static bool ToBool(object value)
        {
            try { return value != null && Convert.ToBoolean(value); }
            catch { return false; }
        }

        private static string Str(object value)
        {
            try { return value == null ? null : value.ToString(); }
            catch { return null; }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string v in values)
                if (!string.IsNullOrWhiteSpace(v)) return v;
            return "";
        }

        private static List<string> Unique(List<string> input)
        {
            var output = new List<string>();
            foreach (string s in input)
                if (!output.Contains(s)) output.Add(s);
            return output;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var sb = new StringBuilder();
            foreach (char c in value)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9') || c == '-' || c == '_')
                    sb.Append(c);
                else if (c == '.' || c == ' ')
                    sb.Append('-');
            }
            return sb.ToString();
        }

        private static string Short(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= 180 ? value : value.Substring(0, 180) + "…";
        }

        private static string TypeName(Type t)
        {
            return t == null ? "(null)" : t.FullName;
        }
    }
}
