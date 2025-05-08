using System.Linq.Expressions;

namespace Midjourney.Infrastructure.Data
{
    public class FreeSqlRepository<T> : IDataHelper<T> where T : class, IBaseId
    {
        private readonly IFreeSql _freeSql;

        public FreeSqlRepository()
        {
            _freeSql = FreeSqlHelper.FreeSql;
        }

        public void Init()
        {

        }

        public void Add(T entity)
        {
            _freeSql.Insert(entity).ExecuteAffrows();
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _freeSql.Insert(entities).ExecuteAffrows();
        }

        public void Delete(T entity)
        {
            _freeSql.Delete<T>(entity.Id).ExecuteAffrows();
        }

        public int Delete(Expression<Func<T, bool>> predicate)
        {
            return _freeSql.Delete<T>().Where(predicate).ExecuteAffrows();
        }

        public void Update(T entity)
        {
            _freeSql.Update<T>().SetSource(entity).ExecuteAffrows();
        }

        /// <summary>
        /// 部分更新
        /// </summary>
        /// <param name="fields">BotToken,IsBlend,Properties</param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Update(string fields, T item)
        {
            // 获取现有文档
            var model = _freeSql.Select<T>().Where(c => c.Id == item.Id).First();
            if (model == null)
                return false;

            // 将更新对象的字段值复制到现有文档
            var fieldArray = fields.Split(',');
            foreach (var field in fieldArray)
            {
                var prop = typeof(T).GetProperty(field.Trim());
                if (prop != null)
                {
                    var newValue = prop.GetValue(item);
                    prop.SetValue(model, newValue);
                }
            }

            // 更新文档
           _freeSql.Update<T>().SetSource(model).ExecuteAffrows();

            return true;
        }

        public List<T> GetAll()
        {
            return _freeSql.Select<T>().ToList();
        }

        /// <summary>
        /// 获取所有实体的 ID 列表。
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllIds()
        {
            return _freeSql.Queryable<T>().Select(x => x.Id).ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            return _freeSql.Select<T>().Where(predicate).ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc = true)
        {
            var query = _freeSql.Select<T>().Where(filter);
            if (orderByAsc)
            {
                query = query.OrderBy(orderBy);
            }
            else
            {
                query = query.OrderByDescending(orderBy);
            }
            return query.ToList();
        }

        public T Single(Expression<Func<T, bool>> predicate)
        {
            return _freeSql.Select<T>().Where(predicate).First();
        }

        public T Single(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc = true)
        {
            var query = _freeSql.Select<T>().Where(filter);
            if (orderByAsc)
            {
                query = query.OrderBy(orderBy);
            }
            else
            {
                query = query.OrderByDescending(orderBy);
            }
            return query.First();
        }

        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return _freeSql.Select<T>().Where(predicate).Any();
        }

        public long Count(Expression<Func<T, bool>> predicate)
        {
            return _freeSql.Select<T>().Where(predicate).Count();
        }

        public long Count()
        {
            return _freeSql.Select<T>().Count();
        }

        public void Save(T entity)
        {
            if (entity != null && !string.IsNullOrEmpty(entity.Id))
            {
                _freeSql.InsertOrUpdate<T>().SetSource(entity).ExecuteAffrows();
            }
        }

        public void Delete(string id)
        {
            _freeSql.Delete<T>().Where(c => c.Id == id).ExecuteAffrows();
        }

        public T Get(string id)
        {
            return _freeSql.Select<T>().Where(c => c.Id == id).First();
        }

        public List<T> List()
        {
            return _freeSql.Select<T>().ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc, int limit)
        {
            var query = _freeSql.Select<T>().Where(filter);
            if (orderByAsc)
            {
                query = query.OrderBy(orderBy).Limit(limit);
            }
            else
            {
                query = query.OrderByDescending(orderBy).Limit(limit);
            }
            return query.ToList();
        }

        public IQuery<T> StreamQuery()
        {
            var query = _freeSql.Select<T>();
            return new FreeSqlQuery<T>(query);
        }
    }


    /// <summary>
    /// FreeSql 查询类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FreeSqlQuery<T> : IQuery<T>
    {
        private FreeSql.ISelect<T> _query;

        public FreeSqlQuery(FreeSql.ISelect<T> query)
        {
            _query = query;
        }

        public IQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        {
            if (condition)
                _query = _query.Where(predicate);
            return this;
        }

        public IQuery<T> OrderByIf(bool condition, Expression<Func<T, object>> orderBy, bool desc = true)
        {
            if (condition)
            {
                 if (desc) {
                     _query = _query.OrderByDescending(orderBy);
                 } else {
                      _query = _query.OrderBy(orderBy);
                 }
            }
            return this;
        }

        public IQuery<T> Skip(int count)
        {
            _query = _query.Skip(count);
            return this;
        }

        public IQuery<T> Take(int count)
        {
            _query = _query.Limit(count);
            return this;
        }

        public List<T> ToList()
        {
            return _query.ToList();
        }

        public long Count()
        {
            return _query.Count();
        }
    }

}