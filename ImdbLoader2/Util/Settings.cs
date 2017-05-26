using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImdbLoader2.Util
{
    public static class Settings
    {
        public static string ImdbSourceFile { get { return GetSetting(); } }
        public static string ImdbSourceFileContainer { get { return GetSetting(); } }
        public static string ImdbParsedFileContainer { get { return GetSetting(); } }
        public static string StorageAccountConnectionString { get { return GetSetting(); } }
        public static string GraphEndpoint { get { return GetSetting(); } }
        public static string GraphKey { get { return GetSetting(); } }
        public static string GraphDatabase { get { return GetSetting(); } }
        public static string GraphCollection { get { return GetSetting(); } }
        public static string BatchAccountName { get { return GetSetting(); } }
        public static string BatchAccountKey { get { return GetSetting(); } }
        public static string BatchAccountUrl { get { return GetSetting(); } }
        public static string BatchAppStorageContainer { get { return GetSetting(); } }
        public static string BatchJobId { get { return GetSetting(); } }
        public static string BatchPoolId { get { return GetSetting(); } }
        public static string VirtualMachineSize { get { return GetSetting(); } }
        public static int GraphThroughput { get { return GetSettingInt(); } }
        public static int ComputeNodes { get { return GetSettingInt(); } }
        public static int ConnectionsPerProc { get { return GetSettingInt(); } }
        public static int FeedbackFrequency { get { return GetSettingInt(); } }
        public static int BatchSize { get { return GetSettingInt(); } }
        public static int RowsToLoad { get { return GetSettingInt(); } }
        public static int MaxDegreeOfParallelism { get { return GetSettingInt(); } }
        

        private static string GetSetting([CallerMemberName] string key = null)
        {
            return ConfigurationManager.AppSettings[key];
        }

        private static int GetSettingInt([CallerMemberName] string key = null)
        {
            return int.Parse(ConfigurationManager.AppSettings[key]);
        }
    }
}
