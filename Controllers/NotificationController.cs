using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WMSClean.Services;

namespace WMSClean.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<IdentityUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);

            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                await _notificationService.MarkAsReadAsync(notification.Id);
            }

            return Ok();
        }
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }
    }
}