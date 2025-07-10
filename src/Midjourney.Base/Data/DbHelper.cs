

using Midjourney.Base.Util;
using MongoDB.Driver;
using Serilog;

namespace Midjourney.Base.Data
{
    /// <summary>
    /// 任务帮助类
    /// </summary>
    public class DbHelper : SingletonBase<DbHelper>
    {
        private IDataHelper<TaskInfo> _taskStore;
        private IDataHelper<DiscordAccount> _accountStore;
        private IDataHelper<User> _userStore;
        private IDataHelper<DomainTag> _domainStore;
        private IDataHelper<BannedWord> _bannedWordStore;

        public DbHelper()
        {
            Init();
        }

        /// <summary>
        /// Task 数据库操作
        /// </summary>
        public IDataHelper<TaskInfo> TaskStore => _taskStore;

        /// <summary>
        /// Discord 账号数据库操作
        /// </summary>
        public IDataHelper<DiscordAccount> AccountStore => _accountStore;

        /// <summary>
        /// User 数据库操作
        /// </summary>
        public IDataHelper<User> UserStore => _userStore;

        /// <summary>
        /// 领域标签数据库操作
        /// </summary>
        public IDataHelper<DomainTag> DomainStore => _domainStore;

        /// <summary>
        /// 禁用词数据库操作
        /// </summary>
        public IDataHelper<BannedWord> BannedWordStore => _bannedWordStore;

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public void Init()
        {
            var setting = GlobalConfiguration.Setting;
            switch (setting.DatabaseType)
            {
                case DatabaseType.LiteDB:
                    {
                        _taskStore = LiteDBHelper.TaskStore;
                        _accountStore = LiteDBHelper.AccountStore;
                        _userStore = LiteDBHelper.UserStore;
                        _domainStore = LiteDBHelper.DomainStore;
                        _bannedWordStore = LiteDBHelper.BannedWordStore;
                    }
                    break;
                case DatabaseType.MongoDB:
                    {
                        _taskStore = new MongoDBRepository<TaskInfo>();
                        _accountStore = new MongoDBRepository<DiscordAccount>();
                        _userStore = new MongoDBRepository<User>();
                        _domainStore = new MongoDBRepository<DomainTag>();
                        _bannedWordStore = new MongoDBRepository<BannedWord>();
                    }
                    break;
                case DatabaseType.SQLite:
                case DatabaseType.MySQL:
                case DatabaseType.PostgreSQL:
                case DatabaseType.SQLServer:
                    {
                        _taskStore = new FreeSqlRepository<TaskInfo>();
                        _accountStore = new FreeSqlRepository<DiscordAccount>();
                        _userStore = new FreeSqlRepository<User>();
                        _domainStore = new FreeSqlRepository<DomainTag>();
                        _bannedWordStore = new FreeSqlRepository<BannedWord>();
                    }
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// 初始化数据库索引
        /// </summary>
        public void IndexInit()
        {
            try
            {
                LocalLock.TryLock("IndexInit", TimeSpan.FromSeconds(10), () =>
                {
                    var setting = GlobalConfiguration.Setting;
                    switch (setting.DatabaseType)
                    {
                        case DatabaseType.NONE:
                            break;
                        case DatabaseType.LiteDB:
                            {
                                // LiteDB 索引
                                var coll = LiteDBHelper.TaskStore.GetCollection();
                                coll.EnsureIndex(c => c.SubmitTime);
                                coll.EnsureIndex(c => c.Status);
                                coll.EnsureIndex(c => c.Action);
                                coll.EnsureIndex(c => c.UserId);
                                coll.EnsureIndex(c => c.ClientIp);
                                coll.EnsureIndex(c => c.InstanceId);
                            }
                            break;
                        case DatabaseType.MongoDB:
                            {

                                var coll = MongoHelper.GetCollection<TaskInfo>();

                                var index1 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Descending(c => c.SubmitTime));
                                coll.Indexes.CreateOne(index1);

                                var index2 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.PromptEn));
                                coll.Indexes.CreateOne(index2);

                                var index3 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Descending(c => c.Prompt));
                                coll.Indexes.CreateOne(index3);

                                var index4 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.InstanceId));
                                coll.Indexes.CreateOne(index4);

                                var index5 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.Status));
                                coll.Indexes.CreateOne(index5);

                                var index6 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.Action));
                                coll.Indexes.CreateOne(index6);

                                var index7 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.Description));
                                coll.Indexes.CreateOne(index7);

                                var index8 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.ImageUrl));
                                coll.Indexes.CreateOne(index8);

                                var index9 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.UserId));
                                coll.Indexes.CreateOne(index9);

                                var index10 = new CreateIndexModel<TaskInfo>(Builders<TaskInfo>.IndexKeys.Ascending(c => c.ClientIp));
                                coll.Indexes.CreateOne(index10);
                            }
                            break;
                        case DatabaseType.SQLite:
                            break;
                        case DatabaseType.MySQL:
                            break;
                        case DatabaseType.PostgreSQL:
                            break;
                        case DatabaseType.SQLServer:
                            break;
                        default:
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "初始化数据库索引异常");
            }
        }

        /// <summary>
        /// 验证数据库连接
        /// </summary>
        /// <returns></returns>
        public static bool Verify()
        {
            try
            {
                var setting = GlobalConfiguration.Setting;
                if (setting.DatabaseType == DatabaseType.LiteDB)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(setting.DatabaseConnectionString))
                {
                    switch (setting.DatabaseType)
                    {
                        case DatabaseType.MongoDB:
                            {
                                return MongoHelper.Verify();
                            }
                        case DatabaseType.SQLite:
                        case DatabaseType.MySQL:
                        case DatabaseType.PostgreSQL:
                        case DatabaseType.SQLServer:
                            {
                                // 首次初始化，并同步实体结构
                                var freeSql = FreeSqlHelper.Init(true);
                                if (freeSql != null)
                                {
                                    var obj = freeSql.Ado.ExecuteScalar("SELECT 1");
                                    var succees = obj != null && obj.ToString() == "1";
                                    if (succees)
                                    {
                                        // 同步实体结构

                                        // 注意：
                                        // 1. MySQL InnoDB 存储引擎的限制 1 行 65535 字节
                                        // 这个错误提示 "Row size too large" 表示你创建的表 DiscordAccount 的一行数据大小超过了 MySQL InnoDB 存储引擎的限制（65535 字节）。
                                        // 即使你已经使用了 TEXT 和 LONGTEXT 类型，但这些类型在计算行大小时仍然会占用一部分空间（指针大小，通常很小），而其他 VARCHAR 类型的列仍然会直接计入行大小。

                                        // 2. postgresql 需要启动扩展支持字典类型
                                        // CREATE EXTENSION hstore;

                                        freeSql.CodeFirst.SyncStructure(typeof(User),
                                            typeof(BannedWord),
                                            typeof(DiscordAccount),
                                            typeof(TaskInfo),
                                            typeof(DomainTag));

                                        // 验证成功后，确认配置当前数据库
                                        freeSql = FreeSqlHelper.Init(false);
                                        if (freeSql != null)
                                        {
                                            FreeSqlHelper.Configure(freeSql);
                                        }

                                        return succees;
                                    }
                                }

                                return false;
                            }
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库连接验证失败");
            }
            return false;
        }
    }
}
