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
    using System.Threading;

    public class TimingThread
    {
        private bool _Checking = false;
        private int CheckSecond { get; set; }
        private int Heartbeat { get; set; }
        //启动延时
        private int StartDelay { get; set; }
        private Action CheckingAction { get; set; }

        private Thread _CurrentThread { get; set; }

        private static List<TimingThread> _RuningThreads = new List<TimingThread>();

        /// <summary>
        /// 定时执行线程：每隔checkSecond秒执行一次checkingAction，
        /// 每隔heartbeat（默认5s，一般小于checkSecond）检查一次是否达到执行时间和是否停止线程
        /// </summary>
        /// <param name="name">线程名称</param>
        /// <param name="checkSecond">间隔（秒）</param>
        /// <param name="startDelay">启动延时（秒）</param>
        /// <param name="checkingAction">执行后函数</param>
        /// <param name="heartbeat">心跳间隔</param>
        public TimingThread(string name, int checkSecond, int startDelay, Action checkingAction, int heartbeat)
        {
            Name = name;
            CheckSecond = checkSecond;
            StartDelay = startDelay;
            Heartbeat = heartbeat;
            CheckingAction = checkingAction;
        }

        public TimingThread(string name, int checkSecond, int startDelay, Action checkingAction) :
            this(name, checkSecond, startDelay, checkingAction, 5)
        {
        }

        #region 属性 Name
        private string _Name = string.Empty;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
        #endregion

        #region 属性 IsChecking
        public bool IsChecking
        {
            get
            {
                return _Checking;
            }
        }
        #endregion

        #region 属性 ExecuteCount
        private int _ExecuteCount = 0;
        public int ExecuteCount
        {
            get
            {
                return _ExecuteCount;
            }
            private set
            {
                _ExecuteCount = value;
            }
        }
        #endregion

        #region 属性 TotalTime
        private TimeSpan _TotalTime = TimeSpan.Zero;
        public TimeSpan TotalTime
        {
            get
            {
                return _TotalTime;
            }
            private set
            {
                _TotalTime = value;
            }
        }
        #endregion

        #region 属性 ThreadID
        public int ThreadID
        {
            get
            {
                return _CurrentThread == null ? 0 : _CurrentThread.ManagedThreadId;
            }
        }
        #endregion

        #region 属性 ThreadState 
        public System.Threading.ThreadState ThreadState
        {
            get
            {
                return _CurrentThread == null ?
                    System.Threading.ThreadState.Unstarted
                    : _CurrentThread.ThreadState;
            }
        }
        #endregion

        public static IEnumerable<TimingThread> GetAll()
        {
            return _RuningThreads.ToList();
        }

        public static void StopAll()
        {
            lock (_RuningThreads)
            {
                foreach (var thr in _RuningThreads.ToList())
                {
                    thr.Stop();
                }
                _RuningThreads.Clear();
            }
        }

        public TimingThread Start()
        {
            lock (_RuningThreads)
            {
                if (!_RuningThreads.Contains(this))
                {
                    _RuningThreads.Add(this);
                }
            }
            StartLinkChecking();
            return this;
        }

        public void Stop()
        {
            lock (_RuningThreads)
            {
                if (_RuningThreads.Contains(this))
                {
                    _RuningThreads.Remove(this);
                }
            }
            StopLinkChecking();
        }

        public void ChangeInterval(int checkSecond)
        {
            CheckSecond = checkSecond;
        }

        private void StartLinkChecking()
        {
            _Checking = true;
            _CurrentThread = Threads.StartNew(Checking);
        }
        private void StopLinkChecking()
        {
            _Checking = false;
        }

        private void Checking()
        {
            long _nextTime = DateTime.Now.AddSeconds(StartDelay).Ticks;
            while (_Checking)
            {
                if (_nextTime <= DateTime.Now.Ticks)
                {
                    //先计算下次执行时间
                    _nextTime = DateTime.Now.AddSeconds(CheckSecond).Ticks;
                    //执行
                    try
                    {
                        var start = DateTime.Now;
                        Debug.WriteLine(string.Format("{0}-{1}:开始执行", Name, DateTime.Now));
                        CheckingAction();
                        _ExecuteCount++;
                        TotalTime = TotalTime.Add(DateTime.Now - start);
                        Debug.WriteLine(string.Format("{0}-{1}:执行完成，下一次{2}", Name, DateTime.Now, _nextTime));
                    }
                    catch (Exception ex)
                    {
                        string.Format("线程名称:{0}\t错误:{1}", Name, ex.ToString()).WriteErrorLog();
                    }
                }

                Thread.Sleep(Heartbeat);
            }
        }

        public void RunOnce()
        {
            CheckingAction?.Invoke();
        }
    }

    public enum TimingType
    {
        /// <summary>
        /// 固定间隔执行
        /// </summary>
        Period,
        /// <summary>
        /// 每天某个时间执行
        /// </summary>
        EveryDay,
        /// <summary>
        /// 每周某天/时间执行
        /// </summary>
        Week,
        /// <summary>
        /// 每月某天/时间执行
        /// </summary>
        Month
    }
}
