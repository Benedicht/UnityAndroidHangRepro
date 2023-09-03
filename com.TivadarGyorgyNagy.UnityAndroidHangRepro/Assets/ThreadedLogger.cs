using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

internal static class ThreadedLogger
{
    struct LogJob
    {
        public string div;
        public string msg;

        public DateTime time;
        public int threadId;
        public string stackTrace;
    }

    static ConcurrentQueue<LogJob> messageQueue = new ConcurrentQueue<LogJob>();
    static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

    public static void Start()
    {
        var _task = new Task(LogThread, TaskCreationOptions.LongRunning);
        _task.ConfigureAwait(false);
        _task.Start();
    }

    public static void EnqueueMessage(string message)
    {
        messageQueue.Enqueue(new LogJob { msg = message, threadId = Thread.CurrentThread.ManagedThreadId, stackTrace = Environment.StackTrace, time = DateTime.Now });
        autoResetEvent.Set();
    }

    private static void LogThread()
    {
        System.Threading.Thread.CurrentThread.Name = "LogThread";
        while (true)
        {
            while (messageQueue.TryDequeue(out LogJob log))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"[{log.time.ToBinary()}] ");
                sb.Append($"[{log.threadId}] ");
                sb.Append(log.msg);

                Debug.Log(sb.ToString());
            }

            autoResetEvent.WaitOne();
        }
    }

}
