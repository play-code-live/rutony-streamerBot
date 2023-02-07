using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace RutonyChat
{
    public class Script
    {
        private const string streamerBotWebserverAddress = "http://localhost:7474/";
        private const string endpointDoAction = "DoAction";
        private const string eventTypeDonate = "donate";

        private static string BackgroundWatcherDefaultAction;

        #region Background Script
        // Скрипты Автономного запуска
        public void InitParams(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                RutonyBot.SayToWindow("Необходимо указать целевое действие в параметрах автономного скрипта интеграции StreamerBot");
                throw new InvalidDataException("Необходим параметр для Action");
            }

            BackgroundWatcherDefaultAction = param;
            RutonyBot.SayToWindow("Отслеживание донатов с интеграцией StreamerBot запущено");
        }

        public void Closing()
        {
            RutonyBot.SayToWindow("Отслеживание донатов с интеграцией StreamerBot остановлено");
        }

        public static void NewDonate(string Site = "donationalerts", string Name = "test_user", string Text = "test text", float Amount = 123.45f, string Currency = "RUB")
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
            if (TypeEvent != eventTypeDonate)
                return;

            NewDonate(Site, Name, Text, Donate, Currency);
        }
        #endregion

        #region Remote Control Script
        // Скрипт Удаленного управления
        public void RunScript(string Text, string Param)
        {
            var Args = new Dictionary<string, string>() { { "message", Text } };
            DoAction(Param, Args);
        }
        #endregion

        #region Chat Bot Scripts
        // Скрипт Бота
        public void RunScript(string Site, string Username, string Text, string Param)
        {
            var Args = new Dictionary<string, string>()
            {
                { "site", Site },
                { "user", Username },
                { "userName", Username },
                { "message", Text }
            };

            DoAction(Param, Args);
        }

        public void RunScript(string Site, string Usename, string Text, Dictionary<string, string> Param)
        {
            foreach (var p in Param)
            {
                RutonyBot.SayToWindow(string.Format("Param {0} = {1}", p.Key, p.Value));
            }
        }
        #endregion

        #region Client Methods
        private static void DoAction(string Action, Dictionary<string, string> Args)
        {
            if (string.IsNullOrEmpty(Action)) {
                RutonyBot.SayToWindow("Ошибка. Необходимо указать название Action в параметрах вызова скрипта");
                return;
            }

            var Payload = new DoActionRequest
            {
                action = new DoActionRequestBody { name = Action, args = Args }
            };

            try
            {
                PerformRequest(endpointDoAction, Payload);
            } catch (Exception e)
            {
                int n;
                if (int.TryParse(Action, out n))
                {
                    // RutonyChat ломает обработку параметра, если завязываться на донат
                    // Вместо указанного параметра он подсовывает сумму доната
                    // Поэтому подобные случаи приходиться игнорировать
                    return;
                }
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
                var jsonPayload = JsonConvert.SerializeObject(Payload);
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
    }

    public struct DoActionRequestBody
    {
        public string name { get; set; }

        public Dictionary<string, string> args { get; set; }
    }
    #endregion
}