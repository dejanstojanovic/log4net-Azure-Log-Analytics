using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Log4net.AzureLogAnalytics
{
    internal class BufferReadScheduleJob : IJob
    {

        public void Execute(IJobExecutionContext context)
        {
            String workspaceId = context.JobDetail.JobDataMap.GetString("WorkspaceId");
            String sharedKey = context.JobDetail.JobDataMap.GetString("SharedKey");
            String logType = context.JobDetail.JobDataMap.GetString("LogType");

            ConcurrentQueue<LogAnalyticsLoggingEvent> loggingEvenQueue = context.JobDetail.JobDataMap.Get("LoggingEvenQueue") as ConcurrentQueue<LogAnalyticsLoggingEvent>;

            if (loggingEvenQueue.Count > 0)
            {
                loggingEvenQueue.TryDequeue(out LogAnalyticsLoggingEvent loggingEvent);
                if (loggingEvent != null)
                {
                    String jsonMessage = JsonConvert.SerializeObject(loggingEvent);
                    Post(jsonMessage, workspaceId, sharedKey,logType);
                }
            }
        }



        private void Post(String json, String workspaceId, String sharedKey, String logType, string apiVersion = "2016-04-01")
        {
            String requestUriString = $"https://{workspaceId}.ods.opinsights.azure.com/api/logs?api-version={apiVersion}";
            DateTime dateTime = DateTime.UtcNow;
            String dateString = dateTime.ToString("r");

            String signature;
            string message = $"POST\n{json.Length}\napplication/json\nx-ms-date:{dateString}\n/api/logs";
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            using (HMACSHA256 encryptor = new HMACSHA256(Convert.FromBase64String(sharedKey)))
            {
                signature = $"SharedKey {workspaceId}:{Convert.ToBase64String(encryptor.ComputeHash(bytes))}";
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
            request.ContentType = "application/json";
            request.Method = "POST";
            request.Headers["Log-Type"] = logType;
            request.Headers["x-ms-date"] = dateString;
            request.Headers["Authorization"] = signature;
            byte[] content = Encoding.UTF8.GetBytes(json);
            using (Stream requestStreamAsync = request.GetRequestStream())
            {
                requestStreamAsync.Write(content, 0, content.Length);
            }
            using (HttpWebResponse responseAsync = (HttpWebResponse)request.GetResponse())
            {
                if (responseAsync.StatusCode != HttpStatusCode.OK && responseAsync.StatusCode != HttpStatusCode.Accepted)
                {
                    Stream responseStream = responseAsync.GetResponseStream();
                    if (responseStream != null)
                    {
                        using (StreamReader streamReader = new StreamReader(responseStream))
                        {
                            throw new Exception(streamReader.ReadToEnd());
                        }
                    }
                }
            }
        }


    }
}
