using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Midjourney.Base.Data
{
    /// <summary>
    /// Mongo DB 单例辅助
    /// </summary>
    public abstract class MongoHelper : MongoHelper<MongoHelper> { }

    /// <summary>
    /// Mongo DB 单例辅助
    /// </summary>
    public abstract class MongoHelper<TMark>
    {
        private static readonly object _locker = new object();

        private static IMongoDatabase _instance;

        static MongoHelper()
        {

        }

        /// <summary>
        /// MongoDB 静态实列
        /// </summary>
        public static IMongoDatabase Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (_locker)
                {
                    if (_instance == null)
                    {
                        if (typeof(TMark) == typeof(MongoHelper))
                        {
                            var connectionString = GlobalConfiguration.Setting.DatabaseConnectionString;
                            var name = GlobalConfiguration.Setting.DatabaseName;

                            if (!string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(name))
                            {
                                var client = new MongoClient(connectionString);
                                var database = client.GetDatabase(name);
                                _instance = database;
                            }
                        }
                    }
                }

                if (_instance == null)
                {
                    throw new Exception("使用前请初始化 MongoHelper.Initialization(); ");
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMongoCollection<T> GetCollection<T>() => Instance.GetCollection<T>();

        /// <summary>
        /// Gets a collection by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IMongoCollection<T> GetCollection<T>(string name, MongoCollectionSettings settings = null)
            => Instance.GetCollection<T>(name, settings);

        /// <summary>
        /// 初始化 Mongo DB，使用默认的 Mongo DB 不需要初始化
        /// </summary>
        /// <param name="database"></param>
        public static void Initialization(IMongoDatabase database)
        {
            _instance = database;
        }

        /// <summary>
        /// 验证 mongo 连接 - 旧版连接
        /// </summary>
        /// <returns></returns>
        public static bool OldVerify()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GlobalConfiguration.Setting.MongoDefaultConnectionString)
                    || string.IsNullOrWhiteSpace(GlobalConfiguration.Setting.MongoDefaultDatabase))
                {
                    return false;
                }

                var client = new MongoClient(GlobalConfiguration.Setting.MongoDefaultConnectionString);
                var database = client.GetDatabase(GlobalConfiguration.Setting.MongoDefaultDatabase);

                return database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MongoDB 连接失败");

                return false;
            }
        }

        /// <summary>
        /// 验证 mongo 连接
        /// </summary>
        /// <returns></returns>
        public static bool Verify()
        {
            try
            {
                var setting = GlobalConfiguration.Setting;
                if (string.IsNullOrWhiteSpace(setting.DatabaseConnectionString)
                    || string.IsNullOrWhiteSpace(setting.DatabaseName))
                {
                    return false;
                }

                var client = new MongoClient(setting.DatabaseConnectionString);
                var database = client.GetDatabase(setting.DatabaseName);

                return database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MongoDB 连接失败");

                return false;
            }
        }
    }
}
