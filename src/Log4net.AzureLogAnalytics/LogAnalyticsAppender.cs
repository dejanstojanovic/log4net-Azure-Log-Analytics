using log4net.Appender;
using log4net.Core;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Log4net.AzureLogAnalytics
{
    public class LogAnalyticsAppender : AppenderSkeleton
    {
        #region Fileds
        private readonly ConcurrentQueue<LogAnalyticsLoggingEvent> azureLoggingEventQueue;
        private readonly IScheduler scheduler;
        private String workspaceId;
        private String sharedKey;
        private String logType;
        private int bufferTimeout = 500;

        #endregion

        #region Properties
        string WorkspaceId
        {
            get
            {
                return this.workspaceId;
            }
            set
            {
                this.workspaceId = value;
                initilizeLogger();
            }
        }

        string SharedKey
        {
            get
            {
                return this.sharedKey;
            }
            set
            {
                this.sharedKey = value;
                initilizeLogger();
            }
        }

        string LogType
        {
            get
            {
                return this.logType;

            }
            set
            {
                this.logType = value;
                initilizeLogger();
            }
        }

        #endregion


        #region Constructors
        public LogAnalyticsAppender()
        {
            this.azureLoggingEventQueue = new ConcurrentQueue<LogAnalyticsLoggingEvent>();
            this.scheduler = StdSchedulerFactory.GetDefaultScheduler();

        }
        #endregion

        #region Methods
        private void initilizeLogger()
        {
            if (!String.IsNullOrWhiteSpace(workspaceId) && !String.IsNullOrWhiteSpace(sharedKey) && !String.IsNullOrWhiteSpace(logType))
            {
                scheduler.Start();
                IDictionary<String, Object> map = new Dictionary<String, Object>() {
                { "LoggingEvenQueue", this.azureLoggingEventQueue },
                { "SharedKey",this.SharedKey },
                { "WorkspaceId",this.WorkspaceId },
                { "LogType",this.LogType }
            };

                IJobDetail job = JobBuilder.Create<BufferReadScheduleJob>()
                                           .UsingJobData(new JobDataMap(map))
                                           .Build();

                ITrigger trigger = TriggerBuilder.Create()
                     .WithSimpleSchedule
                      (s =>
                         s.WithInterval(TimeSpan.FromMilliseconds(bufferTimeout))
                         .RepeatForever()
                      )
                    .Build();

                scheduler.ScheduleJob(job, trigger);
            }
        }
        #endregion

        #region Abstract implementation methods
        protected override void Append(LoggingEvent loggingEvent)
        {
            var serializableEvent = new LogAnalyticsLoggingEvent(loggingEvent,
                () =>
                {
                    if (Layout != null)
                    {
                        using (StringWriter writer = new StringWriter())
                        {
                            Layout.Format(writer, loggingEvent);
                            return writer.ToString();
                        }
                    }
                    return loggingEvent.RenderedMessage;
                });

            azureLoggingEventQueue.Enqueue(serializableEvent);
        }

        protected override void OnClose()
        {
            if (!scheduler.IsShutdown)
            {
                scheduler.Shutdown();
            }

            base.OnClose();
        }
        #endregion
    }
}
