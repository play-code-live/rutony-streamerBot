using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

///-------------------------------------------------------------------
///   Module:     StreamerBotIntegration
///   Author:     play_code (https://twitch.tv/play_code)
///   Email:      info@play-code.live
///   Repository: https://github.com/play-code-live/rutony-streamerBot
///-------------------------------------------------------------------
namespace RutonyChat
{
    public class Script
    {
        private const string ScriptName = "Обработчик донатов";
        private const string targetEventType = eventTypeDonate;

        private const string streamerBotWebserverAddress = "http://localhost:7474/";
        private const string endpointDoAction = "DoAction";
        
        private const string eventTypeDonate = "donate";
        private const string eventTypeRank   = "rank_promote";
        private const string eventTypeRepost = "new_repost";
        private const string eventTypeLike   = "new_like";

        private static string BackgroundWatcherAction;

        #region Background Script
        public void InitParams(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                RutonyBot.SayToWindow("Необходимо указать целевое действие в параметрах автономного скрипта интеграции StreamerBot");
                throw new InvalidDataException("Необходим параметр для Action");
            }

            BackgroundWatcherAction = param;
            RutonyBot.SayToWindow(string.Format("StreamerBot: {0} запущен", ScriptName));
        }

        public void Closing()
        {
            RutonyBot.SayToWindow(string.Format("StreamerBot: {0} остановлен", ScriptName));
        }

        public static void NewDonate(string Site, string Name, string Text, float Amount, string Currency)
        {
            var Args = new Dictionary<string, string>()
            {
                { "site", Site },
                { "user", Name },
                { "userName", Name },
                { "message", Text },
                { "amount", Amount.ToString() },
                { "currency", Currency }
            };

            DoAction(BackgroundWatcherDefaultAction, Args);
        }

        public void NewMessage(string Site, string Name, string Text, bool System)
        {
        }

        public void NewAlert(string Site, string TypeEvent, string Subplan, string Name, string Text, float Donate, string Currency, int Qty)
        {
            if (TypeEvent != targetEventType)
                return;

            NewDonate(Site, Name, Text, Donate, Currency);
        }
        #endregion

        #region Client Methods
        private static void DoAction(string Action, Dictionary<string, string> Args)
        {
            if (string.IsNullOrEmpty(Action))
            {
                RutonyBot.SayToWindow("Ошибка. Необходимо указать название Action в параметрах вызова скрипта");
                return;
            }

            var Payload = new DoActionRequest
            {
                action = new DoActionRequestBody { name = Action },
                args   = Args
            };

            try
            {
                PerformRequest(endpointDoAction, Payload);
            }
            catch (Exception e)
            {
                RutonyBot.SayToWindow(  
                    string.Format("Ошибка. Не удалось вызвать Action StreamerBot: {0}", e.Message)
                );
            }
        }

        private static string PerformRequest(string Endpoint, object Payload = null)
        {
            var url = streamerBotWebserverAddress + Endpoint;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = "application/json";

            if (Payload != null)
            {
                var jsonSerializerSettings = new JsonSerializerSettings{StringEscapeHandling = StringEscapeHandling.EscapeNonAscii};
                var jsonPayload = JsonConvert.SerializeObject(Payload, jsonSerializerSettings);
                var requestBytes = Encoding.ASCII.GetBytes(jsonPayload);
                webRequest.ContentLength = requestBytes.Length;
                Stream requestStream = webRequest.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
            }


            var response = (HttpWebResponse)webRequest.GetResponse();
            if (!IsSuccessStatusCode(response.StatusCode))
                throw new Exception(string.Format("server responded with {0}", response.StatusCode));

            string jsonResponse = "";
            using (Stream respStr = response.GetResponseStream())
            {
                using (StreamReader rdr = new StreamReader(respStr, Encoding.UTF8))
                {
                    jsonResponse = rdr.ReadToEnd();
                    rdr.Close();
                }
            }

            return jsonResponse;
        }

        private static bool IsSuccessStatusCode(HttpStatusCode StatusCode)
        {
            return ((int)StatusCode) >= 200 && ((int)StatusCode) <= 299;
        }
        #endregion
    }

    #region Structs/DTOs
    public struct DoActionRequest
    {
        public DoActionRequestBody action { get; set; }
        public Dictionary<string, string> args { get; set; }
    }

    public struct DoActionRequestBody
    {
        public string name { get; set; }
    }
    #endregion
}