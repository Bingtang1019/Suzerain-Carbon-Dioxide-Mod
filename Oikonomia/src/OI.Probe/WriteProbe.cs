using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Oikonomia.Probe
{
    
    
    
    
    
    
    
    
    
    
    internal static class WriteProbe
    {
        private static Type _dataType;
        private static Type _propsType;
        private static Type _instrType;
        private static Type _helperType;
        private static MethodInfo _evaluateData;

        private static bool _ranThisSession;
        private static int _baselineThreshold = -1;
        private static int _pendingChecks;

        public static void Init(Assembly asm)
        {
            _dataType = Reflect.FindType(asm, "ConditionalInstructionData");
            _propsType = Reflect.FindType(asm, "ConditionalInstructionProperties");
            _helperType = Reflect.FindType(asm, "ConditionalInstructionHelper");
            if (_propsType != null) _instrType = _propsType.GetNestedType("ConditionalInstruction");
            if (_helperType != null && _dataType != null)
                _evaluateData = _helperType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static,
                                                      null, new[] { _dataType }, null);

            ProbeLog.Line("[WP] 类型就绪：data=" + (_dataType != null) + " props=" + (_propsType != null)
                + " instr=" + (_instrType != null) + " evaluateData=" + (_evaluateData != null));
        }

        
        public static void RunAtTurnBoundary(Func<string, bool> isTrue, Action<string> runInstant)
        {
            if (!ProbeLog.WriteTest) return;
            if (_ranThisSession) { DeferredCheck(isTrue); return; }
            _ranThisSession = true;

            ProbeLog.Mark("[WP] ===== 写入通道对照实验开始 =====");

            
            _baselineThreshold = ReadIntByConditions(isTrue, "BaseGame.Economy_DangerThreshold", 0, 12);
            ProbeLog.Mark("[WP] 基线 BaseGame.Economy_DangerThreshold = " + _baselineThreshold);

            
            ProbeLog.Mark("[WP][A] RunInstant(\"BaseGame.OI_ProbeWrite = 7\")  （无分号 / 新变量）");
            runInstant("BaseGame.OI_ProbeWrite = 7");
            ProbeLog.Mark("[WP][A] 立即回读 == 7 → " + isTrue("BaseGame.OI_ProbeWrite == 7"));

            
            if (_baselineThreshold >= 0)
            {
                ProbeLog.Mark("[WP][B] RunInstant(\"BaseGame.Economy_DangerThreshold = 99;\")  （有分号 / 已存在变量）");
                runInstant("BaseGame.Economy_DangerThreshold = 99;");
                bool hit = isTrue("BaseGame.Economy_DangerThreshold == 99");
                ProbeLog.Mark("[WP][B] 立即回读 == 99 → " + hit);
                runInstant("BaseGame.Economy_DangerThreshold = " + _baselineThreshold + ";");
                bool restored = isTrue("BaseGame.Economy_DangerThreshold == " + _baselineThreshold);
                ProbeLog.Mark("[WP][B] 还原为 " + _baselineThreshold + " → " + restored + (restored ? " ✓" : " ✗（需人工核对！）"));
            }

            
            ProbeLog.Mark("[WP][C] RunInstant(\"BaseGame.OI_ProbeWrite2 = 8;\")  （有分号 / 新变量）");
            runInstant("BaseGame.OI_ProbeWrite2 = 8;");
            ProbeLog.Mark("[WP][C] 立即回读 == 8 → " + isTrue("BaseGame.OI_ProbeWrite2 == 8"));

            
            RunEvaluateExperiment(isTrue);

            _pendingChecks = 3;
            ProbeLog.Mark("[WP] ===== 对照实验结束（延迟回读见后续快照）=====");
        }

        
        public static void DeferredCheck(Func<string, bool> isTrue)
        {
            if (_pendingChecks <= 0) return;
            _pendingChecks--;
            ProbeLog.Mark("[WP][延迟回读] OI_ProbeWrite==7 → " + isTrue("BaseGame.OI_ProbeWrite == 7")
                + " ; OI_ProbeWrite2==8 → " + isTrue("BaseGame.OI_ProbeWrite2 == 8")
                + " ; Threshold==" + _baselineThreshold + " → " + isTrue("BaseGame.Economy_DangerThreshold == " + _baselineThreshold));
        }

        private static void RunEvaluateExperiment(Func<string, bool> isTrue)
        {
            try
            {
                if (_evaluateData == null || _dataType == null || _propsType == null || _instrType == null)
                {
                    ProbeLog.Error("[WP][D] 类型缺失，跳过（data=" + (_dataType != null) + " props=" + (_propsType != null)
                        + " instr=" + (_instrType != null) + " evaluate=" + (_evaluateData != null) + "）");
                    return;
                }

                object instr = Activator.CreateInstance(_instrType);
                Reflect.SetPropOrField(instr, "Condition", "");
                Reflect.SetPropOrField(instr, "Instruction", "BaseGame.OI_ProbeWrite3 = 9;");

                object list = Activator.CreateInstance(_propsType.GetProperty("ConditionalInstructions").PropertyType);
                list.GetType().GetMethod("Add").Invoke(list, new[] { instr });

                object props = Activator.CreateInstance(_propsType);
                Reflect.SetPropOrField(props, "ConditionalInstructions", list);
                Reflect.SetPropOrField(props, "Priority", 0);
                Reflect.SetPropOrField(props, "CheckPerTurn", false);
                Reflect.SetPropOrField(props, "CheckPerStep", false);
                Reflect.SetPropOrField(props, "IsOneTime", false);
                Reflect.SetPropOrField(props, "IsDone", false);
                Reflect.SetPropOrField(props, "CheckOnTurnNo", -1);
                Reflect.SetPropOrField(props, "CheckOnStepNo", -1);

                object data = Activator.CreateInstance(_dataType);
                Reflect.SetPropOrField(data, "NameInDatabase", "OI_Probe_AdHoc");
                Reflect.SetPropOrField(data, "ConditionalInstructionProperties", props);

                ProbeLog.Mark("[WP][D] 构造 ConditionalInstructionData 完成，调用 ConditionalInstructionHelper.Evaluate(data)…");
                _evaluateData.Invoke(null, new[] { data });
                ProbeLog.Mark("[WP][D] Evaluate 返回；立即回读 OI_ProbeWrite3 == 9 → " + isTrue("BaseGame.OI_ProbeWrite3 == 9"));
            }
            catch (Exception e)
            {
                ProbeLog.Error("[WP][D] 异常：" + e.GetType().Name + " : " + e.Message
                    + (e.InnerException != null ? " | inner: " + e.InnerException.Message : ""));
            }
        }

        
        private static int ReadIntByConditions(Func<string, bool> isTrue, string varName, int lo, int hi)
        {
            for (int v = lo; v <= hi; v++)
                if (isTrue(varName + " == " + v)) return v;
            return -1;
        }
    }
}
