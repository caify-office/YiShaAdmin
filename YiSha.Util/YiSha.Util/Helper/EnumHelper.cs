using System.Text;
using YiSha.Util.Extension;

namespace YiSha.Util.Helper
{
    public static class EnumHelper
    {
        /// <summary>
        /// 为有Flags特性的枚举类提供拆解后用逗号连接的值字符串
        /// </summary>
        public static string GetValueString<T>(T t) where T : System.Enum
        {
            var builder = new StringBuilder();
            foreach (T value in System.Enum.GetValues(typeof(T)))
            {
                if ((t.ParseToInt() & value.ParseToInt()) != 0)
                {
                    builder.Append(value.ToString("d") + ",");
                }
            }
            return builder.ToString().TrimEnd(',');
        }

        /// <summary>
        /// 为有Flags特性的枚举类提供拆解后用逗号连接的Description字符串
        /// </summary>
        public static string GetDescription<T>(T t) where T : System.Enum
        {
            var builder = new StringBuilder();
            foreach (T value in System.Enum.GetValues(typeof(T)))
            {
                if ((t.ParseToInt() & value.ParseToInt()) != 0)
                {
                    builder.Append(value.GetDescription() + ",");
                }
            }
            return builder.ToString().TrimEnd(',');
        }
    }
}