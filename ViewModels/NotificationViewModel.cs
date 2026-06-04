namespace WMSClean.ViewModels
{
    public class NotificationViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Notifications { get; set; } = new();
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Link { get; set; }
    }
}