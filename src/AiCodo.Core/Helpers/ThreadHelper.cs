// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    public class DelayAction
    {
        private DateTime _ActiveTime = DateTime.Now;
        private bool _IsStart = false;
        private object _ThreadLock = new object();

        public DelayAction(Action action, int delaySeconds = 5)
        {
            _Do = action;
            _DelaySeconds = delaySeconds;
        }

        public bool IsStart
        {
            get
            {
                return _IsStart;
            }
        }

        #region 属性 Do
        private Action _Do = null;
        public Action Do
        {
            get
            {
                return _Do;
            }
            set
            {
                _Do = value;
            }
        }
        #endregion

        #region 属性 DelaySeconds
        private int _DelaySeconds = 5;
        public int DelaySeconds
        {
            get
            {
                return _DelaySeconds;
            }
            set
            {
                if (_DelaySeconds == value)
                {
                    return;
                }
                if (value < 1)
                {
                    throw new Exception("延时时间不能小于1秒");
                }
                _DelaySeconds = value;
            }
        }
        #endregion

        public void CheckStart()
        {
            _ActiveTime = DateTime.Now;
            if (_IsStart)
            {
                return;
            }
            lock (_ThreadLock)
            {
                if (_IsStart)
                {
                    return;
                }
                _IsStart = true;
                Threads.StartNew(Check);
            }
        }

        public void Cancel()
        {
            if (_IsStart)
            {
                _IsStart = false;
            }
        }

        private void Check()
        {
            while (_IsStart)
            {
                Thread.Sleep(_DelaySeconds * 1000);
                if (_ActiveTime.AddSeconds(_DelaySeconds) <= DateTime.Now)
                {
                    try
                    {
                        _Do?.Invoke();
                        break;
                    }
                    catch (Exception ex)
                    {
                        "DelayActon".Log(ex.Message);
                    }
                }
            }
            if (_IsStart)
            {
                _IsStart = false;
            }
        }
    }

    public static class Threads
    {
        static object _NewThreadLock = new object();

        static int _MaxNewThreads = 1000;

        static int _NewThreads = 0;

        static int _NewWorkThreads = 0;

        public static void SetMaxNewThreads(int maxNewThreads)
        {
            _MaxNewThreads = maxNewThreads;
        }

        private static void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                "Threads".Log(ex.Message);
            }
        }

        public static Thread StartNew(Action action, string name = "")
        {
            //lock (_NewThreadLock)
            //{
            //    if (_NewWorkThreads >= _MaxNewThreads)
            //    {
            //        AddToThreadPool(action, name);
            //        Debug.WriteLine($"threads {_NewWorkThreads}/ {_MaxNewThreads}");
            //        return null;
            //    }
            //}
            if (string.IsNullOrEmpty(name))
            {
                StackTrace trace = new StackTrace(true);
                if (trace.FrameCount > 1)
                {
                    var f = trace.GetFrame(1);
                    var m = f.GetMethod();
                    name = $"{m.Module.Name}-{m.MemberType}-{m.Name}-{f.GetFileLineNumber()}";
                }
            }

            Interlocked.Increment(ref _NewThreads);
            Interlocked.Increment(ref _NewWorkThreads);
            ThreadStart start = new ThreadStart(() =>
            {
                __SafeAction(action, name);
                Interlocked.Decrement(ref _NewWorkThreads);
            });

            Thread thr = new Thread(start);
            thr.Name = name;
            thr.IsBackground = true;
            thr.Start();
            return thr;
        }

        private static void __SafeAction(Action action, string name = "")
        {
            try
            {
                action();
                //"Threads".Log($"thread {name} end", Category.Debug);
            }
            catch (Exception ex)
            {
                "Threads".Log($"thread {name} {ex.ToString()}", Category.Exception);
            }
        }

        //private static void __SafeAction(object actionObject)
        //{
        //    try
        //    {
        //        ((Action)actionObject)();
        //        "Threads".Log($"thread end", Category.Debug);
        //    }
        //    catch (Exception ex)
        //    {
        //        "Threads".Log(ex.ToString(), Category.Exception);
        //    }
        //}

        /// <summary>
        /// 不是很着急的零碎任务
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static bool AddToThreadPool(Action action, string name = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                StackTrace trace = new StackTrace(true);
                if (trace.FrameCount > 1)
                {
                    var f = trace.GetFrame(1);
                    var m = f.GetMethod();
                    name = $"ThreadPool {m.Module.Name}-{m.MemberType}-{m.Name}-{f.GetFileLineNumber()}";
                }
            }
            return ThreadPool.QueueUserWorkItem((obj) =>
            {
                __SafeAction(action, name);
            });
        }

        public static void DelayRun(Action action, double checkSeconds, Func<bool> canRun, Func<bool> isCancel, Action callback = null)
        {
            StartNew(() =>
            {
                while (!isCancel())
                {
                    if (canRun())
                    {
                        action();
                        break;
                    }
                    Thread.Sleep((int)(checkSeconds * 1000));
                }
                callback?.Invoke();
            });
        }
    }
}
