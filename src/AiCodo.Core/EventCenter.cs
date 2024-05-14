// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// This program file is open source and follows the MIT open source license.
// If you have any questions, please contact the author.
// You can use some or all of the code for personal or commercial use.
// When modifying the source code, please maintain the integrity of the original code
// to avoid problems caused by version upgrades.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace AiCodo
{
    public static class EventCenter
    {
        private static Dictionary<string, List<EventSubscribe>> _EventSubscribes = new Dictionary<string, List<EventSubscribe>>();

        private static ConcurrentQueue<EventItem> _Events = new ConcurrentQueue<EventItem>();

        private static object _EventsLock = new object();

        private static ManualResetEvent _ThreadEvent = new ManualResetEvent(true);

        private static int _ThreadCount = 0;

        private static bool _UseThreadPool = true;

        private static int _MaxThread = 5;

        public static EventHandler<EventData> NewEvent;

        struct EventItem
        {
            public object Sender { get; set; }

            public string Key { get; set; }

            public object Data { get; set; }
        }

        static EventCenter()
        {
        }

        public static void UseThreadPool(bool use)
        {
            _UseThreadPool = use;
        }

        public static void SetMaxThread(int count)
        {
            _MaxThread = count;
        }

        private static void TryStartNewThread()
        {
            if (_ThreadCount < _MaxThread)
            {
                Interlocked.Increment(ref _ThreadCount);
                Thread newThread = new Thread(new ThreadStart(HandlerEvents));
                newThread.IsBackground = true;
                newThread.Start();
                Debug.WriteLine(string.Format("thread count:{0},event count:{1}", _ThreadCount, _Events.Count));
            }
        }

        private static void HandlerEvents()
        {
            EventItem item;
            while (_Events.TryDequeue(out item))
            {
                DoEvent(item.Sender, item.Key, item.Data);
            }
            Interlocked.Decrement(ref _ThreadCount);
        }

        /// <summary>
        /// 发布一个事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public static void Publish(this object sender, string key, object data)
        {
            try
            {
                if (_UseThreadPool)
                {
                    ThreadPool.QueueUserWorkItem(obj => DoEvent(sender, key, data));
                    return;
                }
                _Events.Enqueue(new EventItem { Sender = sender, Key = key, Data = data });
                Debug.WriteLine("[{0}]\tEvent Publish[{1}]", DateTime.Now, key);
                TryStartNewThread();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private static void DoEvent(object sender, string key, object data)
        {
            try
            {
                NewEvent?.Invoke(sender, new EventData { Key = key, Data = data });
            }
            catch (Exception ex)
            {
                "EventCenter".Log($"执行事件处理出错：{ex.Message}", Category.Exception);
                //继续
            }

            if (!_EventSubscribes.ContainsKey(key))
            {
                return;
            }

            List<EventSubscribe> list = null;
            lock (_EventSubscribes)
            {
                if (!_EventSubscribes.TryGetValue(key, out list))
                {
                    return;
                }
            }
            Debug.WriteLine("[{0}]\tEvent Handler[{1}]", DateTime.Now, key);

            foreach (var item in list.ToList())
            {
                if (item.Subscriber != null && item.Subscriber == sender)
                {
                    continue;
                }
                try
                {
                    item.DoAction(data);
                }
                catch (Exception ex)
                {
                    ex.Message.WriteErrorLog();
                }
            }
        }

        /// <summary>
        /// 订阅key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        /// <param name="withAction"></param>
        public static void Subscribe(this object sender, string key, Action<object> withAction)
        {
            lock (_EventSubscribes)
            {
                try
                {
                    Debug.WriteLine("[{0}]\tEvent Subscribe[{1}]", DateTime.Now, key);
                    List<EventSubscribe> list = null;
                    if (_EventSubscribes.TryGetValue(key, out list))
                    {
                        var item = list.FirstOrDefault(e => e.Subscriber == sender);
                        if (item != null)
                        {
                            throw new Exception("事件不允许重复订阅");
                        }
                    }
                    else
                    {
                        list = new List<EventSubscribe>();
                        _EventSubscribes[key] = list;
                    }
                    list.Add(new EventSubscribe(sender, key, withAction));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 清除(sender)的(key)订阅
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        public static void UnSubscribe(this object sender, string key)
        {
            lock (_EventSubscribes)
            {
                try
                {
                    List<EventSubscribe> list = null;
                    if (_EventSubscribes.TryGetValue(key, out list))
                    {
                        var item = list.FirstOrDefault(e => e.Subscriber == sender);
                        if (item != null)
                        {
                            list.Remove(item);
                            if (list.Count == 0)
                            {
                                _EventSubscribes.Remove(key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 清除(sender)所有的订阅
        /// </summary>
        /// <param name="sender"></param>
        public static void ClearSubscribe(this object sender)
        {
            lock (_EventSubscribes)
            {
                try
                {
                    foreach (var eventItem in _EventSubscribes)
                    {
                        List<EventSubscribe> list = eventItem.Value;
                        var item = list.FirstOrDefault(e => e.Subscriber == sender);
                        if (item != null)
                        {
                            list.Remove(item);
                            if (list.Count == 0)
                            {
                                _EventSubscribes.Remove(eventItem.Key);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
        }
    }

    class EventSubscribe
    {
        public EventSubscribe(object subscriber, string key, Action<object> doAction)
        {
            Subscriber = subscriber;
            Key = key;
            DoAction = doAction;
        }

        public object Subscriber { get; set; }

        public string Key { get; set; }

        public Action<object> DoAction { get; set; }
    }

    public class EventData : EventArgs
    {
        public string Key { get; set; }

        public object Data { get; set; }
    }
}
