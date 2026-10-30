using System;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class BridgeOutcome
    {
        
        public const string Applied = "applied";

        
        public const string Absent = "absent";

        
        public const string VersionUnsupported = "version_unsupported";

        
        public const string ReadFailed = "read_failed";

        
        
        
        
        
        
        
        
        
        public static string Describe(string outcome, string overridePath, string lastError)
        {
            if (outcome == VersionUnsupported)
                return "override 【存在但版本不支持】⇒ 已忽略，使用 cfg 配置。文件：" + overridePath;

            if (outcome == ReadFailed)
                return "override 【存在但读失败】⇒ 使用 cfg 配置。原因：" + (lastError ?? "")
                    + " ｜ 文件：" + overridePath;

            if (outcome == Applied)
                return null;   

            return "未发现 override 文件（路径：" + overridePath + "），使用 cfg 配置。";
        }

        
        
        
        public static string[] AllOutcomes()
        {
            return new string[] { Applied, Absent, VersionUnsupported, ReadFailed };
        }
    }
}
