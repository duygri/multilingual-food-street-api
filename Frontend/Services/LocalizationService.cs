namespace FoodStreet.Client.Services
{
    public interface ILocalizationService
    {
        string this[string key] { get; }
        string CurrentLanguage { get; }
        event Action? OnLanguageChanged;
        void SetLanguage(string languageCode);
    }

    public class LocalizationService : ILocalizationService
    {
        public string CurrentLanguage { get; private set; } = "vi-VN";
        public event Action? OnLanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["vi-VN"] = new Dictionary<string, string>
            {
                // Login Page
                ["WelcomeBack"] = "Chào mừng trở lại",
                ["CreateAccount"] = "Tạo tài khoản",
                ["SignInSubtitle"] = "Đăng nhập để vào trang quản trị",
                ["RegisterSubtitle"] = "Đăng ký tài khoản quản trị mới",
                ["FullName"] = "Họ và tên",
                ["Email"] = "Email",
                ["Password"] = "Mật khẩu",
                ["ConfirmPassword"] = "Xác nhận mật khẩu",
                ["YourNamePlaceholder"] = "Nhập tên của bạn",
                ["EmailPlaceholder"] = "admin@example.com",
                ["PasswordPlaceholder"] = "••••••••",
                ["SignInButton"] = "Đăng nhập",
                ["RegisterButton"] = "Tạo tài khoản",
                ["NoAccountRegister"] = "Chưa có tài khoản? Đăng ký",
                ["HaveAccountSignIn"] = "Đã có tài khoản? Đăng nhập",
                ["BackToHome"] = "Về trang chủ",
                ["LoginFailed"] = "Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.",
                ["PasswordsDoNotMatch"] = "Mật khẩu không khớp.",
                ["RegistrationSuccess"] = "Tạo tài khoản thành công! Bạn có thể đăng nhập ngay.",
                ["RegistrationFailed"] = "Đăng ký thất bại.",
                ["PleaseTryAgain"] = "Vui lòng thử lại.",
                ["ConnectionError"] = "Lỗi kết nối",
                ["EmailRequired"] = "Vui lòng nhập email",
                ["InvalidEmail"] = "Email không hợp lệ",
                ["PasswordRequired"] = "Vui lòng nhập mật khẩu",
                ["PasswordTooShort"] = "Mật khẩu phải có ít nhất 6 ký tự",

                // Dashboard
                ["Dashboard"] = "Bảng điều khiển",
                ["TotalFoods"] = "Tổng món ăn",
                ["TotalAudios"] = "Tổng audio",
                ["TotalUsers"] = "Tổng người dùng",
                ["QuickActions"] = "Thao tác nhanh",
                ["AddNewFood"] = "Thêm món ăn mới",
                ["ManageAudio"] = "Quản lý Audio",
                ["Translations"] = "Bản dịch",
                ["SystemInfo"] = "Thông tin hệ thống",
                ["ServerStatus"] = "Trạng thái máy chủ",
                ["Online"] = "Hoạt động",
                ["ClientVersion"] = "Phiên bản Client",

                // ManageFood
                ["ManageFoods"] = "Quản lý món ăn",
                ["AddFood"] = "Thêm món ăn",
                ["EditFood"] = "Sửa món ăn",
                ["DeleteFood"] = "Xóa món ăn",
                ["FoodName"] = "Tên món ăn",
                ["Description"] = "Mô tả",
                ["Latitude"] = "Vĩ độ",
                ["Longitude"] = "Kinh độ",
                ["Actions"] = "Thao tác",
                ["Save"] = "Lưu",
                ["Cancel"] = "Hủy",
                ["NoFoodsFound"] = "Chưa có món ăn nào",
                ["Loading"] = "Đang tải...",

                // Navigation
                ["Home"] = "Trang chủ",
                ["AdminPanel"] = "Quản trị",
                ["Settings"] = "Cài đặt",
                ["Logout"] = "Đăng xuất",
                ["StreetFoods"] = "Món Ăn Đường Phố",
                ["AccessDenied"] = "Truy cập bị từ chối",
                ["PleaseLoginMessage"] = "Vui lòng đăng nhập để tiếp tục",
                ["Translations"] = "Bản dịch",

                // Home Page
                ["AppTitle"] = "Hướng Dẫn Phố Ẩm Thực",
                ["HeroTitle"] = "Khám Phá Ẩm Thực Đường Phố",
                ["HeroSubtitle"] = "Trải nghiệm hương vị địa phương với hướng dẫn GPS thông minh và thuyết minh đa ngôn ngữ",
                ["ExploreNow"] = "Khám Phá Ngay",
                ["FeaturesTitle"] = "Tính Năng Nổi Bật",
                ["FeatureGPS"] = "Định Vị GPS",
                ["FeatureGPSDesc"] = "Tự động tìm các món ăn ngon gần bạn nhất",
                ["FeatureMultilingual"] = "Đa Ngôn Ngữ",
                ["FeatureMultilingualDesc"] = "Hỗ trợ Tiếng Việt, Anh, Nhật, Hàn, Trung",
                ["FeatureAudio"] = "Thuyết Minh Audio",
                ["FeatureAudioDesc"] = "Nghe giới thiệu về từng món ăn bằng giọng nói",
                ["FeatureMobile"] = "Tối Ưu Di Động",
                ["FeatureMobileDesc"] = "Hoạt động mượt mà trên mọi thiết bị",
                ["StatFoods"] = "Món Ăn",
                ["StatLanguages"] = "Ngôn Ngữ",
                ["StatAudios"] = "File Audio",
                ["CTATitle"] = "Sẵn Sàng Khám Phá?",
                ["CTASubtitle"] = "Bắt đầu hành trình ẩm thực của bạn ngay hôm nay!",
                ["GetStarted"] = "Bắt Đầu Ngay"
            },
            ["en-US"] = new Dictionary<string, string>
            {
                // Login Page
                ["WelcomeBack"] = "Welcome Back",
                ["CreateAccount"] = "Create Account",
                ["SignInSubtitle"] = "Sign in to access admin panel",
                ["RegisterSubtitle"] = "Register a new admin account",
                ["FullName"] = "Full Name",
                ["Email"] = "Email",
                ["Password"] = "Password",
                ["ConfirmPassword"] = "Confirm Password",
                ["YourNamePlaceholder"] = "Your name",
                ["EmailPlaceholder"] = "admin@example.com",
                ["PasswordPlaceholder"] = "••••••••",
                ["SignInButton"] = "Sign In",
                ["RegisterButton"] = "Create Account",
                ["NoAccountRegister"] = "Don't have an account? Register",
                ["HaveAccountSignIn"] = "Already have an account? Sign In",
                ["BackToHome"] = "Back to Home",
                ["LoginFailed"] = "Login failed. Please check your email and password.",
                ["PasswordsDoNotMatch"] = "Passwords do not match.",
                ["RegistrationSuccess"] = "Account created successfully! You can now sign in.",
                ["RegistrationFailed"] = "Registration failed.",
                ["PleaseTryAgain"] = "Please try again.",
                ["ConnectionError"] = "Connection error",
                ["EmailRequired"] = "Please enter email",
                ["InvalidEmail"] = "Invalid email format",
                ["PasswordRequired"] = "Please enter password",
                ["PasswordTooShort"] = "Password must be at least 6 characters",

                // Dashboard
                ["Dashboard"] = "Dashboard",
                ["TotalFoods"] = "Total Foods",
                ["TotalAudios"] = "Total Audios",
                ["TotalUsers"] = "Total Users",
                ["QuickActions"] = "Quick Actions",
                ["AddNewFood"] = "Add New Food",
                ["ManageAudio"] = "Manage Audio",
                ["Translations"] = "Translations",
                ["SystemInfo"] = "System Info",
                ["ServerStatus"] = "Server Status",
                ["Online"] = "Online",
                ["ClientVersion"] = "Client Version",

                // ManageFood
                ["ManageFoods"] = "Manage Foods",
                ["AddFood"] = "Add Food",
                ["EditFood"] = "Edit Food",
                ["DeleteFood"] = "Delete Food",
                ["FoodName"] = "Food Name",
                ["Description"] = "Description",
                ["Latitude"] = "Latitude",
                ["Longitude"] = "Longitude",
                ["Actions"] = "Actions",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["NoFoodsFound"] = "No foods found",
                ["Loading"] = "Loading...",

                // Navigation
                ["Home"] = "Home",
                ["AdminPanel"] = "Admin Panel",
                ["Settings"] = "Settings",
                ["Logout"] = "Logout",
                ["StreetFoods"] = "Street Foods",
                ["AccessDenied"] = "Access Denied",
                ["PleaseLoginMessage"] = "Please login to continue",
                ["Translations"] = "Translations",

                // Home Page
                ["AppTitle"] = "Street Food Guide",
                ["HeroTitle"] = "Discover Street Food",
                ["HeroSubtitle"] = "Experience local flavors with smart GPS guidance and multilingual narration",
                ["ExploreNow"] = "Explore Now",
                ["FeaturesTitle"] = "Key Features",
                ["FeatureGPS"] = "GPS Location",
                ["FeatureGPSDesc"] = "Automatically find delicious food near you",
                ["FeatureMultilingual"] = "Multilingual",
                ["FeatureMultilingualDesc"] = "Support Vietnamese, English, Japanese, Korean, Chinese",
                ["FeatureAudio"] = "Audio Guide",
                ["FeatureAudioDesc"] = "Listen to introductions about each dish",
                ["FeatureMobile"] = "Mobile Optimized",
                ["FeatureMobileDesc"] = "Works smoothly on all devices",
                ["StatFoods"] = "Foods",
                ["StatLanguages"] = "Languages",
                ["StatAudios"] = "Audio Files",
                ["CTATitle"] = "Ready to Explore?",
                ["CTASubtitle"] = "Start your culinary journey today!",
                ["GetStarted"] = "Get Started"
            }
        };

        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(CurrentLanguage, out var langDict))
                {
                    if (langDict.TryGetValue(key, out var value))
                        return value;
                }
                // Fallback to English
                if (_translations["en-US"].TryGetValue(key, out var fallback))
                    return fallback;
                return key; // Return key if not found
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (CurrentLanguage != languageCode && _translations.ContainsKey(languageCode))
            {
                CurrentLanguage = languageCode;
                OnLanguageChanged?.Invoke();
            }
        }
    }
}
