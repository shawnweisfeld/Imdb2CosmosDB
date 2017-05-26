using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace ImdbSite.Util
{
    public static class Settings
    {
        public static string GraphKey { get { return GetSetting(); } }
        public static string GraphDatabase { get { return GetSetting(); } }
        public static string GraphEndpoint { get { return GetSetting(); } }
        public static string GraphCollection { get { return GetSetting(); } }
        public static int GraphThroughput { get { return GetSettingInt(); } }
        public static int ConnectionsPerProc { get { return GetSettingInt(); } }

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