using Microsoft.AspNetCore.Mvc;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.Services;

using TaskStatus = Midjourney.Infrastructure.TaskStatus;

namespace Midjourney.API.Controllers
{
    [ApiController]
    [Route("mj/admin/dashboard")]
    public class DashboardController : ControllerBase
    {

        private readonly ITaskService _taskService;
        private readonly WorkContext _workContext;

        public DashboardController(ITaskService taskService, WorkContext workContext)
        {
            _taskService = taskService;
            _workContext = workContext;
        }

        [HttpGet("summary")]
        public Result<DashboardSummaryDto> Summary()
        {

            // 获取用户数据
            var user = _workContext.GetUser();

            var resultDto = new DashboardSummaryDto();

            // 获取总的任务数据
            resultDto.TotalTasks = (int)DbHelper.Instance.TaskStore.StreamQuery()
                .WhereIf(user?.Role == EUserRole.USER, x => x.UserId == user.Id)
                .Count();

            // // 今日任务数
            var now = new DateTimeOffset(DateTime.Now.Date).ToUnixTimeMilliseconds();
            resultDto.TodayTasks = (int)DbHelper.Instance.TaskStore.StreamQuery()
                .WhereIf(true, x => x.SubmitTime >= now)
                .Count();

            // 运行中任务
            resultDto.RunningTasks = (int)DbHelper.Instance.TaskStore.StreamQuery()
                .WhereIf(true, x => x.Status == TaskStatus.IN_PROGRESS)
                .Count();

            // 等待中任务
            resultDto.WaitingTasks = (int)DbHelper.Instance.TaskStore.StreamQuery()
                .WhereIf(true, x => x.Status == TaskStatus.MODAL)
                .Count();

            // 账号统计
            resultDto.TotalAccounts = (int)DbHelper.Instance.AccountStore.StreamQuery().Count();
            resultDto.ActiveAccounts = (int)DbHelper.Instance.AccountStore.StreamQuery()
                .WhereIf(true, x => x.Enable == true)
                .Count();

            // 计算总成功率
            if (resultDto.TotalTasks > 0)
            {
                // 获取总的成功任务数
                var okTaskNum = DbHelper.Instance.TaskStore.StreamQuery()
                    .WhereIf(true, x => x.Status == TaskStatus.SUCCESS)
                    .Count();
                resultDto.SuccessRate = Math.Round((double)okTaskNum / resultDto.TotalTasks * 100, 2);
            }

            // 计算今日成功率
            if (resultDto.TodayTasks > 0)
            {
                // 获取今日的成功任务数
                var okTaskNum = DbHelper.Instance.TaskStore.StreamQuery()
                    .WhereIf(true, x => x.Status == TaskStatus.SUCCESS)
                    .WhereIf(true, x => x.SubmitTime >= now)
                    .Count();

                resultDto.TodaySuccessRate = Math.Round((double)okTaskNum / resultDto.TodayTasks * 100, 2);
            }


            return Result.Ok(resultDto);
        }
    }
}