using log4net;

namespace HYESOFT.EDI.Common.Log
{
    /// <summary>
    /// 调用log4net写日志
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// 在exe或者网站所在的目录会自动生成log4net的配置文件，
        /// 如果想控制日志的输出格式，请修改那个配置文件
        /// </summary>
        public static ILog Logger;

        static LogHelper()
        {
            Logger = log4net.LogManager.GetLogger("");
        }
    }
}
