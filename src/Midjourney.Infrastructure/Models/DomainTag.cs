

using FreeSql.DataAnnotations;
using Midjourney.Infrastructure.Data;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace Midjourney.Infrastructure.Models
{
    /// <summary>
    /// 账号垂直领域
    /// </summary>
    [BsonCollection("domain")]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    [Serializable]
    public class DomainTag : DomainObject
    {
        public DomainTag()
        {
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        [JsonMap]
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 权重
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTimeFormat => CreateTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>
        /// 更新时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public string UpdateTimeFormat => UpdateTime.ToString("yyyy-MM-dd HH:mm");
    }
}