﻿using BookingPlatform.Models.LogManage;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace BookingPlatform_InitialHospital
{
    /// <summary>
    /// sqlsugar 使用类
    /// </summary>
    public class SqlSugarManager
    {
        private readonly static String CONNECTION_STRING = System.Configuration.ConfigurationManager.AppSettings["dbConntion"].Trim();
        public static SqlSugarClient DB
        {
            get
            {
                return new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = CONNECTION_STRING,//必填, 数据库连接字符串
                    DbType = SqlSugar.DbType.MySql,         //必填, 数据库类型
                    IsAutoCloseConnection = true,       //默认false, 时候知道关闭数据库连接, 设置为true无需使用using或者Close操作
                    InitKeyType = InitKeyType.SystemTable,    //默认SystemTable, 字段信息读取, 如：该属性是不是主键，是不是标识列等等信息
                    IsShardSameThread = true    //同一线程使用同意连接。跨方法事务不需要去传递db连接
                });
            }


        }
    }

    /// <summary>
    /// 公共处理方法
    /// </summary>
    public class CommonHandleMethod
    {
        /// <summary>
        /// 获取ID
        /// </summary>
        public static string GetID()
        {
            return DateTime.Now.ToString("yyMMddHHmmssfff") + "_" + GenerateStringID().ToUpper();
        }

        /// <summary>
        /// ID生成方法
        /// </summary>
        private static string GenerateStringID()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                i *= ((int)b + 1);
            }

            string re = string.Format("{0:x}", i - DateTime.Now.Ticks);
            if (re.Length < 16) return re.PadRight(16, 'F');
            else return re.Substring(0, 16);
        }
        /// <summary>
        /// 转换List 为字符串
        /// </summary>
        /// <param name="listStr"></param>
        /// <returns></returns>
        public static string TransferListToStr(List<string> listStr)
        {
            var returnList = new List<string>();
            listStr.ForEach(item =>
            {
                item = "'" + item + "'";
                returnList.Add(item);
            });

            return string.Join(",", returnList);
        }
        /// <summary>
        /// 转换List 为字符串
        /// </summary>
        /// <param name="listInt"></param>
        /// <returns></returns>
        public static string TransferIntListToStr(List<int> listInt)
        {
            var returnList = new List<string>();
            listInt.ForEach(item =>
            {
                var s = "'" + item.ToString() + "'";
                returnList.Add(s);
            });

            return string.Join(",", returnList);
        }

        /// <summary>
        /// 针对【号源预约有效期】和【申请单预约有效期】配置，进行数据转换 
        /// </summary>
        /// <param name="flagData">具体配置标识值</param>
        /// <param name="flag">传参标识，0：表示号源预约有效期，1：表示申请单预约有效期</param>
        public static int TransferToEnumData(int flagData, int flag = 0)
        {
            var returnData = 0;
            if (flag == 0)//号源有效期
            {
                switch (flagData)
                {
                    case 0:
                        returnData = 7;//7天
                        break;
                    case 1:
                        returnData = 14;//14天
                        break;
                    case 2:
                        returnData = 30;
                        break;
                    case 3:
                        returnData = 60;
                        break;
                }
            }
            else//申请单有效期
            {
                switch (flagData)
                {
                    case 0:
                        returnData = -1;//无限制
                        break;
                    case 1:
                        returnData = 7;//7天
                        break;
                    case 2:
                        returnData = 14;//14天
                        break;
                    case 3:
                        returnData = 30;//30天
                        break;
                }
            }
            return returnData;
        }

        /// <summary>
        /// 传入号源时间长度，返回需要生成队列排班的周数
        /// </summary>
        /// <param name="dayNums">号源时间长度</param>
        /// <returns></returns>
        public static int GenerateArrangeWeeks(int dayNums)
        {
            try
            {
                var weekNum = 0;
                var devision = dayNums / 7;
                var remainCount = dayNums % 7;
                if (remainCount == 0)
                {
                    weekNum = devision;
                }
                else
                {
                    weekNum = devision + 1;
                }
                return weekNum;
            }
            catch (Exception ex)
            {
                YLog.LogSystemError(ex.Message);
                throw (ex);
            }
        }

        /// <summary>
        /// 获取本周的第一天(以星期一为第一天)
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTime GetWeekFirstDayMon(DateTime datetime)
        {
            //星期一为第一天  
            int weeknow = Convert.ToInt32(datetime.DayOfWeek);

            //因为是以星期一为第一天，所以要判断weeknow等于0时，要向前推6天。  
            weeknow = (weeknow == 0 ? (7 - 1) : (weeknow - 1));
            int daydiff = (-1) * weeknow;

            //本周第一天  
            string FirstDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
            return Convert.ToDateTime(FirstDay);
        }
        /// <summary>
        /// 获取本周的最后一天(以星期日为最后一天)
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTime GetWeekLastDaySun(DateTime datetime)
        {
            //星期天为最后一天  
            int weeknow = Convert.ToInt32(datetime.DayOfWeek);
            weeknow = (weeknow == 0 ? 7 : weeknow);
            int daydiff = (7 - weeknow);

            //本周最后一天  
            string LastDay = datetime.AddDays(daydiff).ToString("yyyy-MM-dd");
            return Convert.ToDateTime(LastDay);
        }

        /// <summary>
        /// 计算具体某个日期是星期几
        /// </summary>        
        /// <param name="dt">日期</param>
        /// <returns></returns>
        public static string CaculateWeekDay(DateTime dt)
        {
            var year = dt.Year;
            var month = dt.Month;
            var day = dt.Day;
            if (month == 1 || month == 2)
            {
                month += 12;
                year--;
            }
            int week = (day + 2 * month + 3 * (month + 1) / 5 + year + year / 4 - year / 100 + year / 400) % 7;
            string weekstr = "";
            switch (week)
            {
                case 0: weekstr = "星期一"; break;
                case 1: weekstr = "星期二"; break;
                case 2: weekstr = "星期三"; break;
                case 3: weekstr = "星期四"; break;
                case 4: weekstr = "星期五"; break;
                case 5: weekstr = "星期六"; break;
                case 6: weekstr = "星期日"; break;
            }
            return weekstr;
        }

        /// <summary>
        /// 根据传入的天数返回需要添加的排班周
        /// </summary>
        /// <param name="dayCount"></param>
        /// <returns></returns>
        public static int ReturnWeekNums(int dayCount)
        {
            var returnData = 1;
            if ((dayCount / 7 < 1 && dayCount % 7 > 0) || (dayCount / 7 == 1 && dayCount % 7 == 0))
            {
                returnData = 1;
            }
            else if (dayCount / 7 >= 1 && dayCount / 7 < 2 && dayCount % 7 > 0)
            {
                returnData = 2;
            }
            else if (dayCount / 7 >= 2 && dayCount / 7 < 3 && dayCount % 7 > 0)
            {
                returnData = 3;
            }
            else if (dayCount / 7 >= 3 && dayCount / 7 < 4 && dayCount % 7 > 0)
            {
                returnData = 4;
            }
            else if (dayCount / 7 >= 4 && dayCount / 7 < 5 && dayCount % 7 > 0)
            {
                returnData = 5;
            }
            return returnData;
        }
        /// <summary>
        /// 计算时间差值
        /// </summary>
        /// <param name="DateTime1"></param>
        /// <param name="DateTime2"></param>
        /// <returns></returns>
        public static int DateDiff(DateTime DateTime1, DateTime DateTime2)
        {
            try
            {
                TimeSpan ts1 = new TimeSpan(DateTime1.Ticks);
                TimeSpan ts2 = new TimeSpan(DateTime2.Ticks);
                TimeSpan ts = ts1.Subtract(ts2);
                return ts.Days;
            }
            catch (Exception ex)
            {
                LogManage.LogError(ex.Message + "--------" + ex.StackTrace);
                return -1;
            }
        }

        /// <summary>
        /// 通过传入的检查项目时段ID获取时段名称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetExamItemPeriodNameByID(int id)
        {
            switch (id)
            {
                case 0:
                    return "无限制";
                case 1:
                    return "上午";
                case 2:
                    return "中午";
                case 3:
                    return "下午";
                case 4:
                    return "夜间";
                default:
                    return "无限制";
            }
        }
    }

    /// <summary>
    /// 获取GUID
    /// </summary>
    public static class CreateID
    {
        /// <summary>
        /// 生成业务主键
        /// </summary>
        public static string GetID()
        {
            return DateTime.Now.ToString("yyMMddHHmmssfff") + "_" + GenerateStringID().ToUpper();
        }

        /// <summary>
        /// 
        /// </summary>
        private static string GenerateStringID()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                i *= ((int)b + 1);
            }

            string re = string.Format("{0:x}", i - DateTime.Now.Ticks);
            if (re.Length < 16) return re.PadRight(16, 'F');
            else return re.Substring(0, 16);
        }
    }

    /// <summary>
    /// API_Get处理类
    /// </summary>
    public class MyWebApi_Get
    {
        /// <summary>  
        /// 指定Post地址使用Get 方式获取全部字符串  
        /// </summary>  
        /// <param name="url">请求后台地址</param>  
        /// <returns></returns>  
        public static string Get(string url)
        {
            var sbUrl = new StringBuilder(url);
            var lastUrl = sbUrl.ToString();
            string result = "";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var tmpResult = client.GetAsync(lastUrl).Result;
                tmpResult.EnsureSuccessStatusCode();
                result = tmpResult.Content.ReadAsStringAsync().Result;
            }

            return result;
        }

        /// <summary>  
        /// 指定Post地址使用Get 方式获取全部字符串  
        /// </summary>  
        /// <param name="url">请求后台地址</param>  
        /// <returns></returns>  
        public static string Get(string url, Dictionary<string, string> dic, string appID)
        {
            return Get(url, dic, appID, string.Empty);
        }

        /// <summary>  
        /// 指定Post地址使用Get 方式获取全部字符串  
        /// </summary>  
        /// <param name="url">请求后台地址</param>  
        /// <returns></returns>  
        public static string Get(string url, Dictionary<string, string> dic, string appID, string token)
        {
            var sbUrl = new StringBuilder(url);
            if (dic != null && dic.Count > 0)
            {
                sbUrl.Append("?");
                int index = 0;
                foreach (var item in dic)
                {
                    sbUrl.Append(string.Format("{0}={1}", item.Key,
                        HttpUtility.UrlEncode(item.Value, Encoding.UTF8)));
                    if (index < dic.Count - 1)
                    {
                        sbUrl.Append("&");
                    }
                    index++;
                }
            }
            var lastUrl = sbUrl.ToString();
            string result = "";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("appid", appID);
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Add("token", token);
                }
                YLog.LogInfo($"调用地址:{lastUrl}");
                var tmpResult = client.GetAsync(lastUrl).Result;
                tmpResult.EnsureSuccessStatusCode();
                result = tmpResult.Content.ReadAsStringAsync().Result;
            }

            return result;
        }
    }
}
