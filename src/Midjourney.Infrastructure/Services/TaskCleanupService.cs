using Microsoft.Extensions.Hosting;
using Midjourney.Infrastructure.Data;
using Serilog;

namespace Midjourney.Infrastructure.Services
{
    /// <summary>
    /// 定时清理任务服务
    /// </summary>
    public class TaskCleanupService : IHostedService, IDisposable
    {
        private Timer _timer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 每小时执行一次
            _timer = new Timer(DoCleanup, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private void DoCleanup(object state)
        {
            try
            {
                Log.Information("正在执行任务清理...");

                var cutoffTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();

                // 查询超过1小时的任务（使用时间戳比较）
                List<TaskInfo> tasksToClean = DbHelper.Instance.TaskStore.Where(x => (x.Status == TaskStatus.MODAL || 
                    x.Status == TaskStatus.SUBMITTED || 
                    x.Status == TaskStatus.IN_PROGRESS) && 
                    x.SubmitTime < cutoffTime).ToList();

                Log.Warning($"发现{tasksToClean.Count}个超时任务，开始清理...");

                // 更新任务状态为异常
                foreach (var task in tasksToClean)
                {
                    // 增加详细日志记录
                    Log.Debug($"清理任务：{task.Id}，提交时间：{task.SubmitTime:yyyy-MM-dd HH:mm:ss}");

                    task.Fail("系统检测任务超时，标记为异常");
                    DbHelper.Instance.TaskStore.Update(task);
                }

                Log.Information($"共清理{tasksToClean.Count}个超时任务");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "任务清理异常");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}