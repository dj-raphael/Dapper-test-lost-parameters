using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DapperLostParameters
{
    public static class ExceptionDataHelper
    {

        public static string SqlExceptionDataKey
        {
            get { return "sql-exception-details"; }
        }

        /// <summary> 
        ///     Get info about sql script in which occurred error. 
        ///     This info to be used in NLog extension.
        /// </summary>
        public static KeyValuePair<string, string> GetExceptionDataSqlInfo(string sql)
        {
            return new KeyValuePair<string, string>(SqlExceptionDataKey, string.Format("Command: {0}"));
        }

        /// <summary> 
        ///     Get info about sql script in which occurred error. 
        ///     This info to be used in NLog extension.
        /// </summary>
        public static KeyValuePair<string, string> GetExceptionDataSqlInfo(string sql, object[] parameterValues)
        {
            var parameters = parameterValues.Where(o => o != null).ToArray();
            return new KeyValuePair<string, string>(SqlExceptionDataKey, string.Format("Command: {0}\nParameter values: {1}", sql ?? "", string.Join(";", parameters)));
        }

        /// <summary> 
        ///     Get info about sql script in which occurred error. 
        ///     This info to be used in NLog extension.
        /// </summary>
        public static KeyValuePair<string, string> GetExceptionDataSqlInfo(string sql, object parameterValues)
        {
            var parameters = "";
            if (parameterValues != null)
            {
                var dict = parameterValues as IDictionary<string, object>;
                if (dict != null)
                {
                    parameters = string.Join(";\n", dict.ToDictionary(p => p.Key, p => FormatValue(p.Value, p.Value?.GetType()?.Name)));
                }
                else
                {
                    var inst = parameterValues.GetType().GetProperties();
                    parameters = string.Join(";\n", inst.ToDictionary(p => p.Name, p => FormatValue(p.GetValue(parameterValues, null), p.PropertyType.ToString())));
                }
            };
            return new KeyValuePair<string, string>(SqlExceptionDataKey, $"Command: {sql}\nParameter values:\n{parameters}");
        }

        private static string FormatValue(object val, string typeName)
        {
            IEnumerable items = val as IEnumerable;
            if (items != null && !(items is string))
            {
                var i = 0;
                var res = new StringBuilder(items.GetType() + "{qty}[");
                foreach (var o in items)
                {
                    res.Append((o?.ToString() ?? "null") + ", ");
                    i++;
                }

                if (i == 0)
                {
                    res.Replace("[]{qty}[", "[0]");
                    res.Replace("]{qty}[", "][0]");
                }
                else
                {
                    res.Replace("[]{qty}[", "[" + i + "](");
                    res.Replace("]{qty}[", "][" + i + "](");
                    res.Length -= 2;
                    res.Append(")");
                }
                return res.ToString();
            }
            else
            {
                if (val is string)
                {
                    return "\"" + val + "\"";
                }
                else if (val == null)
                {
                    return typeName + ":null";
                }
                else
                {
                    return val.ToString();
                }
            }
        }

        public static void AppendExceptionData(this DbException exception, string sql)
        {
            var eInfo = GetExceptionDataSqlInfo(sql);
            exception.Data.Add(eInfo.Key, eInfo.Value);
        }

        public static void AppendExceptionData(this DbException exception, string sql, object[] parameterValues)
        {
            var eInfo = GetExceptionDataSqlInfo(sql, parameterValues);
            exception.Data.Add(eInfo.Key, eInfo.Value);
        }

        public static void AppendExceptionData(this DbException exception, string sql, object parameterValues)
        {
            var eInfo = GetExceptionDataSqlInfo(sql, parameterValues);
            exception.Data.Add(eInfo.Key, eInfo.Value);
        }

        public static void AppendExceptionData(this Exception exception, string info)
        {
            exception.Data.Add(SqlExceptionDataKey, info);
        }

        public static void AppendExceptionData(this Exception exception, string sql, object parameterValues)
        {
            var eInfo = GetExceptionDataSqlInfo(sql, parameterValues);
            exception.Data.Add(eInfo.Key, eInfo.Value);
        }
    }
}