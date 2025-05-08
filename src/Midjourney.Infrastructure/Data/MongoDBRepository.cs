

using MailKit.Search;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Midjourney.Infrastructure.Data
{
    public class MongoDBRepository<T> : IDataHelper<T> where T : IBaseId
    {
        private readonly IMongoCollection<T> _collection;

        public MongoDBRepository()
        {
            _collection = MongoHelper.GetCollection<T>();
        }

        public IMongoCollection<T> MongoCollection => _collection;

        public void Init()
        {
            // MongoDB 不需要显式地创建索引，除非你需要额外的索引
        }

        public void Add(T entity)
        {
            _collection.InsertOne(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _collection.InsertMany(entities);
        }

        public void Delete(T entity)
        {
            var filter = Builders<T>.Filter.Eq("_id", entity.Id);
            _collection.DeleteOne(filter);
        }

        public int Delete(Expression<Func<T, bool>> predicate)
        {
            var result = _collection.DeleteMany(predicate);
            return (int)result.DeletedCount;
        }

        public void Update(T entity)
        {
            var filter = Builders<T>.Filter.Eq("_id", entity.Id);
            _collection.ReplaceOne(filter, entity);
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
            var model = _collection.Find(c => c.Id == item.Id).FirstOrDefault();
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
            _collection.ReplaceOne(c => c.Id == item.Id, model);

            return true;
        }


        public List<T> GetAll()
        {
            return _collection.Find(Builders<T>.Filter.Empty).ToList();
        }


        /// <summary>
        /// 获取所有实体的 ID 列表。
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllIds()
        {
            return _collection.Find(Builders<T>.Filter.Empty).Project(x => x.Id).ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> predicate)
        {
            return _collection.Find(predicate).ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc = true)
        {
            var query = _collection.Find(filter);
            if (orderByAsc)
            {
                query = query.SortBy(orderBy);
            }
            else
            {
                query = query.SortByDescending(orderBy);
            }
            return query.ToList();
        }

        public T Single(Expression<Func<T, bool>> predicate)
        {
            return _collection.Find(predicate).FirstOrDefault();
        }

        public T Single(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc = true)
        {
            var query = _collection.Find(filter);
            if (orderByAsc)
            {
                query = query.SortBy(orderBy);
            }
            else
            {
                query = query.SortByDescending(orderBy);
            }
            return query.FirstOrDefault();
        }

        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return _collection.Find(predicate).Any();
        }

        public long Count(Expression<Func<T, bool>> predicate)
        {
            return _collection.CountDocuments(predicate);
        }

        public long Count()
        {
            return _collection.CountDocuments(c => true);
        }

        public void Save(T entity)
        {
            if (entity != null && !string.IsNullOrEmpty(entity.Id))
            {
                var model = _collection.Find(c => c.Id == entity.Id).FirstOrDefault();
                if (model == null)
                {
                    _collection.InsertOne(entity);
                }
                else
                {
                    _collection.ReplaceOne(c => c.Id == entity.Id, entity);
                }
            }
        }

        public void Delete(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            _collection.DeleteOne(filter);
        }

        public T Get(string id)
        {
            return _collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefault();
        }

        public List<T> List()
        {
            return _collection.Find(Builders<T>.Filter.Empty).ToList();
        }

        public List<T> Where(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool orderByAsc, int limit)
        {
            var query = _collection.Find(filter);
            if (orderByAsc)
            {
                return query.SortBy(orderBy).Limit(limit).ToList();
            }
            else
            {
                return query.SortByDescending(orderBy).Limit(limit).ToList();
            }
        }

        public IQuery<T> StreamQuery()
        {
            return new MongoDBQuery<T>(_collection);
        }
    }

    /// <summary>
    /// MongoDB 查询类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MongoDBQuery<T> : IQuery<T>
    {
        private IQueryable<T> _query;

        public MongoDBQuery(IMongoCollection<T> collection)
        {
            _query = collection.AsQueryable();
        }

        public IQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        {
            if (condition)
            {
                _query = _query.Where(predicate);
            }
            return this;
        }

        public IQuery<T> OrderByIf(bool condition, Expression<Func<T, object>> keySelector, bool desc = true)
        {

             if (condition)
            {
                 if (desc) {
                     _query = _query.OrderByDescending(keySelector);
                 } else {
                      _query = _query.OrderBy(keySelector);
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
            _query = _query.Take(count);
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
