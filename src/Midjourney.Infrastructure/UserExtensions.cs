using Midjourney.Infrastructure.Dto;

namespace Midjourney.Infrastructure
{
    public static class UserExtensions
{
    public static CurrentUserDto ToDto(this User model)
    {
        return new CurrentUserDto
        {
            Id = model.Id,
            Name = model.Name,
            Email = model.Email,
            Avatar = model.Avatar,
            Role = model.Role.ToString(),
            Active = model.Status == EUserStatus.NORMAL,
            ApiSecret = model.Token,
            Token = model.Token,
            Version = GlobalConfiguration.Version,
            DayDrawLimit = model.DayDrawLimit,
            DayDrawCount = model.DayDrawCount,
            TotalDrawCount = model.TotalDrawCount,
            CoreSize = model.CoreSize,
            QueueSize = model.QueueSize,
            ValidStartTime = model.ValidStartTime,
            ValidEndTime = model.ValidEndTime,
            LastLoginTime = model.LastLoginTime,
        };
    }
    
    // 集合转换
    public static IEnumerable<CurrentUserDto> ToDtos(this IEnumerable<User> models)
    {
        return models.Select(m => m.ToDto());
    }
}

}