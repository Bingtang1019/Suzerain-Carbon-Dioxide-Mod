using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace OiVerify
{
    
    
    
    
    [BepInPlugin(Guid, "Oi.Verify", Version)]
    public sealed class Plugin : BasePlugin
    {
        public const string Guid = "com.dell.suzerain.oi.verify";
        public const string Version = "0.1.0";

        internal static ManualLogSource Logger;

        private ControlServer _server;

        public override void Load()
        {
            Logger = Log;

            ConfigEntry<bool> enabled = Config.Bind(
                "Verify", "Enabled", false,
                "Master switch. Keep false outside the disposable verification sandbox.");
            ConfigEntry<int> port = Config.Bind(
                "Verify", "Port", 39091,
                "Loopback TCP port. The server always binds 127.0.0.1 only.");
            ConfigEntry<string> tokenFile = Config.Bind(
                "Verify", "TokenFile", "",
                "Path to a UTF-8 file containing the shared token.");
            ConfigEntry<string> artifactsDir = Config.Bind(
                "Verify", "ArtifactsDir", "",
                "Only directory this plugin may write diagnostics/screenshots into.");
            ConfigEntry<int> maxSteps = Config.Bind(
                "Verify", "MaxSteps", 500,
                "Hard upper bound accepted by future scenario commands.");
            ConfigEntry<int> stepTimeoutSec = Config.Bind(
                "Verify", "StepTimeoutSec", 30,
                "Default wait timeout in seconds.");
            ConfigEntry<bool> autoQuit = Config.Bind(
                "Verify", "AutoQuit", true,
                "Allow the quit command to end the process.");
            ConfigEntry<int> exitWatchdogSec = Config.Bind(
                "Verify", "ExitWatchdogSec", 10,
                "After Application.Quit(), force process exit if other plugins keep the player hung.");

            if (!enabled.Value)
            {
                Log.LogInfo("[OiVerify] disabled by [Verify] Enabled=false");
                return;
            }

            try
            {
                string root = BepInEx.Paths.GameRootPath;
                string tokenPath = ResolvePath(root, tokenFile.Value);
                string artifactPath = ResolvePath(root, artifactsDir.Value);
                if (string.IsNullOrWhiteSpace(tokenPath) || !File.Exists(tokenPath))
                    throw new FileNotFoundException("TokenFile does not exist", tokenPath);
                if (string.IsNullOrWhiteSpace(artifactPath))
                    throw new InvalidOperationException("ArtifactsDir is empty");
                Directory.CreateDirectory(artifactPath);

                VerifyBehaviour behaviour = AddComponent<VerifyBehaviour>();
                behaviour.Initialize(Log, artifactPath, maxSteps.Value, stepTimeoutSec.Value,
                    autoQuit.Value, exitWatchdogSec.Value);

                _server = new ControlServer(
                    port.Value, tokenPath, Log, behaviour);
                _server.Start();
                behaviour.SetShutdownAction(_server.Stop);

                WriteStartupEvidence(artifactPath, tokenPath, port.Value);
                Log.LogInfo("[OiVerify] ready on 127.0.0.1:" + port.Value
                    + " | token=" + tokenPath
                    + " | artifacts=" + artifactPath);
            }
            catch (Exception e)
            {
                Log.LogError("[OiVerify] startup failed: " + e);
                try { _server?.Stop(); } catch { }
                _server = null;
            }
        }

        public override bool Unload()
        {
            try { _server?.Stop(); } catch { }
            _server = null;
            return true;
        }

        private static string ResolvePath(string root, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return Path.IsPathRooted(value)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(Path.Combine(root, value));
        }

        private static void WriteStartupEvidence(string artifactsDir, string tokenPath, int port)
        {
            try
            {
                string file = Path.Combine(artifactsDir, "oi-verify-startup.json");
                string json =
                    "{\n" +
                    "  \"readyAtUtc\": " + JsonQuote(DateTime.UtcNow.ToString("o")) + ",\n" +
                    "  \"pluginVersion\": " + JsonQuote(Version) + ",\n" +
                    "  \"pid\": " + System.Diagnostics.Process.GetCurrentProcess().Id + ",\n" +
                    "  \"port\": " + port + ",\n" +
                    "  \"tokenFile\": " + JsonQuote(tokenPath) + ",\n" +
                    "  \"artifactsDir\": " + JsonQuote(artifactsDir) + "\n" +
                    "}\n";
                File.WriteAllText(file, json, new System.Text.UTF8Encoding(false));
            }
            catch (Exception e)
            {
                try { Logger.LogWarning("[OiVerify] startup evidence failed: " + e.Message); } catch { }
            }
        }

        private static string JsonQuote(string s)
        {
            return System.Text.Json.JsonSerializer.Serialize(s ?? "");
        }
    }
}
