using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace AutoMerge
{
    public static class JsonParser
    {
        public static Dictionary<string, string> ParseJson(string jsonText)
        {
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<Dictionary<string, string>>(jsonText) ?? new Dictionary<string, string>();
        }

        public static string ToJson(Dictionary<string, string> dict)
        {
            var entries = dict.Select(d =>
                string.Format("\"{0}\": \"{1}\"", d.Key, d.Value));
            return "{" + Environment.NewLine + "  " + string.Join(",\r\n  ", entries) + Environment.NewLine + "}";
        }
    }
}
