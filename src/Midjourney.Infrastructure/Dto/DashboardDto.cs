namespace Midjourney.Infrastructure.Dto
{
    public class DashboardSummaryDto
    {
        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 今日任务数
        /// </summary>
        public int TodayTasks { get; set; }

        /// <summary>
        /// 正在运行的任务
        /// </summary>
        public int RunningTasks { get; set; }

        /// <summary>
        /// 等待中的任务
        /// </summary>
        public int WaitingTasks { get; set; }

        /// <summary>
        /// 可用账号数
        /// </summary>
        public int ActiveAccounts { get; set; }

        /// <summary>
        /// 总账号数
        /// </summary>
        public int TotalAccounts { get; set; }

        /// <summary>
        /// 任务成功率
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// 今日任务成功率
        /// </summary>
        public double TodaySuccessRate { get; set; }
    }
}