// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Diagnostics;

    public class ParallelSortQueue<T> : ParallelQueueBase<T>
    {
        SortedSet<T> _Queue = null;
        Func<T, bool> _ItemReady = null;
        Object _QueueLock = new object();

        public override int ItemCount { get { return _Queue.Count; } }

        public ParallelSortQueue(Action<T> itemAction, int threadCount, IComparer<T> comparer = null, Func<T, bool> itemReady = null) : base(itemAction, threadCount)
        {
            _Queue = comparer == null ? new SortedSet<T>() : new SortedSet<T>(comparer);
            _ItemReady = itemReady;
        }

        public override void AddItem(T item)
        {
            lock (_QueueLock)
            {
                _Queue.Add(item);
            }
        }

        protected override T GetItem()
        {
            if (_Queue.Count > 0)
            {
                lock (_QueueLock)
                {
                    if (_ItemReady == null)
                    {
                        var item = _Queue.FirstOrDefault();
                        if (item != null)
                        {
                            _Queue.Remove(item);
                        }
                        return item;
                    }
                    var ready = _ItemReady;
                    foreach (var item in _Queue.ToList())
                    {
                        if (ready(item))
                        {
                            var ok = _Queue.Remove(item);
                            if (ok == false)
                            {
                                $"删除实验队列元素失败{item}".WriteErrorLog();
                                continue;
                            }
                            return item;
                        }
                    }
                }
            }
            return default(T);
        }
    }

    public class ParallelQueue<T> : ParallelQueueBase<T>
    {
        Queue<T> _Queue = new Queue<T>();

        public override int ItemCount { get { return _Queue.Count; } }

        public ParallelQueue(Action<T> itemAction, int threadCount) : base(itemAction, threadCount)
        {

        }

        public override void AddItem(T item)
        {
            lock (_Queue)
            {
                _Queue.Enqueue(item);
            }
        }
        protected override T GetItem()
        {
            if (_Queue.Count == 0)
            {
                return default(T);
            }
            lock (_Queue)
            {
#if NETSTANDARD2_0
                if (_Queue.Count > 0)
                {
                    return _Queue.Dequeue();
                }
#else
                if (_Queue.Count > 0 && _Queue.TryDequeue(out T item))
                {
                    return item;
                }
#endif
                return default(T);
            }
        }
    }

    #region Thread Item
    class ParallelQueueThreadItem<T>
    {
        public event EventHandler Completed;

        public int Index { get; set; } = 0;

        public bool Enable { get; set; } = true;

        public Func<bool> CanRun { get; set; }

        public Action<T> Action { get; set; }

        public Func<T> GetItem { get; set; }

        public bool IsRunning { get; set; } = false;

        TimeSpan _RunTime = TimeSpan.Zero;
        DateTime _StartTime = DateTime.Now;

        int _DoneCount = 0;

        int _ErrorCount = 0;

        public int DoneCount
        {
            get
            {
                return _DoneCount;
            }
        }

        public int ErrorCount
        {
            get
            {
                return _ErrorCount;
            }
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            _StartTime = DateTime.Now;
            Threads.StartNew(Run);
        }

        private void Run()
        {
            while (Enable && CanRun())
            {
                var item = GetItem();
                if (item != null)
                {
                    try
                    {
                        var start = DateTime.Now;
                        Action(item);
                        var spendTime = DateTime.Now - start;
                        _RunTime += spendTime;

                        Interlocked.Increment(ref _DoneCount);
                        this.Log($"Thread:[{Index}], done:[{_DoneCount}],{spendTime}");
                        Thread.Yield();
                    }
                    catch (Exception ex)
                    {
                        //不应该报错
                        Interlocked.Increment(ref _ErrorCount);
                        $"批量线程内部出错：{ex.ToString()}".WriteErrorLog();
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
            IsRunning = false;
            this.Log($"队列线程[{Index}]退出，完成[{_DoneCount}]，错误[{_ErrorCount}]，运行时间[{(DateTime.Now - _StartTime).TotalSeconds}s]，执行耗时[{_RunTime.TotalSeconds}s]");
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
    #endregion

    public abstract class ParallelQueueBase<T>
    {
        public string ID { get; } = DateTime.Now.ToString("HHmmss");

        private bool _CanRun = false;

        public Action<T> ItemAction { get; set; }

        public int ThreadCount { get; private set; } = 5;

        public abstract int ItemCount { get; }

        public bool IsSealed { get; private set; } = false;

        List<ParallelQueueThreadItem<T>> _ThreadItems = new List<ParallelQueueThreadItem<T>>();

        object _ThreadLock = new object();

        ManualResetEvent _DoneEvent = new ManualResetEvent(false);

        int _DoneCount = 0;

        protected abstract T GetItem();
        public abstract void AddItem(T item);

        public ParallelQueueBase()
        {
        }

        public ParallelQueueBase(int threadCount)
        {
            ThreadCount = threadCount;
        }

        public ParallelQueueBase(Action<T> itemAction, int threadCount = 5)
        {
            ItemAction = itemAction;
            ThreadCount = threadCount;
        }

        public bool CanRun()
        {
            return _CanRun;
        }

        public virtual void Cancel()
        {
            if (_CanRun)
            {
                _CanRun = false;
            }
        }

        public void Sealed()
        {
            IsSealed = true;
        }

        //public void AddThread()
        //{
        //    lock (_ThreadLock)
        //    {
        //        ThreadCount++;
        //        AddThread(ThreadCount);
        //    }
        //}

        public Task<int> Start()
        {
            _CanRun = true;
            if (ItemAction == null)
            {
                throw new ArgumentException("ItemAction没有设置", "ItemAction");
            }

            return Task.Run(() =>
            {

                if (!_CanRun)
                {
                    return 0;
                }

                var threadCount = ThreadCount;
                _DoneEvent.Reset();
                for (int i = 0; i < threadCount; i++)
                {
                    AddThread(i + 1);
                }
                _DoneEvent.WaitOne();
                return _DoneCount;
            });
        }

        protected virtual bool CanThreadRun()
        {
            return _CanRun && (IsSealed == false || ItemCount > 0);
        }

        protected void AddThread(int index)
        {
            var threadItem = new ParallelQueueThreadItem<T>
            {
                Index = index,
                Action = ItemAction,
                CanRun = CanThreadRun,
                GetItem = GetItem,
                Enable = true
            };
            threadItem.Completed += ThreadItem_Completed;
            lock (_ThreadLock)
            {
                _ThreadItems.Add(threadItem);
                threadItem.Start();
            }
        }

        private void ThreadItem_Completed(object sender, EventArgs e)
        {
            if (sender is ParallelQueueThreadItem<T> item)
            {
                item.Completed -= ThreadItem_Completed;
                lock (_ThreadLock)
                {
                    _DoneCount += item.DoneCount;
                    _ThreadItems.Remove(item);
                    if (_ThreadItems.Count == 0)
                    {
                        _DoneEvent.Set();
                    }
                }
            }
        }
    }
}
