using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace TGInstaAudioToText
{
    public static class Sys
    {
        public const int GMT_RUSSIA = 3;

        public static string GetAppPath()
        {
#if DEBUG
            return System.AppDomain.CurrentDomain.BaseDirectory;
#else
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
#endif
            //System.AppDomain.CurrentDomain.BaseDirectory; //Doesn't work with PublishSingleFile
        }

        static string _AppExeName = null;
        public static string GetAppExeName()
        {
            if (_AppExeName == null)
            {
                _AppExeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            }
            return _AppExeName;
        }

        public static string GetConfigFileName()
        {
            return Path.Combine(GetAppPath(), Path.GetFileNameWithoutExtension(GetAppExeName())) + ".cfg";
        }

        public static string GetAppName()
        {
            return System.AppDomain.CurrentDomain.FriendlyName;
        }

        public static string TimeDefaultFormat(DateTime time, string Default = "N/A")
        {
            if (time == default(DateTime) || time.ToTimestamp64() == 0)
            {
                return Default;
            }
            return time.ToString("yyyy.MM.dd HH:mm:ss");
        }

        public static void FromString(string value, ref object to)
        {
            if (to is string)
            {
                to = value;
            }
            else if (to is int)
            {
                to = STR2INT_EX(value);
            }
            else if (to is bool)
            {
                to = STR2BOOL(value);
            }
            else if (to is Int64)
            {
                to = STR2INT64_EX(value);
            }
            else if (to is UInt32)
            {
                to = (UInt32)STR2INT64_EX(value);
            }
            else if (to is UInt16)
            {
                to = (UInt16)STR2INT64_EX(value);
            }
            else if (to is Int16)
            {
                to = (Int16)STR2INT64_EX(value);
            }
            else if (to is byte)
            {
                to = (byte)STR2INT64_EX(value);
            }
            else if (to is double)
            {
                to = STR2DOUBLE(value);
            }
            else if (to is decimal)
            {
                to = STR2DECIMAL(value);
            }
        }




        public static Int64 STR2INT64_EX(string s)
        {
            Int64 value = 0;
            if (s != null)
            {
                int len = s.Length;
                if (len > 0)
                {
                    if (s[0] == '-')
                    {
                        for (int i = 1; i < len; i++)
                        {
                            if (s[i] < '0' || s[i] > '9')
                            {
                                break;
                            }
                            value = value * 10 + (s[i] - '0');
                        }
                        return -value;
                    }
                    else
                    {
                        for (int i = 0; i < len; i++)
                        {
                            if (s[i] < '0' || s[i] > '9')
                            {
                                break;
                            }
                            value = value * 10 + (s[i] - '0');
                        }
                        return value;
                    }
                }
            }
            return 0;
        }
        public static int STR2INT_EX(string s)
        {
            int value = 0;
            int len = s.Length;
            if (s != null)
            {
                if (len > 0)
                {
                    if (s[0] == '-')
                    {
                        for (int i = 1; i < len; i++)
                        {
                            if (s[i] < '0' || s[i] > '9')
                            {
                                break;
                            }
                            value = value * 10 + (s[i] - '0');
                        }
                        return -value;
                    }
                    else
                    {
                        for (int i = 0; i < len; i++)
                        {
                            if (s[i] < '0' || s[i] > '9')
                            {
                                break;
                            }
                            value = value * 10 + (s[i] - '0');
                        }
                        return value;
                    }
                }
            }
            return 0;
        }

        public static bool STR2BOOL(string _str, bool Default = false)
        {
            if (_str == null)
            {
                return Default;
            }
            if (_str.Length == 0)
            {
                return Default;
            }
            if (_str == "true" || _str == "True")
            {
                return true;
            }
            else if (_str == "false" || _str == "False")
            {
                return false;
            }
            return (STR2INT_EX(_str) != 0);
        }

        public static decimal STR2DECIMAL(string _str, decimal def_val = 0)
        {
            if (_str == null)
            {
                return def_val;
            }
            decimal outVal;
            if (!decimal.TryParse(_str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out outVal))
            {
                outVal = def_val;
            }
            return outVal;
        }

        public static double STR2DOUBLE(string _str, double def_val = 0.0)
        {
            if (_str == null)
            {
                return def_val;
            }
            double outVal;
            if (!double.TryParse(_str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out outVal))
            {
                outVal = def_val;
            }
            return outVal;
        }
        public static UInt32 TimeToTimestamp(DateTime Time, int Hours = GMT_RUSSIA)
        {
            if (Time <= new DateTime(1970, 1, 1, Hours, 0, 0))
            {
                return 0;
            }
            TimeSpan _UnixTimeSpan = (Time - new DateTime(1970, 1, 1, Hours, 0, 0));
            return (UInt32)_UnixTimeSpan.TotalSeconds;
        }

        public static Int64 TimeToTimestamp64(DateTime Time, int Hours = GMT_RUSSIA)
        {
            if (Time <= new DateTime(1970, 1, 1, Hours, 0, 0))
            {
                return 0;
            }
            TimeSpan _UnixTimeSpan = (Time - new DateTime(1970, 1, 1, Hours, 0, 0));
            return (Int64)_UnixTimeSpan.TotalSeconds;
        }

        public static DateTime TimestampToTime(UInt32 time_t, int milliseconds = 0)
        {
            return (new DateTime(1970, 1, 1, GMT_RUSSIA, 0, 0)).AddSeconds(time_t).AddMilliseconds(milliseconds);
        }

        public static DateTime Timestamp64ToTime(Int64 time_t, int milliseconds = 0)
        {
            return (new DateTime(1970, 1, 1, GMT_RUSSIA, 0, 0)).AddSeconds(time_t).AddMilliseconds(milliseconds);
        }

        public static DateTime ToDateTime(this long value)
        {
            return Timestamp64ToTime((long)value);
        }
        public static DateTime ToDateTime(this uint value)
        {
            return TimestampToTime(value);
        }

        public static uint ToTimestamp(this DateTime value, int Hours = GMT_RUSSIA)
        {
            return TimeToTimestamp(value, Hours);
        }

        public static long ToTimestamp64(this DateTime value, int Hours = GMT_RUSSIA)
        {
            return TimeToTimestamp64(value, Hours);
        }

        public static string ToStringNoNull(this JToken obj, string defaultValue = "")
        {
            if (obj != null)
            {
                return obj.ToString();
            }
            return defaultValue;
        }
        public static bool GetMember(JToken obj, string name, out JToken member)
        {
            if (obj != null)
            {
                member = obj[name];
                return (member != null);
            }
            member = null;
            return false;
        }
        public static string GetString(JToken obj, string name, string defaultValue = "")
        {
            if (obj != null)
            {
                string value = obj[name]?.ToString();
                if (value != null)
                {
                    return value;
                }
            }
            return defaultValue;
        }

        public static JToken AutoCreateObject(JToken obj, string name)
        {
            try
            {
                JToken o = obj[name];
                if (o == null)
                {
                    obj[name] = new JObject();
                    return obj[name];
                }
                return o;
            }
            catch { }
            return new JObject();
        }
    }
}
