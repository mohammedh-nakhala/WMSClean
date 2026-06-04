using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WMSClean.Services;
using WMSClean.ViewModels;

namespace WMSClean.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationViewComponent(INotificationService notificationService, UserManager<IdentityUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if (string.IsNullOrEmpty(userId))
            {
                return View(new NotificationViewModel { UnreadCount = 0, Notifications = new List<NotificationDto>() });
            }

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);

            var model = new NotificationViewModel
            {
                UnreadCount = unreadCount,
                Notifications = notifications.Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    Type = n.Type,
                    Link = n.Link
                }).ToList()
            };

            return View(model);
        }
    }
}