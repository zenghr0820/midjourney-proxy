
using Newtonsoft.Json.Linq;

namespace Midjourney.Base.StandardTable
{
    public class StandardSearch : StandardSearch<JObject>
    {
        public override JObject PredicateObject { get; set; }
    }

    public class StandardSearch<T> where T : class, new()
    {
        public virtual T PredicateObject { get; set; } = new T();
    }
}