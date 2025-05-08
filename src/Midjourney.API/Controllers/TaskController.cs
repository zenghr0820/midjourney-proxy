﻿

using Microsoft.AspNetCore.Mvc;
using Midjourney.Infrastructure.Data;
using Midjourney.Infrastructure.Dto;
using Midjourney.Infrastructure.LoadBalancer;
using Midjourney.Infrastructure.Services;
using System.Net;

namespace Midjourney.API.Controllers
{
    /// <summary>
    /// 控制器用于查询任务信息
    /// </summary>
    [ApiController]
    [Route("mj/task")]
    [Route("mj-fast/mj/task")]
    [Route("mj-turbo/mj/task")]
    [Route("mj-relax/mj/task")]
    public class TaskController : ControllerBase
    {
        private readonly ITaskStoreService _taskStoreService;
        private readonly ITaskService _taskService;

        private readonly DiscordLoadBalancer _discordLoadBalancer;
        private readonly WorkContext _workContext;

        public TaskController(
            ITaskStoreService taskStoreService,
            DiscordLoadBalancer discordLoadBalancer,
            ITaskService taskService,
            IHttpContextAccessor httpContextAccessor,
            WorkContext workContext)
        {
            _taskStoreService = taskStoreService;
            _discordLoadBalancer = discordLoadBalancer;
            _taskService = taskService;

            _workContext = workContext;

            var user = _workContext.GetUser();

            // 如果非演示模式、未开启访客，如果没有登录，直接返回 403 错误
            if (GlobalConfiguration.IsDemoMode != true
                && GlobalConfiguration.Setting.EnableGuest != true)
            {
                if (user == null)
                {
                    // 如果是普通用户, 并且不是匿名控制器，则返回 403
                    httpContextAccessor.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    httpContextAccessor.HttpContext.Response.WriteAsync("未登录");
                    return;
                }
            }
        }

        /// <summary>
        /// 根据任务ID获取任务信息
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>任务信息</returns>
        [HttpGet("{id}/fetch")]
        public ActionResult<TaskInfo> Fetch(string id)
        {
            var queueTask = _discordLoadBalancer.GetQueueTasks().FirstOrDefault(t => t.Id == id);
            return queueTask ?? _taskStoreService.Get(id);
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("{id}/cancel")]
        public ActionResult<TaskInfo> Cancel(string id)
        {
            if (GlobalConfiguration.IsDemoMode == true)
            {
                // 直接抛出错误
                return BadRequest("演示模式，禁止操作");
            }

            var queueTask = _discordLoadBalancer.GetQueueTasks().FirstOrDefault(t => t.Id == id);
            if (queueTask != null)
            {
                queueTask.Fail("主动取消任务");
            }

            return Ok();
        }

        /// <summary>
        /// 获取任务图片的seed（需设置mj或niji的私信ID）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/image-seed")]
        public async Task<ActionResult<SubmitResultVO>> ImageSeed(string id)
        {
            var targetTask = _taskStoreService.Get(id);
            if (targetTask != null)
            {
                if (!string.IsNullOrWhiteSpace(targetTask.Seed))
                {
                    return Ok(SubmitResultVO.Of(ReturnCode.SUCCESS, "成功", targetTask.Seed));
                }
                else
                {
                    var hash = targetTask.GetProperty<string>(Constants.TASK_PROPERTY_MESSAGE_HASH, default);
                    if (string.IsNullOrWhiteSpace(hash))
                    {
                        return BadRequest(SubmitResultVO.Fail(ReturnCode.VALIDATION_ERROR, "关联任务状态错误"));
                    }
                    else
                    {
                        // 有 hash 但没有 seed，说明任务已经完成，但是没有 seed
                        // 重新获取 seed
                        var data = await _taskService.SubmitSeed(targetTask);
                        return Ok(data);
                    }
                }
            }

            return NotFound(SubmitResultVO.Fail(ReturnCode.NOT_FOUND, "关联任务不存在或已失效"));
        }

        /// <summary>
        /// 获取任务队列中的所有任务
        /// </summary>
        /// <returns>任务队列中的所有任务</returns>
        [HttpGet("queue")]
        public ActionResult<List<TaskInfo>> Queue()
        {
            return Ok(_discordLoadBalancer.GetQueueTasks().OrderBy(t => t.SubmitTime).ToList());
        }

        /// <summary>
        /// 获取最新100条任务信息
        /// </summary>
        /// <returns>所有任务信息</returns>
        [HttpGet("list")]
        public ActionResult<List<TaskInfo>> List()
        {
            var data = DbHelper.Instance.TaskStore.Where(c => true, t => t.SubmitTime, false, 100).ToList();
            return Ok(data);
        }

        /// <summary>
        /// 根据条件查询任务信息/根据ID列表查询任务
        /// </summary>
        /// <param name="conditionDTO">任务查询条件</param>
        /// <returns>符合条件的任务信息</returns>
        [HttpPost("list-by-condition")]
        [HttpPost("list-by-ids")]
        public ActionResult<List<TaskInfo>> ListByCondition([FromBody] TaskConditionDTO conditionDTO)
        {
            if (conditionDTO.Ids == null || !conditionDTO.Ids.Any())
            {
                return Ok(new List<TaskInfo>());
            }

            var result = new List<TaskInfo>();
            var notInQueueIds = new HashSet<string>(conditionDTO.Ids);

            foreach (var task in _discordLoadBalancer.GetQueueTasks())
            {
                if (notInQueueIds.Contains(task.Id))
                {
                    result.Add(task);
                    notInQueueIds.Remove(task.Id);
                }
            }

            var list = _taskStoreService.GetList(notInQueueIds.ToList());
            if (list.Any())
            {
                result.AddRange(list);
            }

            //foreach (var id in notInQueueIds)
            //{
            //    var task = _taskStoreService.Get(id);
            //    if (task != null)
            //    {
            //        result.Add(task);
            //    }
            //}

            return Ok(result);
        }

        /// <summary>
        /// 删除Discord消息
        /// </summary>
        /// <param name="messageId">Discord消息ID</param>
        /// <returns>删除结果</returns>
        [HttpPost("delete-message/{messageId}")]
        public string DeleteMessage(string messageId)
        {
            // if (GlobalConfiguration.IsDemoMode == true)
            // {
            //     return BadRequest(SubmitResultVO.Fail(ReturnCode.VALIDATION_ERROR, "演示模式，禁止操作"));
            // }

            // if (string.IsNullOrWhiteSpace(messageId))
            // {
            //     return BadRequest(SubmitResultVO.Fail(ReturnCode.VALIDATION_ERROR, "消息ID不能为空"));
            // }

            // var instance = _discordLoadBalancer.GetInstance();
            // if (instance == null)
            // {
            //     return BadRequest(SubmitResultVO.Fail(ReturnCode.VALIDATION_ERROR, "没有可用的Discord实例"));
            // }

            // var result = await instance.DeleteMessageAsync(messageId);
            // if (result.Code != ReturnCode.SUCCESS)
            // {
            //     return BadRequest(SubmitResultVO.Fail(result.Code, result.Description));
            // }

            return "删除成功";
        }
    }
}