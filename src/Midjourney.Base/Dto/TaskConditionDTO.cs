
using Swashbuckle.AspNetCore.Annotations;

namespace Midjourney.Base.Dto
{
    /// <summary>
    /// 任务查询参数。
    /// </summary>
    [SwaggerSchema("任务查询参数")]
    public class TaskConditionDTO
    {
        /// <summary>
        /// 任务ID列表。
        /// </summary>
        [SwaggerSchema("任务ID列表")]
        public List<string> Ids { get; set; }
    }
}