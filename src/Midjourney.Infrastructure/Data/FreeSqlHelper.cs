

using FreeSql;
using FreeSql.Aop;
using Serilog;

namespace Midjourney.Infrastructure.Data
{
    /// <summary>
    /// 任务帮助类
    /// </summary>
    public class FreeSqlHelper
    {
        private static IFreeSql _freeSql;

        public static IFreeSql FreeSql => _freeSql;

        /// <summary>
        /// 配置 FreeSql
        /// </summary>
        /// <param name="freeSql"></param>
        public static void Configure(IFreeSql freeSql)
        {
            _freeSql = freeSql;
        }

        /// <summary>
        /// 初始化 FreeSql
        /// </summary>
        public static IFreeSql Init(bool autoSyncStructure = false)
        {
            var setting = GlobalConfiguration.Setting;

            if (string.IsNullOrWhiteSpace(setting.DatabaseConnectionString))
            {
                return null;
            }

            var fsqlBuilder = new FreeSqlBuilder()
                .UseLazyLoading(true); // 开启延时加载功能

            // 生产环境注意
            // 自动同步实体结构到数据库
            if (autoSyncStructure)
            {
                fsqlBuilder.UseAutoSyncStructure(true)
                      // 监视 SQL 命令对象
                      .UseMonitorCommand(cmd =>
                      {
                          Log.Information(cmd.CommandText);
                      });
            }

#if DEBUG
            fsqlBuilder.UseAutoSyncStructure(true)
                  // 监视 SQL 命令对象
                  .UseMonitorCommand(cmd =>
                  {
                      Log.Information(cmd.CommandText);
                      Console.WriteLine(cmd.CommandText);
                  });
#endif


            switch (setting.DatabaseType)
            {
                case DatabaseType.LiteDB:
                case DatabaseType.MongoDB:
                    {
                        return null;
                    }
                case DatabaseType.SQLite:
                    {
                        // Data Source=|DataDirectory|\document.db; Attachs=xxxtb.db; Pooling=true;Min Pool Size=1
                        fsqlBuilder.UseConnectionString(DataType.Sqlite, @"Data Source=data/mj_sqlite.db");
                    }
                    break;
                case DatabaseType.MySQL:
                    {
                        // Data Source=192.168.3.241;Port=3306;User ID=root;Password=root; Initial Catalog=mj;Charset=utf8mb4; SslMode=none;Min pool size=1
                        fsqlBuilder.UseConnectionString(DataType.MySql, setting.DatabaseConnectionString);
                    }
                    break;
                case DatabaseType.PostgreSQL:
                    {
                        // Host=192.168.164.10;Port=5432;Username=postgres;Password=123456; Database=tedb;ArrayNullabilityMode=Always;Pooling=true;Minimum Pool Size=1
                        fsqlBuilder.UseConnectionString(DataType.PostgreSQL, setting.DatabaseConnectionString);
                    }
                    break;
                case DatabaseType.SQLServer:
                    {
                        // Data Source=.;User Id=sa;Password=123456;Initial Catalog=freesqlTest;Encrypt=True;TrustServerCertificate=True;Pooling=true;Min Pool Size=1
                        fsqlBuilder.UseConnectionString(DataType.SqlServer, setting.DatabaseConnectionString);
                    }
                    break;
                default:
                    break;
            }

            var fsql = fsqlBuilder.Build();

            fsql.UseJsonMap(); // 开启功能

            // 日志
            fsql.Aop.CurdAfter += (s, e) =>
            {
                if (e.CurdType == CurdType.Select)
                {
                    if (e.ElapsedMilliseconds > 2000)
                    {
                        Log.Warning(e.Sql);
                    }
                    else if (e.ElapsedMilliseconds > 500)
                    {
                        Log.Information(e.Sql);
                    }
                }
            };

            //Configure(fsql);

            //// 请务必定义成 Singleton 单例模式
            //services.AddSingleton(fsql);

            return fsql;
        }
    }
}
