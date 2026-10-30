using System;
using System.Reflection;

namespace Oikonomia
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    internal static class MemberWriteCore
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static string SetPropOrFieldReason(object obj, string name, object value)
        {
            if (obj == null) return "对象为空";
            if (string.IsNullOrEmpty(name)) return "成员名为空";

            Type type;
            try
            {
                type = obj.GetType();
            }
            catch (Exception e)
            {
                return "读取对象类型失败：" + InnermostMessage(e);
            }

            string propertyFailure = null;
            PropertyInfo property = null;
            try
            {
                property = type.GetProperty(name, Flags);
            }
            catch (Exception e)
            {
                propertyFailure = "属性探测异常：" + InnermostMessage(e);
            }

            if (property == null)
            {
                if (propertyFailure == null) propertyFailure = "未找到属性";
            }
            else if (!property.CanWrite)
            {
                propertyFailure = "属性存在但不可写";
            }
            else
            {
                try
                {
                    property.SetValue(obj, value, null);
                    return null;
                }
                catch (Exception e)
                {
                    propertyFailure = "属性写入异常：" + InnermostMessage(e);
                }
            }

            FieldInfo field = null;
            try
            {
                field = type.GetField(name, Flags);
            }
            catch (Exception e)
            {
                return propertyFailure + "；字段探测异常：" + InnermostMessage(e);
            }

            if (field == null)
                return propertyFailure + "；未找到字段";

            try
            {
                field.SetValue(obj, value);
                return null;
            }
            catch (Exception e)
            {
                return propertyFailure + "；字段写入异常：" + InnermostMessage(e);
            }
        }

        private static string InnermostMessage(Exception e)
        {
            while (e != null && e.InnerException != null) e = e.InnerException;
            return e == null ? "未知异常" : e.Message;
        }
    }
}
