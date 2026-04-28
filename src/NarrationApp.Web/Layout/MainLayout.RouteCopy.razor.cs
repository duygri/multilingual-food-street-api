namespace NarrationApp.Web.Layout;

public partial class MainLayout
{
    private void UpdateRouteCopy()
    {
        var route = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).Trim('/').ToLowerInvariant();

        (_eyebrow, _heading, _summary) = route switch
        {
            "auth/login" => ("Truy cập", "Đăng nhập portal", "Sử dụng tài khoản admin hoặc chủ POI đã được duyệt để truy cập không gian vận hành phù hợp."),
            "auth/register" => ("Đăng ký", "Đăng ký owner", "Gửi yêu cầu đăng ký owner để chờ admin duyệt trước khi vào được cổng quản lý chủ POI."),
            "admin/dashboard" => ("Admin", "Dashboard", "Admin / Tổng quan"),
            "admin/poi-management" => ("Admin", "Quản lý POI", "Admin / Quản lý nội dung > POI"),
            "admin/moderation-queue" => ("Admin", "Moderation Queue", "Admin / Vận hành > Moderation"),
            "admin/analytics" => ("Admin", "Analytics", "Admin / Hệ thống > Analytics — Theo dõi điểm POI nổi bật và mức nghe audio để nhìn ra hành vi hệ thống."),
            "admin/language-management" => ("Admin", "Quản lý ngôn ngữ hệ thống", "Admin / Hệ thống > Ngôn ngữ — Theo dõi vùng phủ đa ngôn ngữ, bật hoặc mở rộng danh sách ngôn ngữ đang dùng."),
            "admin/audio-management" => ("Admin", "Audio Management", "Admin / Quản lý nội dung > Audio"),
            "admin/translation-review" => ("Admin", "Bản dịch", "Admin / Quản lý nội dung > Bản dịch — Google Cloud Translation cho phép admin rà soát và chỉnh sửa nội dung."),
            "admin/category-management" => ("Admin", "Danh mục", "Admin / Quản lý nội dung > Danh mục"),
            "admin/qr-management" => ("Admin", "QR Codes", "Admin / Quản lý nội dung > QR Codes"),
            "admin/tour-management" => ("Admin", "Tour Management", "Admin / Quản lý nội dung > Tour"),
            "admin/user-management" or "admin/visitor-devices" => ("Admin", "Thiết bị visitor", "Admin / Vận hành > Visitor devices — Theo dõi tổng visitor và visitor đang online theo từng thiết bị."),
            "owner/dashboard" => ("Chủ POI", "Bảng theo dõi POI", "Nắm trạng thái nội dung, geofence, audio hiện có và thông báo kiểm duyệt của POI."),
            "owner/pois" => ("Chủ POI", "POI", "Quản lý danh sách POI, tìm nhanh theo tên và mở từng điểm để cập nhật nội dung."),
            "owner/pois/new" => ("Chủ POI", "Tạo POI mới", "Tạo bản nháp POI trong một màn riêng trước khi gửi duyệt."),
            _ when route.StartsWith("owner/pois/", StringComparison.Ordinal) => ("Chủ POI", "Chi tiết POI", "Xem trạng thái, audio, geofence và kiểm duyệt của từng POI trong một không gian tập trung."),
            "owner/poi-management" => ("Chủ POI", "Quản lý POI", "Chọn POI, cập nhật vùng kích hoạt, metadata, nội dung nguồn và gửi duyệt theo luồng quản lý chủ POI trong PRD."),
            "owner/moderation" => ("Chủ POI", "Moderation", "Theo dõi các POI đang chờ duyệt, kết quả xử lý gần đây và ghi chú phản hồi từ admin."),
            "owner/notifications" => ("Chủ POI", "Notifications", "Xem lịch sử thông báo, trạng thái chưa đọc và các cập nhật vận hành quan trọng."),
            "owner/profile" => ("Chủ POI", "Profile", "Quản lý hồ sơ và bảo mật tài khoản owner trong một không gian riêng."),
            _ => ("Cổng truy cập", "Portal vận hành thuyết minh", "Không gian vận hành dành cho admin và chủ POI để quản trị nội dung, kiểm duyệt và trạng thái hệ thống.")
        };
    }
}
