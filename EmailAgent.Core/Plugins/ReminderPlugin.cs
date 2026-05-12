using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;

namespace EmailAgent.Core.Agents
{
    /// <summary>
    /// SK Plugin: Quản lý nhắc việc / lịch hẹn với thông báo qua Email.
    /// Background thread sẽ kiểm tra và gửi email nhắc đúng giờ.
    /// </summary>
    public class ReminderPlugin
    {
        private static readonly List<ReminderItem> _reminders = new();
        private static int _nextId = 1;
        private static readonly object _lock = new();

        [KernelFunction("AddReminder")]
        [Description("Thêm nhắc việc hoặc lịch hẹn mới. Gọi khi user muốn đặt lịch, nhắc nhở công việc. Sẽ tự động gửi email nhắc đúng giờ nếu có địa chỉ email.")]
        public string AddReminder(
            [Description("Nội dung công việc, ví dụ: 'Họp team', 'Gửi báo cáo'")] string title,
            [Description("Thời gian nhắc định dạng HH:mm, ví dụ: '15:00', '09:30'")] string time,
            [Description("Email nhận thông báo nhắc việc, ví dụ: 'abc@gmail.com'")] string notifyEmail,
            [Description("Ghi chú thêm, có thể để trống")] string note = "")
        {
            if (!TimeOnly.TryParse(time, out var parsedTime))
                return $"❌ Định dạng giờ không hợp lệ: '{time}'. Vui lòng nhập theo định dạng HH:mm (ví dụ: 15:00).";

            var now = DateTime.Now;
            var reminderDateTime = new DateTime(now.Year, now.Month, now.Day,
                parsedTime.Hour, parsedTime.Minute, 0);

            // Nếu giờ đã qua thì đặt cho ngày mai
            if (reminderDateTime <= now)
                reminderDateTime = reminderDateTime.AddDays(1);

            var item = new ReminderItem
            {
                Id = _nextId++,
                Title = title,
                Time = time,
                NotifyEmail = notifyEmail,
                Note = note,
                ReminderAt = reminderDateTime,
                CreatedAt = now,
                IsDone = false,
                EmailSent = false
            };

            lock (_lock) _reminders.Add(item);

            var dayLabel = reminderDateTime.Date > now.Date ? "ngày mai" : "hôm nay";
            return $"✅ Đã đặt nhắc việc #{item.Id}: \"{item.Title}\"\n" +
                   $"   🕐 {time} {dayLabel} ({reminderDateTime:dd/MM/yyyy HH:mm})\n" +
                   $"   📧 Email nhắc sẽ gửi tới: {notifyEmail}";
        }

        [KernelFunction("ListReminders")]
        [Description("Xem danh sách tất cả nhắc việc. Gọi khi user hỏi 'việc cần làm', 'lịch hôm nay', 'danh sách nhắc việc', 'còn việc gì không'.")]
        public string ListReminders(
            [Description("Lọc: 'all' = tất cả, 'pending' = chưa xong, 'done' = đã xong")] string filter = "pending")
        {
            List<ReminderItem> list;
            lock (_lock)
            {
                list = filter switch
                {
                    "done" => _reminders.Where(r => r.IsDone).ToList(),
                    "all" => _reminders.ToList(),
                    _ => _reminders.Where(r => !r.IsDone).ToList()
                };
            }

            if (list.Count == 0)
                return "Không có việc nào cần làm.";

            var sb = new StringBuilder();
            sb.AppendLine($"📋 Danh sách nhắc việc ({list.Count} mục):");
            sb.AppendLine(new string('─', 44));

            foreach (var r in list.OrderBy(r => r.ReminderAt))
            {
                var status = r.IsDone ? "✅" : "⏳";
                var emailStatus = r.EmailSent ? " (đã gửi email nhắc)" : "";
                sb.AppendLine($"{status} [{r.Id}] {r.Title}");
                sb.AppendLine($"      🕐 {r.ReminderAt:dd/MM/yyyy HH:mm}{emailStatus}");
                sb.AppendLine($"      📧 {r.NotifyEmail}");
                if (!string.IsNullOrWhiteSpace(r.Note))
                    sb.AppendLine($"      📝 {r.Note}");
            }

            return sb.ToString().TrimEnd();
        }

        [KernelFunction("CompleteReminder")]
        [Description("Đánh dấu nhắc việc là đã hoàn thành. Gọi khi user nói 'xong việc số X', 'hoàn thành task X', 'đã làm xong X'.")]
        public string CompleteReminder(
            [Description("ID của nhắc việc cần đánh dấu hoàn thành")] int id)
        {
            ReminderItem? item;
            lock (_lock) item = _reminders.FirstOrDefault(r => r.Id == id);

            if (item == null) return $"❌ Không tìm thấy nhắc việc #{id}.";
            if (item.IsDone) return $"ℹ️ Nhắc việc #{id} đã hoàn thành trước đó rồi.";

            item.IsDone = true;
            item.CompletedAt = DateTime.Now;
            return $"✅ Đã hoàn thành: \"{item.Title}\"";
        }

        [KernelFunction("DeleteReminder")]
        [Description("Xóa một nhắc việc. Gọi khi user nói 'xóa việc số X', 'bỏ nhắc việc X'.")]
        public string DeleteReminder(
            [Description("ID của nhắc việc cần xóa")] int id)
        {
            lock (_lock)
            {
                var item = _reminders.FirstOrDefault(r => r.Id == id);
                if (item == null) return $"❌ Không tìm thấy nhắc việc #{id}.";
                _reminders.Remove(item);
                return $"🗑️ Đã xóa nhắc việc #{id}: \"{item.Title}\"";
            }
        }

        // ════════════════════════════════════════════════
        // GỌI BỞI BACKGROUND THREAD — không phải SK Plugin
        // ════════════════════════════════════════════════
        public static List<ReminderItem> GetDueReminders()
        {
            var now = DateTime.Now;
            lock (_lock)
                return _reminders
                    .Where(r => !r.IsDone && !r.EmailSent && r.ReminderAt <= now)
                    .ToList();
        }

        public static void MarkEmailSent(int id)
        {
            lock (_lock)
            {
                var item = _reminders.FirstOrDefault(r => r.Id == id);
                if (item != null) item.EmailSent = true;
            }
        }
    }

    public class ReminderItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Time { get; set; } = "";
        public string NotifyEmail { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime ReminderAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsDone { get; set; }
        public bool EmailSent { get; set; }
    }
}