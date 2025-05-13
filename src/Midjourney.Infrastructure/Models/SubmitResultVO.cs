

using Midjourney.Infrastructure.Data;
using Newtonsoft.Json;
using System.Reflection.Metadata;

namespace Midjourney.Infrastructure.Models
{
    /// <summary>
    /// 提交结果类。
    /// </summary>
    public class SubmitResultVO
    {
        /// <summary>
        /// 状态码。
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary>
        /// 描述信息。
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 任务ID。
        /// </summary>
        [JsonProperty("result")]
        public dynamic Result { get; set; }

        /// <summary>
        /// 扩展字段。
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public SubmitResultVO()
        {
        }

        private SubmitResultVO(int code, string description, dynamic result = null)
        {
            Code = code;
            Description = description;
            Result = result;
        }

        /// <summary>
        /// 设置扩展字段。
        /// </summary>
        public SubmitResultVO SetProperty(string name, object value)
        {
            Properties[name] = value;

            // 同时赋值将 Discord 实例 ID  = 频道 ID
            // if (name == Constants.TASK_PROPERTY_DISCORD_INSTANCE_ID)
            // {
            //     Properties[Constants.TASK_PROPERTY_DISCORD_CHANNEL_ID] = value;
            // }

            return this;
        }

        /// <summary>
        /// 移除扩展字段。
        /// </summary>
        public SubmitResultVO RemoveProperty(string name)
        {
            Properties.Remove(name);
            return this;
        }

        /// <summary>
        /// 获取扩展字段。
        /// </summary>
        public object GetProperty(string name) => Properties.TryGetValue(name, out var value) ? value : null;

        /// <summary>
        /// 获取扩展字段的泛型版本。
        /// </summary>
        public T GetPropertyGeneric<T>(string name) => (T)GetProperty(name);

        /// <summary>
        /// 返回带自定义状态码、描述和任务ID的提交结果。
        /// </summary>
        public static SubmitResultVO Of(int code, string description, string result) => new SubmitResultVO(code, description, result);

        /// <summary>
        /// 返回带自定义状态码、描述和任务ID的提交结果。
        /// </summary>
        public static SubmitResultVO Of(int code, string description, List<string> result) => new SubmitResultVO(code, description, result);

        /// <summary>
        /// 返回失败的提交结果。
        /// </summary>
        public static SubmitResultVO Fail(int code, string description) => new SubmitResultVO(code, description);
    }
}