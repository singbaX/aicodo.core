// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。

#if Newton
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif
using System.ComponentModel;

namespace AiCodo
{
    public class ServiceResult : IServiceResult
    {
#if Newton
        [JsonProperty("data")]
#else
        [JsonPropertyName("data")]
#endif
        [DefaultValue(null)]
        public object Data { get; set; }

#if Newton
        [JsonProperty("error")]
#else
        [JsonPropertyName("error")]
#endif
        public string Error { get; set; } = "";

#if Newton
        [JsonProperty("errorCode")]
#else
        [JsonPropertyName("errorCode")]
#endif
        public string ErrorCode { get; set; } = "";

        [JsonIgnore]
        public bool IsError { get { return Error.IsNotEmpty() || ErrorCode.IsNotEmpty(); } }

        [JsonIgnore]
        public bool IsOk { get { return Error.IsNullOrEmpty() && ErrorCode.IsNullOrEmpty(); } }

        public string GetError()
        {
            if (ErrorCode.IsNullOrEmpty())
            {
                if (Error.IsNullOrEmpty())
                {
                    return "";
                }
                return Error;
            }
            return $"[{ErrorCode}]:{Error}";
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
#if Newton
        [JsonProperty("data")]
#else
        [JsonPropertyName("data")]
#endif
        [DefaultValue(null)]
        public new T Data { get; set; }
    }

    public interface IServiceResult
    {
        object Data { get; set; }
        string Error { get; set; }
        string ErrorCode { get; set; }
        bool IsError { get; }
        bool IsOk { get; }

        string GetError();
    }
}
