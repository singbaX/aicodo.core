// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
namespace AiCodo.Flow.Configs
{
    using DynamicExpresso;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Xml.Serialization;

    public partial class FlowInputParameter : ParameterBase
    {
        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute("DefaultValue"), DefaultValue("")]
        public string DefaultValue
        {
            get
            {
                return _DefaultValue;
            }
            set
            {
                _DefaultValue = value;
                RaisePropertyChanged("DefaultValue");
            }
        }
        #endregion

        #region 属性 Ref
        private string _Ref = string.Empty;
        [XmlAttribute("Ref"), DefaultValue("")]
        public string Ref
        {
            get
            {
                return _Ref;
            }
            set
            {
                if (_Ref == value)
                {
                    return;
                }
                _Ref = value;
                RaisePropertyChanged("Ref");
            }
        }
        #endregion

        #region 属性 IsRange
        private bool _IsRange = false;
        [XmlAttribute("IsRange"), DefaultValue(false)]
        public bool IsRange
        {
            get
            {
                return _IsRange;
            }
            set
            {
                if (_IsRange == value)
                {
                    return;
                }
                _IsRange = value;
                RaisePropertyChanged("IsRange");
            }
        }
        #endregion

        #region 属性 IsVisible
        private bool _IsVisible = true;
        [XmlAttribute("IsVisible"), DefaultValue(true)]
        public bool IsVisible
        {
            get
            {
                return _IsVisible;
            }
            set
            {
                if (_IsVisible == value)
                {
                    return;
                }
                _IsVisible = value;
                RaisePropertyChanged("IsVisible");
            }
        }
        #endregion
    }

    public enum LockMode
    {
        /// <summary>
        /// 没有锁
        /// </summary>
        None,
        /// <summary>
        /// 当前对象锁
        /// </summary>
        Current,
        /// <summary>
        /// 全局锁，锁全局对象
        /// </summary>
        Global,
    }

    [XmlRoot("Flow")]
    public partial class MasterFlowConfig : FlowItemBase
    {
        #region 属性 Parameters
        private CollectionBase<FlowInputParameter> _Parameters = null;
        [XmlArray("Parameters"), XmlArrayItem("Item", typeof(FlowInputParameter))]
        public CollectionBase<FlowInputParameter> Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new CollectionBase<FlowInputParameter>();
                }
                return _Parameters;
            }
            set
            {
                _Parameters = value;
                RaisePropertyChanged("Parameters");
            }
        }
        #endregion

        #region 属性 BeforeActions
        private CollectionBase<FunctionFlowAction> _BeforeActions = null;
        [XmlElement("BeforeAction", typeof(FunctionFlowAction))]
        public CollectionBase<FunctionFlowAction> BeforeActions
        {
            get
            {
                if (_BeforeActions == null)
                {
                    _BeforeActions = new CollectionBase<FunctionFlowAction>();
                }
                return _BeforeActions;
            }
            set
            {
                _BeforeActions = value;
                RaisePropertyChanged("BeforeActions");
            }
        }
        #endregion

        #region 属性 AfterActions
        private CollectionBase<FunctionFlowAction> _AfterActions = null;
        [XmlElement("AfterAction", typeof(FunctionFlowAction))]
        public CollectionBase<FunctionFlowAction> AfterActions
        {
            get
            {
                if (_AfterActions == null)
                {
                    _AfterActions = new CollectionBase<FunctionFlowAction>();
                }
                return _AfterActions;
            }
            set
            {
                _AfterActions = value;
                RaisePropertyChanged("AfterActions");
            }
        }
        #endregion

        public IEnumerable<FunctionFlowAction> GetBeforeActions()
        {
            foreach (var item in BeforeActions)
            {
                yield return item;
            }
        }
        public IEnumerable<FunctionFlowAction> GetAfterActions()
        {
            foreach (var item in AfterActions)
            {
                yield return item;
            }
        }

        public IEnumerable<FlowInputParameter> GetParameters()
        {
            foreach (var p in Parameters)
            {
                yield return p;
            }
        }
    }

    [XmlRoot("Flow")]
    public partial class FunctionFlowConfig : FlowItemBase
    {
        #region 属性 LockMode
        private LockMode _LockMode = LockMode.None;
        [XmlAttribute("LockMode"), DefaultValue(typeof(LockMode), "None")]
        public LockMode LockMode
        {
            get
            {
                return _LockMode;
            }
            set
            {
                _LockMode = value;
                RaisePropertyChanged(() => LockMode);
            }
        }
        #endregion

        #region 属性 LockID
        private string _LockID = string.Empty;
        [XmlAttribute("LockID"), DefaultValue("")]
        public string LockID
        {
            get
            {
                return _LockID;
            }
            set
            {
                if (_LockID == value)
                {
                    return;
                }
                _LockID = value;
                RaisePropertyChanged("LockID");
            }
        }
        #endregion

        #region 属性 ErrorMode
        private FlowErrorMode _ErrorMode = FlowErrorMode.Break;
        [XmlAttribute("ErrorMode"), DefaultValue(typeof(FlowErrorMode), "Break")]
        public FlowErrorMode ErrorMode
        {
            get
            {
                return _ErrorMode;
            }
            set
            {
                _ErrorMode = value;
                RaisePropertyChanged("ErrorMode");
            }
        }
        #endregion

        #region 属性 RetryCount
        private int _RetryCount = 0;
        [XmlAttribute("RetryCount"), DefaultValue(0)]
        public int RetryCount
        {
            get
            {
                return _RetryCount;
            }
            set
            {
                if (_RetryCount == value)
                {
                    return;
                }
                _RetryCount = value;
                RaisePropertyChanged("RetryCount");
            }
        }
        #endregion

        #region 属性 MasterID
        private string _MasterID = string.Empty;
        [XmlAttribute("MasterID"), DefaultValue("")]
        public string MasterID
        {
            get
            {
                return _MasterID;
            }
            set
            {
                if (_MasterID == value)
                {
                    return;
                }
                _MasterID = value;
                RaisePropertyChanged("MasterID");
            }
        }
        #endregion

        #region 属性 Parameters
        private CollectionBase<FlowInputParameter> _Parameters = null;
        [XmlArray("Parameters"), XmlArrayItem("Item", typeof(FlowInputParameter))]
        public CollectionBase<FlowInputParameter> Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new CollectionBase<FlowInputParameter>();
                    _Parameters.CollectionChanged += Parameters_CollectionChanged;
                }
                return _Parameters;
            }
            set
            {
                if (_Parameters != null)
                {
                    _Parameters.CollectionChanged -= Parameters_CollectionChanged;
                    OnParametersRemoved(_Parameters);
                }
                _Parameters = value;
                RaisePropertyChanged("Parameters");
                if (_Parameters != null)
                {
                    _Parameters.CollectionChanged += Parameters_CollectionChanged;
                    OnParametersAdded(_Parameters);
                }
            }
        }

        private void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnParametersAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnParametersRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnParametersAdded(IList newItems)
        {
            foreach (FlowInputParameter item in newItems)
            {
                item.ConfigRoot = this;
            }
        }

        protected virtual void OnParametersRemoved(IList oldItems)
        {
            foreach (FlowInputParameter item in oldItems)
            {
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 Actions
        private CollectionBase<FlowActionBase> _Actions = null;
        [XmlElement("Action", typeof(FunctionFlowAction))]
        [XmlElement("Setter", typeof(SetterAction))]
        [XmlElement("Flow", typeof(SubFlowAction))]
        [XmlElement("Switch", typeof(SwitchAction))]
        [XmlElement("ForEach", typeof(ForEachAction))]
        [XmlElement("While", typeof(WhileAction))]
        public CollectionBase<FlowActionBase> Actions
        {
            get
            {
                if (_Actions == null)
                {
                    _Actions = new CollectionBase<FlowActionBase>();
                    _Actions.CollectionChanged += Actions_CollectionChanged;
                }
                return _Actions;
            }
            set
            {
                if (_Actions != null)
                {
                    _Actions.CollectionChanged -= Actions_CollectionChanged;
                    OnActionsRemoved(_Actions);
                }
                _Actions = value;
                RaisePropertyChanged("Actions");
                if (_Actions != null)
                {
                    _Actions.CollectionChanged += Actions_CollectionChanged;
                    OnActionsAdded(_Actions);
                }
            }
        }

        private void Actions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnActionsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnActionsRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnActionsAdded(IList newItems)
        {
            foreach (FlowActionBase item in newItems)
            {
                item.ConfigRoot = this;
            }
        }

        protected virtual void OnActionsRemoved(IList oldItems)
        {
            foreach (FlowActionBase item in oldItems)
            {
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 Results
        private CollectionBase<FlowResultParameter> _Results = null;
        [XmlArray("Results"), XmlArrayItem("Item", typeof(FlowResultParameter))]
        public CollectionBase<FlowResultParameter> Results
        {
            get
            {
                if (_Results == null)
                {
                    _Results = new CollectionBase<FlowResultParameter>();
                    _Results.CollectionChanged += Results_CollectionChanged;
                }
                return _Results;
            }
            set
            {
                if (_Results != null)
                {
                    _Results.CollectionChanged -= Results_CollectionChanged;
                    OnResultsRemoved(_Results);
                }
                _Results = value;
                RaisePropertyChanged("Results");
                if (_Results != null)
                {
                    _Results.CollectionChanged += Results_CollectionChanged;
                    OnResultsAdded(_Results);
                }
            }
        }

        private void Results_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnResultsAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnResultsRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnResultsAdded(IList newItems)
        {
            foreach (FlowResultParameter item in newItems)
            {
                item.ConfigRoot = this;
            }
        }

        protected virtual void OnResultsRemoved(IList oldItems)
        {
            foreach (FlowResultParameter item in oldItems)
            {
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 ResultType
        private string _ResultType = string.Empty;
        [XmlAttribute("ResultType"), DefaultValue("")]
        public string ResultType
        {
            get
            {
                return _ResultType;
            }
            set
            {
                if (_ResultType == value)
                {
                    return;
                }
                _ResultType = value;
                RaisePropertyChanged("ResultType");
            }
        }
        #endregion

        public string GetNextID()
        {
            if (Actions.Count > 0)
            {
                var id = 0;
                foreach (var action in Actions)
                {
                    if (action.ID.IsNotEmpty() && action.ID.StartsWith("A"))
                    {
                        var num = action.ID.Substring(1).ToInt32();
                        if (num > id)
                        {
                            id = num;
                        }
                    }
                }
                return $"A{(id + 1).ToString("d2")}";
            }

            return $"A01";
        }

        public virtual IEnumerable<FlowInputParameter> GetParameters()
        {
            var master = GetMaster();
            if (master != null)
            {
                foreach (var p in master.GetParameters())
                {
                    yield return p;
                }
            }

            foreach (var p in Parameters)
            {
                yield return p;
            }
        }

        public virtual IEnumerable<FlowActionBase> GetActions()
        {
            var master = GetMaster();
            if (master != null)
            {
                foreach (var action in master.GetBeforeActions())
                {
                    yield return action;
                }
            }

            foreach (var action in Actions)
            {
                yield return action;
            }

            if (master != null)
            {
                foreach (var action in master.GetAfterActions())
                {
                    yield return action;
                }
            }
        }

        public MasterFlowConfig GetMaster()
        {
            if (MasterID.IsNotNullOrEmpty())
            {
                if (ServiceIndex.Current.TryGetMasterItem(MasterID, out var item))
                {
                    return item.FlowConfig;
                }
            }
            return null;
        }
    }

    public partial class FlowItemBase : ConfigFile
    {
        #region 属性 ID
        private string _ID = string.Empty;
        [XmlAttribute("ID")]
        public string ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
                RaisePropertyChanged("ID");
            }
        }
        #endregion

        #region 属性 Name
        private string _Name = string.Empty;
        /// <summary>
        /// 算法名称
        /// </summary>
        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 Description
        private string _Description = string.Empty;
        [XmlElement("Description")]
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
                RaisePropertyChanged("Description");
            }
        }
        #endregion
    }

    [Flags]
    public enum FlowErrorMode
    {
        /// <summary>
        /// 重试
        /// </summary>
        Retry = 1,
        /// <summary>
        /// 继续
        /// </summary>
        Continue = 2,
        /// <summary>
        /// 中断
        /// </summary>
        Break = 4,
        /// <summary>
        /// 重试后仍然出错继续
        /// </summary>
        RetryContinue = 3,
        /// <summary>
        /// 重试后仍然出错退出
        /// </summary>
        RetryBreak = 5
    }

    public partial class FunctionAssert : ConfigItemBase
    {
        #region 属性 Condition
        private string _Condition = string.Empty;
        [XmlAttribute("Condition"), DefaultValue("")]
        public string Condition
        {
            get
            {
                return _Condition;
            }
            set
            {
                if (_Condition == value)
                {
                    return;
                }
                _Condition = value;
                RaisePropertyChanged("Condition");
            }
        }
        #endregion

        #region 属性 Error
        private string _Error = string.Empty;
        [XmlAttribute("Error"), DefaultValue("")]
        public string Error
        {
            get
            {
                return _Error;
            }
            set
            {
                if (_Error == value)
                {
                    return;
                }
                _Error = value;
                RaisePropertyChanged("Error");
            }
        }
        #endregion
    }

    public class FunctionActionWaitItem : ConfigItemBase
    {
        #region 属性 Condition
        private string _Condition = string.Empty;
        [XmlElement("Condition"), DefaultValue("")]
        public string Condition
        {
            get
            {
                return _Condition;
            }
            set
            {
                _Condition = value;
                RaisePropertyChanged("Condition");
            }
        }
        #endregion

        #region 属性 CheckMS
        private int _CheckMS = 0;
        [XmlAttribute("CheckMS"), DefaultValue(0)]
        public int CheckMS
        {
            get
            {
                return _CheckMS;
            }
            set
            {
                if (_CheckMS == value)
                {
                    return;
                }
                _CheckMS = value;
                RaisePropertyChanged("CheckMS");
            }
        }
        #endregion

        #region 属性 MaxCount
        private int _MaxCount = 0;
        [XmlAttribute("MaxCount"), DefaultValue(0)]
        public int MaxCount
        {
            get
            {
                return _MaxCount;
            }
            set
            {
                if (_MaxCount == value)
                {
                    return;
                }
                _MaxCount = value;
                RaisePropertyChanged("MaxCount");
            }
        }
        #endregion
    }

    public partial class FlowActionBase : ConfigItemBase
    {
        #region 属性 ID
        private string _ID = string.Empty;
        [XmlAttribute("ID"), DefaultValue("")]
        public string ID
        {
            get
            {
                return _ID;
            }
            set
            {
                if (_ID == value)
                {
                    return;
                }
                _ID = value;
                RaisePropertyChanged("ID");
            }
        }
        #endregion

        #region 属性 Name
        private string _Name = string.Empty;
        /// <summary>
        /// 算法名称
        /// </summary>
        [XmlAttribute("Name")]
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                RaisePropertyChanged("Name");
            }
        }
        #endregion

        #region 属性 IgnoreErrors
        private string _IgnoreErrors = string.Empty;
        [XmlAttribute("IgnoreErrors"), DefaultValue("")]
        public string IgnoreErrors
        {
            get
            {
                return _IgnoreErrors;
            }
            set
            {
                if (_IgnoreErrors == value)
                {
                    return;
                }
                _IgnoreErrors = value;
                RaisePropertyChanged("IgnoreErrors");
            }
        }
        #endregion

        #region 属性 Wait
        private FunctionActionWaitItem _Wait = null;
        [XmlElement("Wait")]
        public FunctionActionWaitItem Wait
        {
            get
            {
                return _Wait;
            }
            set
            {
                _Wait = value;
                RaisePropertyChanged("Wait");
            }
        }
        #endregion

        #region 属性 Condition
        private string _Condition = string.Empty;
        [XmlElement("Condition"), DefaultValue("")]
        public string Condition
        {
            get
            {
                return _Condition;
            }
            set
            {
                _Condition = value;
                RaisePropertyChanged("Condition");
            }
        }
        #endregion

        #region 属性 Asserts
        private CollectionBase<FunctionAssert> _Asserts = null;
        [XmlElement("Assert")]
        public CollectionBase<FunctionAssert> Asserts
        {
            get
            {
                if (_Asserts == null)
                {
                    _Asserts = new CollectionBase<FunctionAssert>();
                }
                return _Asserts;
            }
            set
            {
                _Asserts = value;
                RaisePropertyChanged("Asserts");
            }
        }
        #endregion

        #region 属性 Parameters
        private CollectionBase<ActionInputParameter> _Parameters = null;
        [XmlElement("Input", typeof(ActionInputParameter))]
        public CollectionBase<ActionInputParameter> Parameters
        {
            get
            {
                if (_Parameters == null)
                {
                    _Parameters = new CollectionBase<ActionInputParameter>();
                    _Parameters.CollectionChanged += Parameters_CollectionChanged;
                }
                return _Parameters;
            }
            set
            {
                if (_Parameters != null)
                {
                    _Parameters.CollectionChanged -= Parameters_CollectionChanged;
                    OnParametersRemoved(_Parameters);
                }
                _Parameters = value;
                RaisePropertyChanged("Parameters");
                if (_Parameters != null)
                {
                    _Parameters.CollectionChanged += Parameters_CollectionChanged;
                    OnParametersAdded(_Parameters);
                }
            }
        }

        private void Parameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnParametersAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnParametersRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnParametersAdded(IList newItems)
        {
            foreach (ActionInputParameter item in newItems)
            {
                item.FlowAction = this;
                item.ConfigRoot = this.ConfigRoot;
            }
        }

        protected virtual void OnParametersRemoved(IList oldItems)
        {
            foreach (ActionInputParameter item in oldItems)
            {
                item.FlowAction = null;
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 ResultParameters
        private CollectionBase<ActionOutputParameter> _ResultParameters = null;
        [XmlElement("Output", typeof(ActionOutputParameter))]
        public CollectionBase<ActionOutputParameter> ResultParameters
        {
            get
            {
                if (_ResultParameters == null)
                {
                    _ResultParameters = new CollectionBase<ActionOutputParameter>();
                    _ResultParameters.CollectionChanged += ResultParameters_CollectionChanged;
                }
                return _ResultParameters;
            }
            set
            {
                if (_ResultParameters != null)
                {
                    _ResultParameters.CollectionChanged -= ResultParameters_CollectionChanged;
                    OnResultParametersRemoved(_ResultParameters);
                }
                _ResultParameters = value;
                RaisePropertyChanged("ResultParameters");
                if (_ResultParameters != null)
                {
                    _ResultParameters.CollectionChanged += ResultParameters_CollectionChanged;
                    OnResultParametersAdded(_ResultParameters);
                }
            }
        }

        private void ResultParameters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    OnResultParametersAdded(e.NewItems);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    OnResultParametersRemoved(e.OldItems);
                    break;
                default:
                    break;
            }
        }
        protected virtual void OnResultParametersAdded(IList newItems)
        {
            foreach (ActionOutputParameter item in newItems)
            {
                item.FlowAction = this;
                item.ConfigRoot = this.ConfigRoot;
            }
        }

        protected virtual void OnResultParametersRemoved(IList oldItems)
        {
            foreach (ActionOutputParameter item in oldItems)
            {
                item.FlowAction = null;
                item.ConfigRoot = null;
            }
        }
        #endregion

        #region 属性 ResultAsserts
        private CollectionBase<FunctionAssert> _ResultAsserts = null;
        [XmlElement("ResultAssert")]
        public CollectionBase<FunctionAssert> ResultAsserts
        {
            get
            {
                if (_ResultAsserts == null)
                {
                    _ResultAsserts = new CollectionBase<FunctionAssert>();
                }
                return _ResultAsserts;
            }
            set
            {
                _ResultAsserts = value;
                RaisePropertyChanged("ResultAsserts");
            }
        }
        #endregion

        protected override void OnConfigRootChanged()
        {
            base.OnConfigRootChanged();
            Parameters.ForEach(p => p.ConfigRoot = this.ConfigRoot);
            ResultParameters.ForEach(p => p.ConfigRoot = this.ConfigRoot);
        }

        public virtual bool TryRun(FunctionFlowConfig flow, Dictionary<string, object> flowArgs, out IFunctionResult actionResult)
        {
            actionResult = null;
            return false;
        }

        protected Dictionary<string, object> CreateFunctionArgs(Dictionary<string, object> flowArgs, Interpreter exp, IFunctionItem algItem)
        {
            #region 准备参数
            var args = new Dictionary<string, object>();
            if (flowArgs.TryGetValue(FlowContext.RootArgsName, out var rootArgs))
            {
                args.Add(FlowContext.RootArgsName, rootArgs);
            }
            if (flowArgs.TryGetValue(FlowContext.RootContextArgName, out var rootContext))
            {
                args.Add(FlowContext.RootContextArgName, rootContext);
            }
            args.Add(FlowContext.ParentArgsName, flowArgs);

            foreach (var p in algItem.GetParameters())
            {
                try
                {
                    object pvalue = null;
                    var actionParameter = Parameters.FirstOrDefault(f => f.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                    //参数使用优先级
                    if (actionParameter != null)
                    {
                        if (actionParameter.IsInherit)
                        {
                            object defaultValue = p.DefaultValue;
                            pvalue = defaultValue;
                        }
                        else if (actionParameter.Expression.IsNullOrEmpty())
                        {
                            if (!flowArgs.TryGetValue(p.Name, out pvalue))
                            {
                                pvalue = actionParameter.DefaultValue;
                            }
                        }
                        else
                        {
                            if (!flowArgs.TryGetValue(actionParameter.Expression, out pvalue))
                            {
                                pvalue = exp.Eval(actionParameter.Expression);
                            }
                        }
                        pvalue = actionParameter.GetValue(pvalue);
                    }
                    else
                    {
                        pvalue = GetInputValue(flowArgs, p);
                    }
                    args[p.Name] = pvalue;
                }
                catch (Exception ex)
                {
                    this.Log($"函数[{Name}] 设置参数值出错[{p.Name}] ：{ex}");
                    throw;
                }
            }
            #endregion
            return args;
        }

        public Dictionary<string, object> CreateSubFlowArgs(Dictionary<string, object> flowArgs, Interpreter exp)
        {
            #region 准备参数
            var args = new Dictionary<string, object>();
            if (flowArgs.TryGetValue(FlowContext.RootArgsName, out var rootArgs))
            {
                args.Add(FlowContext.RootArgsName, rootArgs);
            }
            if (flowArgs.TryGetValue(FlowContext.RootContextArgName, out var rootContext))
            {
                args.Add(FlowContext.RootContextArgName, rootContext);
            }
            args.Add(FlowContext.ParentArgsName, flowArgs);

            foreach (var p in Parameters)
            {
                object pvalue = null;
                if (p.Expression.IsNullOrEmpty())
                {
                    if (flowArgs.TryGetValue(p.Name, out object fvalue))
                    {
                        pvalue = fvalue;
                    }
                    else if (p.DefaultValue.IsNotEmpty())
                    {
                        pvalue = p.DefaultValue;
                    }
                    else
                    {
                        throw new Exception($"{p.Name}没有配置参数");
                    }
                }
                else
                {
                    if (!flowArgs.TryGetValue(p.Expression, out pvalue))
                    {
                        pvalue = exp.Eval(p.Expression);
                    }
                }
                args[p.Name] = p.GetValue(pvalue);
            }
            #endregion
            return args;
        }

        private static object GetInputValue(Dictionary<string, object> flowArgs, ParameterItem p)
        {
            if (flowArgs.TryGetValue(p.Name, out object pv))
            {
                return p.GetValue(pv);
            }
            else
            {
                return p.GetValue(p.DefaultValue);
            }
        }

        protected void CheckAssert(FunctionFlowConfig flow, Interpreter exp)
        {
            if (Asserts.Count > 0)
            {
                foreach (var assert in Asserts)
                {
                    if (assert.Condition.IsNullOrEmpty())
                    {
                        continue;
                    }

                    try
                    {
                        var passed = exp.Eval(assert.Condition).ToBoolean();
                        if (passed)
                        {
                            continue;
                        }
                        this.Log($"流程[{flow.Name}]节点[{Name}] Assert异常：{assert.Error}");
                        throw new CodeException(FlowErrors.AssertError, assert.Error);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        protected void CheckWait(Dictionary<string, object> flowArgs)
        {
            #region wait
            if (Wait != null && Wait.Condition.IsNotEmpty())
            {
                var maxCount = Wait.MaxCount > 0 ? Wait.MaxCount : 0;
                var checkMS = Wait.CheckMS > 0 ? Wait.CheckMS : 100;
                var checkCount = 0;
                while (!Wait.Condition.Eval(flowArgs).ToBoolean())
                {
                    checkCount++;
                    if (maxCount > 0 && checkCount >= maxCount)
                    {
                        this.Log($"等待条件失败，重试次数[{checkCount}]");
                        break;
                    }
                    Thread.Sleep(checkMS);
                }
            }
            #endregion
        }
    }

    public partial class FunctionFlowAction : FlowActionBase
    {
        #region 属性 FunctionName
        private string _FunctionName = string.Empty;
        [XmlAttribute("FunctionName")]
        public string FunctionName
        {
            get
            {
                return _FunctionName;
            }
            set
            {
                _FunctionName = value;
                RaisePropertyChanged("FunctionName");
                RaisePropertyChanged("FunctionItem");
            }
        }
        #endregion

        #region 属性 FunctionItem
        [XmlIgnore]
        public FunctionItemBase FunctionItem
        {
            get
            {
                if (FunctionName.IsNullOrEmpty())
                {
                    return null;
                }
                var item = FunctionConfig.Current.GetItem(FunctionName);
                return item;
            }
        }
        #endregion

        public override bool TryRun(FunctionFlowConfig flow, Dictionary<string, object> flowArgs, out IFunctionResult actionResult)
        {
            var exp = ExpressionHelper.GetInterpreter(flowArgs);
            actionResult = null;

            CheckWait(flowArgs);
            if (Condition.IsNotEmpty())
            {
                if (!exp.Eval(Condition).ToBoolean())
                {
                    this.Log($"{Name} 不满足执行条件，执行跳过");
                    return false;
                }
            }

            CheckAssert(flow, exp);

            if (FunctionName.IsNullOrEmpty())
            {
                throw FlowErrors.CreateError_FlowConfigError(flow.Name, $"节点[{Name}]算法没有设置");
            }

            var functionName = FunctionName;
            var algItem = MethodServiceFactory.GetItem(functionName);
            if (algItem == null)
            {
                throw FlowErrors.CreateError_FuncNotFound(functionName);
            }

            Dictionary<string, object> args = CreateFunctionArgs(flowArgs, exp, algItem);
            try
            {
                actionResult = MethodServiceFactory.Run(functionName, args);
            }
            catch (Exception ex)
            {
                ex.WriteErrorLog();
                actionResult = new FunctionResult { ErrorCode = FlowErrors.MethodInnerError, ErrorMessage = ex.Message };
            }
            return true;
        }
    }

    public partial class ActionInputParameter : ParameterBase
    {
        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute("DefaultValue"), DefaultValue("")]
        public string DefaultValue
        {
            get
            {
                return _DefaultValue;
            }
            set
            {
                _DefaultValue = value;
                RaisePropertyChanged("DefaultValue");
            }
        }
        #endregion

        #region 属性 Expression
        private string _Expression = string.Empty;
        [XmlAttribute("Expression")]
        public string Expression
        {
            get
            {
                return _Expression;
            }
            set
            {
                _Expression = value;
                RaisePropertyChanged("Expression");
            }
        }
        #endregion 

        #region 属性 IsInherit
        private bool _IsInherit = false;
        [XmlAttribute("IsInherit"), DefaultValue(false)]
        public bool IsInherit
        {
            get
            {
                return _IsInherit;
            }
            set
            {
                if (_IsInherit == value)
                {
                    return;
                }
                _IsInherit = value;
                RaisePropertyChanged("IsInherit");
            }
        }
        #endregion

        #region 属性 FlowAction
        private FlowActionBase _FlowAction = null;
        [XmlIgnore, JsonIgnore]
        public FlowActionBase FlowAction
        {
            get
            {
                return _FlowAction;
            }
            internal set
            {
                _FlowAction = value;
                RaisePropertyChanged("FlowAction");
            }
        }
        #endregion
    }

    public partial class FlowResultParameter : ParameterBase
    {
        #region 属性 Expression
        private string _Expression = string.Empty;
        [XmlAttribute("Expression")]
        public string Expression
        {
            get
            {
                return _Expression;
            }
            set
            {
                _Expression = value;
                RaisePropertyChanged("Expression");
            }
        }
        #endregion
    }

    public partial class ActionOutputParameter : ParameterBase
    {
        #region 属性 ResetInputName
        private string _ResetInputName = string.Empty;
        [XmlAttribute("ResetInputName"), DefaultValue("")]
        public string ResetInputName
        {
            get
            {
                return _ResetInputName;
            }
            set
            {
                _ResetInputName = value;
                RaisePropertyChanged("ResetInputName");
            }
        }
        #endregion

        #region 属性 ResultName
        private string _ResultName = string.Empty;
        [XmlAttribute("ResultName"), DefaultValue("")]
        public string ResultName
        {
            get
            {
                return _ResultName;
            }
            set
            {
                if (_ResultName == value)
                {
                    return;
                }
                _ResultName = value;
                RaisePropertyChanged("ResultName");
            }
        }
        #endregion

        #region 属性 DefaultValue
        private string _DefaultValue = string.Empty;
        /// <summary>
        /// 默认值
        /// </summary>
        [XmlAttribute("DefaultValue"), DefaultValue("")]
        public string DefaultValue
        {
            get
            {
                return _DefaultValue;
            }
            set
            {
                _DefaultValue = value;
                RaisePropertyChanged("DefaultValue");
            }
        }
        #endregion

        #region 属性 FlowAction
        private FlowActionBase _FlowAction = null;
        [XmlIgnore]
        public FlowActionBase FlowAction
        {
            get
            {
                return _FlowAction;
            }
            internal set
            {
                _FlowAction = value;
                RaisePropertyChanged("FlowAction");
            }
        }
        #endregion
    }

    public partial class FlowParameter : ParameterBase
    {
        #region 属性 Value
        private string _Value = string.Empty;
        [XmlAttribute("Value")]
        public string Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
                RaisePropertyChanged("Value");
            }
        }
        #endregion
    }

    /// <summary>
    /// 算法统一返回的结果
    /// </summary>
    public class FunctionResult : IFunctionResult
    {
        //针对某个具体算法，可以定义多个错误码
        public string ErrorCode { get; set; } = "0";

        public string ErrorMessage { get; set; } = "";

        //返回值，没有则保留默认值
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public static bool IsOKCode(string errorCode)
        {
            return errorCode.IsNullOrEmpty() || errorCode == ErrorCodes.Ok;
        }

        public FunctionResult()
        {

        }

        public FunctionResult(Dictionary<string, object> data)
        {
            if (data != null)
            {
                data.ForEach(d => Data[d.Key] = d.Value);
            }
        }

        public bool IsOk { get { return IsOKCode(ErrorCode) && ErrorMessage.IsNullOrEmpty(); } }

        public bool TryGetValue(string name, out object value)
        {
            return Data.TryGetValue(name, out value);
        }

        public void SetValue(string name, object value)
        {
            Data[name] = value;
        }

        public FunctionResult SetData(string name, object value)
        {
            SetValue(name, value);
            return this;
        }
    }

    /// <summary>
    /// 异常
    /// </summary>
    public class MethodNotFoundException : Exception
    {
        public string MethodName { get; set; }
    }

}
