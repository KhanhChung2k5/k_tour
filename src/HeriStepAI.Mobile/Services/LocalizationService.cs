namespace HeriStepAI.Mobile.Services;

public class LocalizationService : ILocalizationService
{
    private string _currentLanguage = "vi";
    private const string PreferenceKey = "AppLanguage";

    public string CurrentLanguage => _currentLanguage;
    public bool IsVietnamese => _currentLanguage == "vi";

    public event EventHandler? LanguageChanged;

    private static readonly HashSet<string> SupportedLanguages = new()
        { "vi", "en", "ko", "zh", "ja", "th", "fr" };

    public LocalizationService()
    {
        try
        {
            var saved = Preferences.Get(PreferenceKey, "vi");
            if (SupportedLanguages.Contains(saved))
                _currentLanguage = saved;
        }
        catch { }
    }

    public void SetLanguage(string languageCode)
    {
        var lang = SupportedLanguages.Contains(languageCode) ? languageCode : "vi";
        if (_currentLanguage == lang) return;
        _currentLanguage = lang;
        try { Preferences.Set(PreferenceKey, lang); } catch { }
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string GetString(string key)
    {
        if (Translations.TryGetValue(key, out var dict) && dict.TryGetValue(_currentLanguage, out var value))
            return value;
        // Fallback to Vietnamese, then English, then key
        if (Translations.TryGetValue(key, out var fallback))
            return fallback.GetValueOrDefault("vi") ?? fallback.GetValueOrDefault("en") ?? key;
        return key;
    }

    // ── Translations: key → { langCode → text } ──
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        // ── App ──
        ["AppTitle"] = L("HERISTEP AI"),
        ["AppSubtitle"] = new() { ["vi"]="ĐÀ NẴNG", ["en"]="DA NANG", ["ko"]="다낭", ["zh"]="岘港", ["ja"]="ダナン", ["th"]="ดานัง", ["fr"]="DA NANG" },
        ["Welcome"] = new() { ["vi"]="Chào mừng bạn đến với", ["en"]="Welcome to", ["ko"]="환영합니다", ["zh"]="欢迎来到", ["ja"]="ようこそ", ["th"]="ยินดีต้อนรับสู่", ["fr"]="Bienvenue à" },
        ["AppName"] = L("HeriStepAI"),
        ["AppTagline"] = new() { ["vi"]="Ứng dụng thuyết minh tự động cho chuyến đi của bạn", ["en"]="Auto narration app for your trip", ["ko"]="여행을 위한 자동 해설 앱", ["zh"]="旅行自动解说应用", ["ja"]="旅のための自動ガイドアプリ", ["th"]="แอปบรรยายอัตโนมัติสำหรับทริปของคุณ", ["fr"]="Application de narration automatique pour votre voyage" },

        // ── Navigation ──
        ["Home"] = new() { ["vi"]="Trang chủ", ["en"]="Home", ["ko"]="홈", ["zh"]="首页", ["ja"]="ホーム", ["th"]="หน้าหลัก", ["fr"]="Accueil" },
        ["Map"] = new() { ["vi"]="Bản đồ", ["en"]="Map", ["ko"]="지도", ["zh"]="地图", ["ja"]="地図", ["th"]="แผนที่", ["fr"]="Carte" },
        ["Places"] = new() { ["vi"]="Địa điểm", ["en"]="Places", ["ko"]="장소", ["zh"]="地点", ["ja"]="スポット", ["th"]="สถานที่", ["fr"]="Lieux" },
        ["Settings"] = new() { ["vi"]="Cài đặt", ["en"]="Settings", ["ko"]="설정", ["zh"]="设置", ["ja"]="設定", ["th"]="ตั้งค่า", ["fr"]="Paramètres" },

        // ── Tour ──
        ["ChooseTour"] = new() { ["vi"]="Chọn Tour", ["en"]="Choose Tour", ["ko"]="투어 선택", ["zh"]="选择路线", ["ja"]="ツアー選択", ["th"]="เลือกทัวร์", ["fr"]="Choisir un tour" },
        ["CreateNewTour"] = new() { ["vi"]="Tạo Tour mới", ["en"]="Create New Tour", ["ko"]="새 투어 만들기", ["zh"]="创建新路线", ["ja"]="新しいツアーを作成", ["th"]="สร้างทัวร์ใหม่", ["fr"]="Créer un tour" },
        ["RecentTours"] = new() { ["vi"]="Tour gần đây", ["en"]="Recent Tours", ["ko"]="최근 투어", ["zh"]="最近路线", ["ja"]="最近のツアー", ["th"]="ทัวร์ล่าสุด", ["fr"]="Tours récents" },
        ["CreateTour"] = new() { ["vi"]="Tạo Tour", ["en"]="Create Tour", ["ko"]="투어 만들기", ["zh"]="创建路线", ["ja"]="ツアー作成", ["th"]="สร้างทัวร์", ["fr"]="Créer un tour" },
        ["CreateTourComingSoon"] = new() { ["vi"]="Tính năng tạo tour mới sẽ sớm có mặt!", ["en"]="New tour creation coming soon!", ["ko"]="새 투어 만들기 기능이 곧 출시됩니다!", ["zh"]="创建新路线功能即将推出！", ["ja"]="新しいツアー作成機能は近日公開予定です！", ["th"]="ฟีเจอร์สร้างทัวร์ใหม่เร็วๆ นี้!", ["fr"]="Création de tour bientôt disponible !" },
        ["StartTour"] = new() { ["vi"]="Bắt đầu Tour", ["en"]="Start Tour", ["ko"]="투어 시작", ["zh"]="开始路线", ["ja"]="ツアー開始", ["th"]="เริ่มทัวร์", ["fr"]="Démarrer le tour" },
        ["TourModeBanner"] = new() { ["vi"]="Đang xem theo tour", ["en"]="Tour mode", ["ko"]="투어 모드", ["zh"]="路线模式", ["ja"]="ツアーモード", ["th"]="โหมดทัวร์", ["fr"]="Mode tour" },
        ["ShowAllPlaces"] = new() { ["vi"]="Hiện tất cả địa điểm", ["en"]="Show all places", ["ko"]="모든 장소 표시", ["zh"]="显示全部地点", ["ja"]="すべて表示", ["th"]="แสดงทุกสถานที่", ["fr"]="Tout afficher" },
        ["POIList"] = new() { ["vi"]="Danh sách quán ăn", ["en"]="Restaurant list", ["ko"]="음식점 목록", ["zh"]="餐厅列表", ["ja"]="レストランリスト", ["th"]="รายการร้านอาหาร", ["fr"]="Liste des restaurants" },

        // ── Units ──
        ["Minutes"] = new() { ["vi"]="phút", ["en"]="min", ["ko"]="분", ["zh"]="分钟", ["ja"]="分", ["th"]="นาที", ["fr"]="min" },
        ["Points"] = new() { ["vi"]="điểm", ["en"]="points", ["ko"]="곳", ["zh"]="个点", ["ja"]="ヶ所", ["th"]="จุด", ["fr"]="points" },
        ["Shops"] = new() { ["vi"]="quán", ["en"]="shops", ["ko"]="곳", ["zh"]="家店", ["ja"]="店", ["th"]="ร้าน", ["fr"]="restaurants" },

        // ── Search ──
        ["Search"] = new() { ["vi"]="Tìm kiếm...", ["en"]="Search...", ["ko"]="검색...", ["zh"]="搜索...", ["ja"]="検索...", ["th"]="ค้นหา...", ["fr"]="Rechercher..." },
        ["SearchPlaces"] = new() { ["vi"]="Tìm kiếm địa điểm...", ["en"]="Search places...", ["ko"]="장소 검색...", ["zh"]="搜索地点...", ["ja"]="スポットを検索...", ["th"]="ค้นหาสถานที่...", ["fr"]="Rechercher des lieux..." },
        ["NearbyPlaces"] = new() { ["vi"]="Địa điểm gần bạn", ["en"]="Nearby places", ["ko"]="주변 장소", ["zh"]="附近地点", ["ja"]="近くのスポット", ["th"]="สถานที่ใกล้คุณ", ["fr"]="Lieux à proximité" },
        ["NoPlacesFound"] = new() { ["vi"]="Không tìm thấy địa điểm", ["en"]="No places found", ["ko"]="장소를 찾을 수 없습니다", ["zh"]="未找到地点", ["ja"]="スポットが見つかりません", ["th"]="ไม่พบสถานที่", ["fr"]="Aucun lieu trouvé" },
        ["TryDifferentSearch"] = new() { ["vi"]="Thử tìm với từ khóa khác hoặc kéo xuống để đồng bộ", ["en"]="Try a different keyword or pull to sync", ["ko"]="다른 키워드로 검색하거나 당겨서 동기화하세요", ["zh"]="尝试其他关键词或下拉同步", ["ja"]="別のキーワードで検索するか、下にスワイプして同期", ["th"]="ลองค้นหาคำอื่นหรือดึงลงเพื่อซิงค์", ["fr"]="Essayez un autre mot-clé ou tirez pour synchroniser" },

        // ── Settings page ──
        ["SettingsTitle"] = new() { ["vi"]="Cài đặt", ["en"]="Settings", ["ko"]="설정", ["zh"]="设置", ["ja"]="設定", ["th"]="ตั้งค่า", ["fr"]="Paramètres" },
        ["SettingsSubtitle"] = new() { ["vi"]="Tùy chỉnh ứng dụng của bạn", ["en"]="Customize your app", ["ko"]="앱을 사용자 정의하세요", ["zh"]="自定义您的应用", ["ja"]="アプリをカスタマイズ", ["th"]="ปรับแต่งแอปของคุณ", ["fr"]="Personnalisez votre application" },
        ["Narration"] = new() { ["vi"]="Thuyết minh", ["en"]="Narration", ["ko"]="해설", ["zh"]="解说", ["ja"]="ナレーション", ["th"]="บรรยาย", ["fr"]="Narration" },
        ["NarrationLanguage"] = new() { ["vi"]="Ngôn ngữ thuyết minh", ["en"]="Narration language", ["ko"]="해설 언어", ["zh"]="解说语言", ["ja"]="ナレーション言語", ["th"]="ภาษาบรรยาย", ["fr"]="Langue de narration" },
        ["NarrationLanguageHint"] = new() { ["vi"]="Chọn ngôn ngữ cho audio", ["en"]="Select language for audio", ["ko"]="오디오 언어 선택", ["zh"]="选择音频语言", ["ja"]="音声の言語を選択", ["th"]="เลือกภาษาสำหรับเสียง", ["fr"]="Sélectionner la langue audio" },
        ["VoiceNarration"] = new() { ["vi"]="Giọng thuyết minh", ["en"]="Narration voice", ["ko"]="해설 목소리", ["zh"]="解说声音", ["ja"]="ナレーション音声", ["th"]="เสียงบรรยาย", ["fr"]="Voix de narration" },
        ["VoiceGender"] = new() { ["vi"]="Giọng đọc", ["en"]="Voice gender", ["ko"]="음성 성별", ["zh"]="语音性别", ["ja"]="音声の性別", ["th"]="เสียงอ่าน", ["fr"]="Genre de voix" },
        ["VoiceGenderHint"] = new() { ["vi"]="Chọn giọng Nam hoặc Nữ", ["en"]="Select Male or Female", ["ko"]="남성 또는 여성 선택", ["zh"]="选择男声或女声", ["ja"]="男性または女性を選択", ["th"]="เลือกเสียงชายหรือหญิง", ["fr"]="Sélectionner Homme ou Femme" },
        ["Male"] = new() { ["vi"]="Nam", ["en"]="Male", ["ko"]="남성", ["zh"]="男声", ["ja"]="男性", ["th"]="ชาย", ["fr"]="Homme" },
        ["Female"] = new() { ["vi"]="Nữ", ["en"]="Female", ["ko"]="여성", ["zh"]="女声", ["ja"]="女性", ["th"]="หญิง", ["fr"]="Femme" },
        ["Location"] = new() { ["vi"]="Vị trí", ["en"]="Location", ["ko"]="위치", ["zh"]="位置", ["ja"]="位置情報", ["th"]="ตำแหน่ง", ["fr"]="Position" },
        ["GpsStatus"] = new() { ["vi"]="Trạng thái GPS", ["en"]="GPS Status", ["ko"]="GPS 상태", ["zh"]="GPS 状态", ["ja"]="GPS状態", ["th"]="สถานะ GPS", ["fr"]="État du GPS" },
        ["LocationActive"] = new() { ["vi"]="Vị trí đang hoạt động", ["en"]="Location is active", ["ko"]="위치 활성화됨", ["zh"]="位置已开启", ["ja"]="位置情報はオン", ["th"]="ตำแหน่งเปิดอยู่", ["fr"]="Position activée" },
        ["LocationOff"] = new() { ["vi"]="Vị trí bị tắt", ["en"]="Location is off", ["ko"]="위치 비활성화", ["zh"]="位置已关闭", ["ja"]="位置情報はオフ", ["th"]="ตำแหน่งปิดอยู่", ["fr"]="Position désactivée" },
        ["Info"] = new() { ["vi"]="Thông tin", ["en"]="Info", ["ko"]="정보", ["zh"]="信息", ["ja"]="情報", ["th"]="ข้อมูล", ["fr"]="Informations" },
        ["Version"] = new() { ["vi"]="Phiên bản", ["en"]="Version", ["ko"]="버전", ["zh"]="版本", ["ja"]="バージョン", ["th"]="เวอร์ชัน", ["fr"]="Version" },
        ["ContactSupport"] = new() { ["vi"]="Liên hệ hỗ trợ", ["en"]="Contact support", ["ko"]="고객 지원", ["zh"]="联系支持", ["ja"]="サポートに連絡", ["th"]="ติดต่อฝ่ายสนับสนุน", ["fr"]="Contacter le support" },
        ["SyncData"] = new() { ["vi"]="Đồng bộ dữ liệu", ["en"]="Sync data", ["ko"]="데이터 동기화", ["zh"]="同步数据", ["ja"]="データ同期", ["th"]="ซิงค์ข้อมูล", ["fr"]="Synchroniser les données" },
        ["Refresh"] = new() { ["vi"]="Làm mới", ["en"]="Refresh", ["ko"]="새로고침", ["zh"]="刷新", ["ja"]="更新", ["th"]="รีเฟรช", ["fr"]="Actualiser" },
        ["Account"] = new() { ["vi"]="Tài khoản", ["en"]="Account", ["ko"]="계정", ["zh"]="账户", ["ja"]="アカウント", ["th"]="บัญชี", ["fr"]="Compte" },
        ["Logout"] = new() { ["vi"]="Đăng xuất", ["en"]="Log out", ["ko"]="로그아웃", ["zh"]="退出登录", ["ja"]="ログアウト", ["th"]="ออกจากระบบ", ["fr"]="Déconnexion" },
        ["LoggedIn"] = new() { ["vi"]="Đã đăng nhập", ["en"]="Logged in", ["ko"]="로그인됨", ["zh"]="已登录", ["ja"]="ログイン済み", ["th"]="เข้าสู่ระบบแล้ว", ["fr"]="Connecté" },
        ["RegisterSuccessMessage"] = new() { ["vi"]="Đăng ký thành công. Vui lòng đăng nhập.", ["en"]="Registration successful. Please log in.", ["ko"]="가입이 완료되었습니다. 로그인해 주세요.", ["zh"]="注册成功，请登录。", ["ja"]="登録完了。ログインしてください。", ["th"]="ลงทะเบียนสำเร็จ กรุณาเข้าสู่ระบบ", ["fr"]="Inscription réussie. Veuillez vous connecter." },

        // ── Status & Errors ──
        ["On"] = new() { ["vi"]="Bật", ["en"]="ON", ["ko"]="켜짐", ["zh"]="开", ["ja"]="オン", ["th"]="เปิด", ["fr"]="Activé" },
        ["Off"] = new() { ["vi"]="Tắt", ["en"]="OFF", ["ko"]="꺼짐", ["zh"]="关", ["ja"]="オフ", ["th"]="ปิด", ["fr"]="Désactivé" },
        ["Success"] = new() { ["vi"]="Thành công", ["en"]="Success", ["ko"]="성공", ["zh"]="成功", ["ja"]="成功", ["th"]="สำเร็จ", ["fr"]="Succès" },
        ["SyncSuccess"] = new() { ["vi"]="Đã đồng bộ dữ liệu thành công!", ["en"]="Data synced successfully!", ["ko"]="데이터가 성공적으로 동기화되었습니다!", ["zh"]="数据同步成功！", ["ja"]="データの同期に成功しました！", ["th"]="ซิงค์ข้อมูลสำเร็จ!", ["fr"]="Données synchronisées avec succès !" },
        ["Error"] = new() { ["vi"]="Lỗi", ["en"]="Error", ["ko"]="오류", ["zh"]="错误", ["ja"]="エラー", ["th"]="ข้อผิดพลาด", ["fr"]="Erreur" },
        ["SyncError"] = new() { ["vi"]="Không thể đồng bộ dữ liệu", ["en"]="Could not sync data", ["ko"]="데이터를 동기화할 수 없습니다", ["zh"]="无法同步数据", ["ja"]="データを同期できませんでした", ["th"]="ไม่สามารถซิงค์ข้อมูลได้", ["fr"]="Impossible de synchroniser les données" },
        ["MapError"] = new() { ["vi"]="Không thể mở ứng dụng bản đồ", ["en"]="Could not open map app", ["ko"]="지도 앱을 열 수 없습니다", ["zh"]="无法打开地图应用", ["ja"]="マップアプリを開けませんでした", ["th"]="ไม่สามารถเปิดแอปแผนที่ได้", ["fr"]="Impossible d'ouvrir l'application de carte" },
        ["GettingLocation"] = new() { ["vi"]="Đang lấy vị trí...", ["en"]="Getting location...", ["ko"]="위치 확인 중...", ["zh"]="正在获取位置...", ["ja"]="位置情報を取得中...", ["th"]="กำลังรับตำแหน่ง...", ["fr"]="Localisation en cours..." },
        ["GpsNotEnabled"] = new() { ["vi"]="GPS chưa bật. Bật vị trí trong Cài đặt để dùng geofence.", ["en"]="GPS is off. Enable location in Settings for geofence.", ["ko"]="GPS가 꺼져 있습니다. 지오펜스를 사용하려면 설정에서 위치를 켜세요.", ["zh"]="GPS未开启。请在设置中启用位置以使用地理围栏。", ["ja"]="GPSがオフです。ジオフェンスを使用するには設定で位置情報をオンにしてください。", ["th"]="GPS ปิดอยู่ กรุณาเปิดตำแหน่งในตั้งค่าเพื่อใช้ geofence", ["fr"]="GPS désactivé. Activez la localisation dans les paramètres pour le geofence." },
        ["Checking"] = new() { ["vi"]="Đang kiểm tra...", ["en"]="Checking...", ["ko"]="확인 중...", ["zh"]="检查中...", ["ja"]="確認中...", ["th"]="กำลังตรวจสอบ...", ["fr"]="Vérification..." },
        ["CannotOpenMap"] = new() { ["vi"]="Không thể mở bản đồ. Vui lòng thử lại.", ["en"]="Cannot open map. Please try again.", ["ko"]="지도를 열 수 없습니다. 다시 시도해주세요.", ["zh"]="无法打开地图，请重试。", ["ja"]="地図を開けません。もう一度お試しください。", ["th"]="ไม่สามารถเปิดแผนที่ได้ กรุณาลองอีกครั้ง", ["fr"]="Impossible d'ouvrir la carte. Veuillez réessayer." },

        // ── POI Detail ──
        ["Address"] = new() { ["vi"]="Địa chỉ", ["en"]="Address", ["ko"]="주소", ["zh"]="地址", ["ja"]="住所", ["th"]="ที่อยู่", ["fr"]="Adresse" },
        ["FoodType"] = new() { ["vi"]="Loại món", ["en"]="Food type", ["ko"]="음식 종류", ["zh"]="菜品类型", ["ja"]="料理の種類", ["th"]="ประเภทอาหาร", ["fr"]="Type de cuisine" },
        ["Price"] = new() { ["vi"]="Giá", ["en"]="Price", ["ko"]="가격", ["zh"]="价格", ["ja"]="価格", ["th"]="ราคา", ["fr"]="Prix" },
        ["VisitTime"] = new() { ["vi"]="Thời gian tham quan", ["en"]="Visit time", ["ko"]="방문 시간", ["zh"]="参观时间", ["ja"]="所要時間", ["th"]="เวลาเที่ยวชม", ["fr"]="Durée de visite" },
        ["Description"] = new() { ["vi"]="Thông tin", ["en"]="Description", ["ko"]="정보", ["zh"]="信息", ["ja"]="情報", ["th"]="ข้อมูล", ["fr"]="Description" },
        ["ListenNarration"] = new() { ["vi"]="Nghe thuyết minh", ["en"]="Listen", ["ko"]="듣기", ["zh"]="收听解说", ["ja"]="聴く", ["th"]="ฟังบรรยาย", ["fr"]="Écouter" },
        ["GetDirections"] = new() { ["vi"]="Chỉ đường", ["en"]="Directions", ["ko"]="길찾기", ["zh"]="导航", ["ja"]="道案内", ["th"]="นำทาง", ["fr"]="Itinéraire" },

        // ── POI Categories ──
        ["CatAll"] = new() { ["vi"]="Tất cả", ["en"]="All", ["ko"]="전체", ["zh"]="全部", ["ja"]="すべて", ["th"]="ทั้งหมด", ["fr"]="Tous" },
        ["CatSightseeing"] = new() { ["vi"]="Tham quan", ["en"]="Sightseeing", ["ko"]="관광", ["zh"]="观光", ["ja"]="観光", ["th"]="ท่องเที่ยว", ["fr"]="Tourisme" },
        ["CatFood"] = new() { ["vi"]="Ẩm thực", ["en"]="Food", ["ko"]="음식", ["zh"]="美食", ["ja"]="グルメ", ["th"]="อาหาร", ["fr"]="Gastronomie" },
        ["CatAccommodation"] = new() { ["vi"]="Nghỉ dưỡng", ["en"]="Accommodation", ["ko"]="숙소", ["zh"]="住宿", ["ja"]="宿泊", ["th"]="ที่พัก", ["fr"]="Hébergement" },
        ["CatShopping"] = new() { ["vi"]="Mua sắm", ["en"]="Shopping", ["ko"]="쇼핑", ["zh"]="购物", ["ja"]="ショッピング", ["th"]="ช้อปปิ้ง", ["fr"]="Shopping" },
        ["CatEntertainment"] = new() { ["vi"]="Giải trí", ["en"]="Entertainment", ["ko"]="엔터테인먼트", ["zh"]="娱乐", ["ja"]="エンタメ", ["th"]="บันเทิง", ["fr"]="Divertissement" },
        ["CatHistorical"] = new() { ["vi"]="Di tích", ["en"]="Historical", ["ko"]="유적", ["zh"]="古迹", ["ja"]="史跡", ["th"]="โบราณสถาน", ["fr"]="Historique" },
        ["CatNature"] = new() { ["vi"]="Thiên nhiên", ["en"]="Nature", ["ko"]="자연", ["zh"]="自然", ["ja"]="自然", ["th"]="ธรรมชาติ", ["fr"]="Nature" },

        // ── Food Types ──
        ["FoodSeafood"] = new() { ["vi"]="Hải sản", ["en"]="Seafood", ["ko"]="해산물", ["zh"]="海鲜", ["ja"]="シーフード", ["th"]="อาหารทะเล", ["fr"]="Fruits de mer" },
        ["FoodVegetarian"] = new() { ["vi"]="Món chay", ["en"]="Vegetarian", ["ko"]="채식", ["zh"]="素食", ["ja"]="ベジタリアン", ["th"]="อาหารเจ", ["fr"]="Végétarien" },
        ["FoodSpecialty"] = new() { ["vi"]="Đặc sản", ["en"]="Specialty", ["ko"]="특산물", ["zh"]="特产", ["ja"]="名物", ["th"]="อาหารพิเศษ", ["fr"]="Spécialité" },
        ["FoodStreet"] = new() { ["vi"]="Ẩm thực đường phố", ["en"]="Street food", ["ko"]="길거리 음식", ["zh"]="街头小吃", ["ja"]="ストリートフード", ["th"]="อาหารริมทาง", ["fr"]="Cuisine de rue" },
        ["FoodGrilled"] = new() { ["vi"]="Nướng", ["en"]="Grilled", ["ko"]="구이", ["zh"]="烧烤", ["ja"]="焼き料理", ["th"]="ย่าง", ["fr"]="Grillé" },
        ["FoodNoodles"] = new() { ["vi"]="Bún/Phở/Mì", ["en"]="Noodles", ["ko"]="면 요리", ["zh"]="粉面", ["ja"]="麺類", ["th"]="ก๋วยเตี๋ยว", ["fr"]="Nouilles" },

        // ── Tour Generator ──
        ["TourTopRated"] = new() { ["vi"]="Quán ăn được yêu thích nhất", ["en"]="Top Rated Restaurants", ["ko"]="인기 맛집", ["zh"]="最受欢迎的餐厅", ["ja"]="人気レストラン", ["th"]="ร้านอาหารยอดนิยม", ["fr"]="Restaurants les mieux notés" },
        ["TourTopRatedDesc"] = new() { ["vi"]="Những địa điểm ẩm thực được đánh giá cao nhất bởi du khách", ["en"]="Highest rated food places by tourists", ["ko"]="관광객들이 가장 높이 평가한 맛집", ["zh"]="游客评价最高的美食地点", ["ja"]="観光客に最も評価の高いグルメスポット", ["th"]="ร้านอาหารที่นักท่องเที่ยวให้คะแนนสูงสุด", ["fr"]="Les restaurants les mieux notés par les touristes" },
        ["TourQuickEat"] = new() { ["vi"]="Ăn nhanh - Tiết kiệm thời gian", ["en"]="Quick Bites - Save Time", ["ko"]="빠른 식사 - 시간 절약", ["zh"]="快餐 - 节省时间", ["ja"]="サクッとグルメ - 時短", ["th"]="ทานเร็ว - ประหยัดเวลา", ["fr"]="Repas rapides - Gain de temps" },
        ["TourQuickEatDesc"] = new() { ["vi"]="Những quán ăn phục vụ nhanh, phù hợp cho lịch trình gấp", ["en"]="Quick-service restaurants for tight schedules", ["ko"]="빠른 서비스 음식점, 바쁜 일정에 적합", ["zh"]="快速服务餐厅，适合紧凑行程", ["ja"]="忙しいスケジュールに最適なクイックサービスレストラン", ["th"]="ร้านอาหารเสิร์ฟเร็ว เหมาะสำหรับตารางเวลาที่แน่น", ["fr"]="Restaurants service rapide pour les emplois du temps serrés" },
        ["TourBudget"] = new() { ["vi"]="Ăn ngon - Giá rẻ", ["en"]="Delicious & Budget", ["ko"]="맛있고 저렴한", ["zh"]="美味又实惠", ["ja"]="美味しくてリーズナブル", ["th"]="อร่อยและประหยัด", ["fr"]="Bon et pas cher" },
        ["TourBudgetDesc"] = new() { ["vi"]="Các quán ăn bình dân, giá dưới 50,000đ", ["en"]="Affordable restaurants, under 50,000 VND", ["ko"]="50,000동 이하 저렴한 맛집", ["zh"]="人均5万越南盾以下的平价餐厅", ["ja"]="5万ドン以下のお手頃レストラン", ["th"]="ร้านอาหารราคาประหยัด ต่ำกว่า 50,000 ด่ง", ["fr"]="Restaurants abordables, moins de 50 000 VND" },

        // ── Main / Home screen ──
        ["DistrictHoThiKy"] = new() { ["vi"]="Phố Hồ Thị Kỷ", ["en"]="Ho Thi Ky Street", ["ko"]="호티키 거리", ["zh"]="胡氏纪街", ["ja"]="ホティキー通り", ["th"]="ถนนโฮถีกี", ["fr"]="Rue Ho Thi Ky" },
        ["SearchFoodHint"] = new() { ["vi"]="Tìm quán ăn, món ăn...", ["en"]="Search restaurants, dishes...", ["ko"]="맛집·메뉴 검색...", ["zh"]="搜索餐厅、菜品…", ["ja"]="店・料理を検索…", ["th"]="ค้นหาร้านอาหาร อาหาร...", ["fr"]="Rechercher restaurants, plats…" },
        ["TourTodaySection"] = new() { ["vi"]="Tour hôm nay", ["en"]="Today's tour", ["ko"]="오늘의 투어", ["zh"]="今日路线", ["ja"]="今日のツアー", ["th"]="ทัวร์วันนี้", ["fr"]="Tour du jour" },
        ["FeaturedSpotsSection"] = new() { ["vi"]="Điểm nổi bật gần đây", ["en"]="Featured nearby", ["ko"]="주변 추천", ["zh"]="附近精选", ["ja"]="近くのおすすめ", ["th"]="แนะนำใกล้คุณ", ["fr"]="À découvrir près d’ici" },
        ["SeeAll"] = new() { ["vi"]="Xem tất cả →", ["en"]="See all →", ["ko"]="전체 보기 →", ["zh"]="查看全部 →", ["ja"]="すべて見る →", ["th"]="ดูทั้งหมด →", ["fr"]="Tout voir →" },
        ["LoadingTours"] = new() { ["vi"]="Đang tải tour...", ["en"]="Loading tours...", ["ko"]="투어 불러오는 중...", ["zh"]="正在加载路线…", ["ja"]="ツアーを読み込み中…", ["th"]="กำลังโหลดทัวร์...", ["fr"]="Chargement des tours…" },
        ["NoToursYet"] = new() { ["vi"]="Chưa có tour nào. Nhấn Làm mới để tải.", ["en"]="No tours yet. Tap Refresh to load.", ["ko"]="투어가 없습니다. 새로고침을 누르세요.", ["zh"]="暂无路线，请点刷新加载。", ["ja"]="ツアーがありません。更新をタップ。", ["th"]="ยังไม่มีทัวร์ กดรีเฟรชเพื่อโหลด", ["fr"]="Aucun tour. Appuyez sur Actualiser." },
        ["HelloGreeting"] = new() { ["vi"]="Xin chào 👋", ["en"]="Hello 👋", ["ko"]="안녕하세요 👋", ["zh"]="你好 👋", ["ja"]="こんにちは 👋", ["th"]="สวัสดี 👋", ["fr"]="Bonjour 👋" },

        // ── POI Detail extra ──
        ["OpenNow"] = new() { ["vi"]="Đang mở", ["en"]="Open", ["ko"]="영업 중", ["zh"]="营业中", ["ja"]="営業中", ["th"]="เปิดอยู่", ["fr"]="Ouvert" },
        ["AiNarrationTitle"] = new() { ["vi"]="AI THUYẾT MINH", ["en"]="AI NARRATION", ["ko"]="AI 해설", ["zh"]="AI 解说", ["ja"]="AIナレーション", ["th"]="AI บรรยาย", ["fr"]="NARRATION IA" },
        ["ReviewCountFormat"] = new() { ["vi"]="({0} đánh giá)", ["en"]="({0} reviews)", ["ko"]="(리뷰 {0}개)", ["zh"]="（{0} 条评价）", ["ja"]="（レビュー{0}件）", ["th"]="({0} รีวิว)", ["fr"]="({0} avis)" },
        ["IntroTitle"] = new() { ["vi"]="Giới thiệu", ["en"]="About", ["ko"]="소개", ["zh"]="简介", ["ja"]="紹介", ["th"]="แนะนำ", ["fr"]="Présentation" },

        // ── Smart tour by food type (titles + descriptions) ──
        ["TourGen_SeafoodName"] = new() { ["vi"]="Tour Hải sản", ["en"]="Seafood Tour", ["ko"]="해산물 투어", ["zh"]="海鲜之旅", ["ja"]="シーフードツアー", ["th"]="ทัวร์อาหารทะเล", ["fr"]="Tour fruits de mer" },
        ["TourGen_SeafoodDesc"] = new() { ["vi"]="Khám phá hương vị biển cả với các món hải sản tươi ngon", ["en"]="Discover ocean flavors with fresh seafood", ["ko"]="신선한 해산물로 바다의 맛을 만나보세요", ["zh"]="用新鲜海鲜探索海洋风味", ["ja"]="新鮮なシーフードで海の味を", ["th"]="อาหารทะเลสดจากท้องทะเล", ["fr"]="Saveurs de la mer, fruits de mer frais" },
        ["TourGen_VegetarianName"] = new() { ["vi"]="Tour Món chay", ["en"]="Vegetarian Tour", ["ko"]="채식 투어", ["zh"]="素食之旅", ["ja"]="ベジタリアンツアー", ["th"]="ทัวร์มังสวิรัติ", ["fr"]="Tour végétarien" },
        ["TourGen_VegetarianDesc"] = new() { ["vi"]="Thưởng thức ẩm thực chay thanh đạm, tốt cho sức khỏe", ["en"]="Enjoy wholesome vegetarian cuisine", ["ko"]="건강한 채식 요리를 즐기세요", ["zh"]="健康清爽的素食", ["ja"]="ヘルシーな精進料理", ["th"]="อาหารเจเพื่อสุขภาพ", ["fr"]="Cuisine végétarienne équilibrée" },
        ["TourGen_VietnameseName"] = new() { ["vi"]="Tour Đặc sản Việt", ["en"]="Vietnamese Specialties Tour", ["ko"]="베트남 특산 투어", ["zh"]="越南特产之旅", ["ja"]="ベトナム名物ツアー", ["th"]="ทัวร์อาหารพิเศษเวียดนาม", ["fr"]="Tour spécialités vietnamiennes" },
        ["TourGen_VietnameseDesc"] = new() { ["vi"]="Trải nghiệm đặc sản Việt Nam đậm đà bản sắc", ["en"]="Authentic Vietnamese flavors", ["ko"]="정통 베트남 맛", ["zh"]="正宗越南风味", ["ja"]="本格的なベトナムの味", ["th"]="รสชาติเวียดนามแท้", ["fr"]="Saveurs vietnamiennes authentiques" },
        ["TourGen_StreetName"] = new() { ["vi"]="Tour Ẩm thực đường phố", ["en"]="Street Food Tour", ["ko"]="길거리 음식 투어", ["zh"]="街头美食之旅", ["ja"]="ストリートフードツアー", ["th"]="ทัวร์อาหารริมทาง", ["fr"]="Tour street food" },
        ["TourGen_StreetDesc"] = new() { ["vi"]="Khám phá ẩm thực đường phố đầy hấp dẫn", ["en"]="Explore vibrant street food", ["ko"]="활기찬 길거리 음식", ["zh"]="探索街头烟火气", ["ja"]="活気あるストリートフード", ["th"]="สำรวจอาหารริมทาง", ["fr"]="Street food animée" },
        ["TourGen_BBQName"] = new() { ["vi"]="Tour Đồ nướng", ["en"]="BBQ & Grill Tour", ["ko"]="바비큐 투어", ["zh"]="烧烤之旅", ["ja"]="焼き・BBQツアー", ["th"]="ทัวร์ปิ้งย่าง", ["fr"]="Tour grillades" },
        ["TourGen_BBQDesc"] = new() { ["vi"]="Thưởng thức các món nướng thơm ngon, hấp dẫn", ["en"]="Sizzling grilled dishes", ["ko"]="갓 구운 요리", ["zh"]="喷香烧烤", ["ja"]="香ばしい焼き料理", ["th"]="เมนูย่างหอมกรุ่น", ["fr"]="Plats grillés savoureux" },
        ["TourGen_NoodlesName"] = new() { ["vi"]="Tour Bún Phở Mì", ["en"]="Noodles Tour", ["ko"]="쌀국수·면 투어", ["zh"]="河粉面食之旅", ["ja"]="フォー・麺ツアー", ["th"]="ทัวร์ก๋วยเตี๋ยว", ["fr"]="Tour nouilles" },
        ["TourGen_NoodlesDesc"] = new() { ["vi"]="Khám phá thế giới bún, phở, mì đa dạng", ["en"]="Noodles, pho, and more", ["ko"]="쌀국수와 면 요리의 세계", ["zh"]="河粉、面类大千世界", ["ja"]="フォーと麺の世界", ["th"]="โลกของก๋วยเตี๋ยว", ["fr"]="Pho, nouilles et cie." },
        ["TourGen_DefaultName"] = new() { ["vi"]="Tour Ẩm thực", ["en"]="Food Tour", ["ko"]="맛집 투어", ["zh"]="美食之旅", ["ja"]="グルメツアー", ["th"]="ทัวร์อาหาร", ["fr"]="Tour gastronomique" },
        ["TourGen_DefaultDesc"] = new() { ["vi"]="Khám phá ẩm thực đa dạng và phong phú", ["en"]="Diverse local flavors", ["ko"]="다양한 로컬 맛", ["zh"]="丰富多样的本地风味", ["ja"]="多彩なローカルグルメ", ["th"]="หลากหลายรสท้องถิ่น", ["fr"]="Saveurs locales variées" },
        ["TourMidRangeName"] = new() { ["vi"]="Ăn vừa túi tiền", ["en"]="Mid-range dining", ["ko"]="가성비 맛집", ["zh"]="性价比之选", ["ja"]="お手頃グルメ", ["th"]="ราคากลางคุณภาพดี", ["fr"]="Bon rapport qualité-prix" },
        ["TourMidRangeDesc"] = new() { ["vi"]="Các nhà hàng tầm trung, giá 50k-150k", ["en"]="Restaurants around 50k–150k VND", ["ko"]="5만~15만 동대 레스토랑", ["zh"]="人均约5–15万越南盾的餐厅", ["ja"]="5〜15万ドン帯のレストラン", ["th"]="ร้านราคากลาง 50k–150k", ["fr"]="Restaurants milieu de gamme 50k–150k" },
        ["TourPremiumName"] = new() { ["vi"]="Nhà hàng cao cấp", ["en"]="Fine dining", ["ko"]="파인다이닝", ["zh"]="高端餐厅", ["ja"]="高級レストラン", ["th"]="ร้านหรู", ["fr"]="Gastronomie haut de gamme" },
        ["TourPremiumDesc"] = new() { ["vi"]="Trải nghiệm ẩm thực sang trọng, giá trên 150k", ["en"]="Premium experience, from 150k VND", ["ko"]="15만 동 이상 프리미엄", ["zh"]="15万盾以上的精致体验", ["ja"]="15万ドン以上の上質体験", ["th"]="ประสบการณ์พรีเมียม ตั้งแต่ 150k", ["fr"]="Expérience premium dès 150k" },

        // ── Language names ──
        ["LanguageVi"] = new() { ["vi"]="Tiếng Việt", ["en"]="Vietnamese", ["ko"]="베트남어", ["zh"]="越南语", ["ja"]="ベトナム語", ["th"]="ภาษาเวียดนาม", ["fr"]="Vietnamien" },
        ["LanguageEn"] = new() { ["vi"]="English", ["en"]="English", ["ko"]="영어", ["zh"]="英语", ["ja"]="英語", ["th"]="ภาษาอังกฤษ", ["fr"]="Anglais" },
        ["LanguageKo"] = new() { ["vi"]="한국어", ["en"]="Korean", ["ko"]="한국어", ["zh"]="韩语", ["ja"]="韓国語", ["th"]="ภาษาเกาหลี", ["fr"]="Coréen" },
        ["LanguageZh"] = new() { ["vi"]="中文", ["en"]="Chinese", ["ko"]="중국어", ["zh"]="中文", ["ja"]="中国語", ["th"]="ภาษาจีน", ["fr"]="Chinois" },
        ["LanguageJa"] = new() { ["vi"]="日本語", ["en"]="Japanese", ["ko"]="일본어", ["zh"]="日语", ["ja"]="日本語", ["th"]="ภาษาญี่ปุ่น", ["fr"]="Japonais" },
        ["LanguageTh"] = new() { ["vi"]="ภาษาไทย", ["en"]="Thai", ["ko"]="태국어", ["zh"]="泰语", ["ja"]="タイ語", ["th"]="ภาษาไทย", ["fr"]="Thaï" },
        ["LanguageFr"] = new() { ["vi"]="Français", ["en"]="French", ["ko"]="프랑스어", ["zh"]="法语", ["ja"]="フランス語", ["th"]="ภาษาฝรั่งเศส", ["fr"]="Français" },

        // ── Region ──
        ["RegionNorth"] = new() { ["vi"]="Miền Bắc", ["en"]="North", ["ko"]="북부", ["zh"]="北部", ["ja"]="北部", ["th"]="ภาคเหนือ", ["fr"]="Nord" },
        ["RegionCentral"] = new() { ["vi"]="Miền Trung", ["en"]="Central", ["ko"]="중부", ["zh"]="中部", ["ja"]="中部", ["th"]="ภาคกลาง", ["fr"]="Centre" },
        ["RegionSouth"] = new() { ["vi"]="Miền Nam", ["en"]="South", ["ko"]="남부", ["zh"]="南部", ["ja"]="南部", ["th"]="ภาคใต้", ["fr"]="Sud" },

        // ── Settings Profile section ──
        ["ProfileTitle"] = new() { ["vi"]="HỒ SƠ", ["en"]="PROFILE", ["ko"]="프로필", ["zh"]="个人资料", ["ja"]="プロフィール", ["th"]="โปรไฟล์", ["fr"]="PROFIL" },
        ["GuestName"] = new() { ["vi"]="Khách", ["en"]="Guest", ["ko"]="게스트", ["zh"]="游客", ["ja"]="ゲスト", ["th"]="ผู้เยี่ยมชม", ["fr"]="Invité" },
        ["GuestTitle"] = new() { ["vi"]="Thám tử ẩm thực · Cấp 5", ["en"]="Food Explorer · Level 5", ["ko"]="음식 탐험가 · 레벨 5", ["zh"]="美食探索者 · 等级5", ["ja"]="グルメ探偵 · レベル5", ["th"]="นักสำรวจอาหาร · ระดับ 5", ["fr"]="Explorateur culinaire · Niveau 5" },
        ["LevelProgress"] = new() { ["vi"]="Tiến độ Cấp 6", ["en"]="Level 6 Progress", ["ko"]="레벨 6 진행도", ["zh"]="等级6进度", ["ja"]="レベル6の進捗", ["th"]="ความคืบหน้าระดับ 6", ["fr"]="Progression Niveau 6" },
        ["BadgesEarned"] = new() { ["vi"]="HUY HIỆU ĐẠT ĐƯỢC", ["en"]="BADGES EARNED", ["ko"]="획득한 배지", ["zh"]="获得的徽章", ["ja"]="獲得したバッジ", ["th"]="ป้ายที่ได้รับ", ["fr"]="BADGES OBTENUS" },
        ["BadgeChampion"] = new() { ["vi"]="Nhà vô địch", ["en"]="Champion", ["ko"]="챔피언", ["zh"]="冠军", ["ja"]="チャンピオン", ["th"]="แชมเปี้ยน", ["fr"]="Champion" },
        ["BadgeExplorer"] = new() { ["vi"]="Người khám phá", ["en"]="Explorer", ["ko"]="탐험가", ["zh"]="探索者", ["ja"]="エクスプローラー", ["th"]="นักสำรวจ", ["fr"]="Explorateur" },
        ["BadgeLegend"] = new() { ["vi"]="Huyền thoại", ["en"]="Legend", ["ko"]="전설", ["zh"]="传奇", ["ja"]="レジェンド", ["th"]="ตำนาน", ["fr"]="Légende" },
        ["NarrationNotification"] = new() { ["vi"]="Thông báo thuyết minh", ["en"]="Narration Notifications", ["ko"]="해설 알림", ["zh"]="解说通知", ["ja"]="ナレーション通知", ["th"]="การแจ้งเตือนบรรยาย", ["fr"]="Notifications de narration" },
        ["VolumeVoice"] = new() { ["vi"]="Âm lượng & Giọng đọc", ["en"]="Volume & Voice", ["ko"]="볼륨 & 음성", ["zh"]="音量和音色", ["ja"]="音量と音声", ["th"]="ระดับเสียงและเสียงอ่าน", ["fr"]="Volume & Voix" },

        // ── Settings Stats section ──
        ["StatsSection"] = new() { ["vi"]="THỐNG KÊ", ["en"]="STATISTICS", ["ko"]="통계", ["zh"]="统计", ["ja"]="統計", ["th"]="สถิติ", ["fr"]="STATISTIQUES" },
        ["LblShopsVisited"] = new() { ["vi"]="Quán đã ghé", ["en"]="Shops Visited", ["ko"]="방문한 상점", ["zh"]="已访问店铺", ["ja"]="訪問した店", ["th"]="ร้านที่เยี่ยมชม", ["fr"]="Boutiques visitées" },
        ["LblDistance"] = new() { ["vi"]="Quãng đường", ["en"]="Distance", ["ko"]="이동 거리", ["zh"]="距离", ["ja"]="移動距離", ["th"]="ระยะทาง", ["fr"]="Distance" },
        ["LblToursCompleted"] = new() { ["vi"]="Tour hoàn thành", ["en"]="Tours Completed", ["ko"]="완료한 투어", ["zh"]="已完成路线", ["ja"]="完了したツアー", ["th"]="ทัวร์ที่เสร็จแล้ว", ["fr"]="Tours terminés" },
        ["LblNarrationCount"] = new() { ["vi"]="Nghe thuyết minh", ["en"]="Narrations", ["ko"]="해설 청취", ["zh"]="已听解说", ["ja"]="ナレーション再生", ["th"]="ฟังบรรยาย", ["fr"]="Narrations" },
        ["WeeklyActivityTitle"] = new() { ["vi"]="Hoạt động tuần này", ["en"]="This Week's Activity", ["ko"]="이번 주 활동", ["zh"]="本周活动", ["ja"]="今週の活動", ["th"]="กิจกรรมสัปดาห์นี้", ["fr"]="Activité de la semaine" },
        ["SevenDays"] = new() { ["vi"]="7 ngày", ["en"]="7 days", ["ko"]="7일", ["zh"]="7天", ["ja"]="7日間", ["th"]="7 วัน", ["fr"]="7 jours" },
        ["TopPlacesTitle"] = new() { ["vi"]="ĐIỂM ĐƯỢC YÊU THÍCH", ["en"]="FAVORITE PLACES", ["ko"]="즐겨찾는 장소", ["zh"]="最喜欢的地点", ["ja"]="お気に入りスポット", ["th"]="สถานที่โปรด", ["fr"]="LIEUX FAVORIS" },
        ["NoDataYet"] = new() { ["vi"]="Chưa có dữ liệu. Hãy ghé thăm các địa điểm!", ["en"]="No data yet. Visit some places!", ["ko"]="아직 데이터가 없습니다. 장소를 방문해 보세요!", ["zh"]="暂无数据，请去参观一些地点！", ["ja"]="データがありません。スポットを訪問してください！", ["th"]="ยังไม่มีข้อมูล ลองไปเยี่ยมชมสถานที่ดูสิ!", ["fr"]="Pas encore de données. Visitez des lieux !" },
        ["VisitCountFmt"] = new() { ["vi"]="{0} lần ghé thăm", ["en"]="{0} visits", ["ko"]="{0}번 방문", ["zh"]="访问 {0} 次", ["ja"]="{0}回訪問", ["th"]="เยี่ยมชม {0} ครั้ง", ["fr"]="{0} visites" },

        // ── Week day abbreviations ──
        ["DayMon"] = new() { ["vi"]="T2", ["en"]="Mon", ["ko"]="월", ["zh"]="周一", ["ja"]="月", ["th"]="จ", ["fr"]="Lun" },
        ["DayTue"] = new() { ["vi"]="T3", ["en"]="Tue", ["ko"]="화", ["zh"]="周二", ["ja"]="火", ["th"]="อ", ["fr"]="Mar" },
        ["DayWed"] = new() { ["vi"]="T4", ["en"]="Wed", ["ko"]="수", ["zh"]="周三", ["ja"]="水", ["th"]="พ", ["fr"]="Mer" },
        ["DayThu"] = new() { ["vi"]="T5", ["en"]="Thu", ["ko"]="목", ["zh"]="周四", ["ja"]="木", ["th"]="พฤ", ["fr"]="Jeu" },
        ["DayFri"] = new() { ["vi"]="T6", ["en"]="Fri", ["ko"]="금", ["zh"]="周五", ["ja"]="金", ["th"]="ศ", ["fr"]="Ven" },
        ["DaySat"] = new() { ["vi"]="T7", ["en"]="Sat", ["ko"]="토", ["zh"]="周六", ["ja"]="土", ["th"]="ส", ["fr"]="Sam" },
        ["DaySun"] = new() { ["vi"]="CN", ["en"]="Sun", ["ko"]="일", ["zh"]="周日", ["ja"]="日", ["th"]="อา", ["fr"]="Dim" },

        // ── Subscription / Payment ──
        ["SubHeroSubtitle"]    = new() { ["vi"]="Trải nghiệm du lịch thông minh", ["en"]="Smart travel experience", ["ko"]="스마트 여행 경험", ["zh"]="智能旅行体验", ["ja"]="スマートな旅行体験", ["th"]="ประสบการณ์การท่องเที่ยวอัจฉริยะ", ["fr"]="Expérience de voyage intelligente" },
        ["SubHeroPrompt"]      = new() { ["vi"]="Chọn gói phù hợp để bắt đầu", ["en"]="Choose a plan to get started", ["ko"]="시작할 요금제를 선택하세요", ["zh"]="选择适合您的套餐", ["ja"]="プランを選んでください", ["th"]="เลือกแพ็กเกจที่เหมาะกับคุณ", ["fr"]="Choisissez un forfait pour commencer" },

        ["PlanDaily"]          = new() { ["vi"]="Gói Ngày", ["en"]="Daily Plan", ["ko"]="일일 플랜", ["zh"]="日套餐", ["ja"]="デイプラン", ["th"]="แพ็กวัน", ["fr"]="Forfait journée" },
        ["PlanDailyDesc"]      = new() { ["vi"]="1 ngày trải nghiệm thử", ["en"]="1-day trial experience", ["ko"]="1일 체험", ["zh"]="1天体验", ["ja"]="1日間体験", ["th"]="ทดลองใช้ 1 วัน", ["fr"]="1 jour d'essai" },
        ["PlanDailyPeriod"]    = new() { ["vi"]="/1 ngày", ["en"]="/1 day", ["ko"]="/1일", ["zh"]="/1天", ["ja"]="/1日", ["th"]="/1 วัน", ["fr"]="/1 jour" },

        ["PlanWeekly"]         = new() { ["vi"]="Gói Tuần", ["en"]="Weekly Plan", ["ko"]="주간 플랜", ["zh"]="周套餐", ["ja"]="ウィークリープラン", ["th"]="แพ็กสัปดาห์", ["fr"]="Forfait semaine" },
        ["PlanWeeklyDesc"]     = new() { ["vi"]="7 ngày trải nghiệm đầy đủ", ["en"]="7-day full experience", ["ko"]="7일 완전 체험", ["zh"]="7天完整体验", ["ja"]="7日間フル体験", ["th"]="7 วันแบบเต็ม", ["fr"]="7 jours d'expérience complète" },
        ["PlanWeeklyPeriod"]   = new() { ["vi"]="/7 ngày", ["en"]="/7 days", ["ko"]="/7일", ["zh"]="/7天", ["ja"]="/7日", ["th"]="/7 วัน", ["fr"]="/7 jours" },

        ["PlanMonthly"]        = new() { ["vi"]="Gói Tháng", ["en"]="Monthly Plan", ["ko"]="월간 플랜", ["zh"]="月套餐", ["ja"]="マンスリープラン", ["th"]="แพ็กเดือน", ["fr"]="Forfait mensuel" },
        ["PlanMonthlyDesc"]    = new() { ["vi"]="30 ngày không giới hạn", ["en"]="30 days unlimited", ["ko"]="30일 무제한", ["zh"]="30天无限制", ["ja"]="30日無制限", ["th"]="30 วันไม่จำกัด", ["fr"]="30 jours illimité" },
        ["PlanMonthlyPeriod"]  = new() { ["vi"]="/30 ngày", ["en"]="/30 days", ["ko"]="/30일", ["zh"]="/30天", ["ja"]="/30日", ["th"]="/30 วัน", ["fr"]="/30 jours" },
        ["BadgePopular"]       = new() { ["vi"]="PHỔ BIẾN", ["en"]="POPULAR", ["ko"]="인기", ["zh"]="热门", ["ja"]="人気", ["th"]="ยอดนิยม", ["fr"]="POPULAIRE" },

        ["PlanYearly"]         = new() { ["vi"]="Gói Năm", ["en"]="Yearly Plan", ["ko"]="연간 플랜", ["zh"]="年套餐", ["ja"]="イヤリープラン", ["th"]="แพ็กปี", ["fr"]="Forfait annuel" },
        ["PlanYearlyDesc"]     = new() { ["vi"]="365 ngày · Tiết kiệm hơn 40%", ["en"]="365 days · Save over 40%", ["ko"]="365일 · 40% 이상 절약", ["zh"]="365天 · 节省40%以上", ["ja"]="365日 · 40%以上お得", ["th"]="365 วัน · ประหยัดกว่า 40%", ["fr"]="365 jours · Économisez plus de 40%" },
        ["PlanYearlyPeriod"]   = new() { ["vi"]="/365 ngày", ["en"]="/365 days", ["ko"]="/365일", ["zh"]="/365天", ["ja"]="/365日", ["th"]="/365 วัน", ["fr"]="/365 jours" },
        ["BadgeSave"]          = new() { ["vi"]="TIẾT KIỆM", ["en"]="BEST VALUE", ["ko"]="절약", ["zh"]="超值", ["ja"]="お得", ["th"]="คุ้มค่า", ["fr"]="ÉCONOMIQUE" },

        ["SubFeaturesTitle"]   = new() { ["vi"]="Bao gồm trong tất cả các gói:", ["en"]="Included in all plans:", ["ko"]="모든 요금제에 포함:", ["zh"]="所有套餐包含:", ["ja"]="全プランに含まれる:", ["th"]="รวมอยู่ในทุกแพ็กเกจ:", ["fr"]="Inclus dans tous les forfaits :" },
        ["SubFeature1"]        = new() { ["vi"]="Bản đồ tương tác với OpenStreetMap", ["en"]="Interactive map with OpenStreetMap", ["ko"]="OpenStreetMap 인터랙티브 지도", ["zh"]="OpenStreetMap 互动地图", ["ja"]="OpenStreetMapインタラクティブマップ", ["th"]="แผนที่เชิงโต้ตอบด้วย OpenStreetMap", ["fr"]="Carte interactive OpenStreetMap" },
        ["SubFeature2"]        = new() { ["vi"]="Thuyết minh tự động theo vị trí (7 ngôn ngữ)", ["en"]="Auto narration by location (7 languages)", ["ko"]="위치 기반 자동 해설 (7개 언어)", ["zh"]="按位置自动讲解（7种语言）", ["ja"]="位置情報による自動ナレーション（7言語）", ["th"]="การบรรยายอัตโนมัติตามตำแหน่ง (7 ภาษา)", ["fr"]="Narration automatique par position (7 langues)" },
        ["SubFeature3"]        = new() { ["vi"]="Tạo tour AI thông minh", ["en"]="AI-powered smart tour creation", ["ko"]="AI 스마트 투어 생성", ["zh"]="AI智能创建路线", ["ja"]="AIスマートツア作成", ["th"]="สร้างทัวร์อัจฉริยะด้วย AI", ["fr"]="Création de tour intelligent par IA" },
        ["SubFeature4"]        = new() { ["vi"]="Thống kê hành trình cá nhân", ["en"]="Personal journey statistics", ["ko"]="개인 여행 통계", ["zh"]="个人行程统计", ["ja"]="個人旅行統計", ["th"]="สถิติการเดินทางส่วนตัว", ["fr"]="Statistiques de voyage personnelles" },
        ["SubFooterNote"]      = new() { ["vi"]="Thanh toán qua chuyển khoản ngân hàng · Không tự động gia hạn", ["en"]="Pay via bank transfer · No auto-renewal", ["ko"]="계좌이체로 결제 · 자동 갱신 없음", ["zh"]="银行转账支付 · 不自动续费", ["ja"]="銀行振込支払 · 自動更新なし", ["th"]="ชำระผ่านการโอนเงิน · ไม่ต่ออายุอัตโนมัติ", ["fr"]="Paiement par virement · Pas de renouvellement automatique" },

        ["SubPayTitle"]        = new() { ["vi"]="Thanh toán", ["en"]="Payment", ["ko"]="결제", ["zh"]="支付", ["ja"]="お支払い", ["th"]="การชำระเงิน", ["fr"]="Paiement" },
        ["SubScanQR"]          = new() { ["vi"]="Quét mã để thanh toán", ["en"]="Scan QR to pay", ["ko"]="QR 스캔하여 결제", ["zh"]="扫码支付", ["ja"]="QRをスキャンして支払い", ["th"]="สแกน QR เพื่อชำระเงิน", ["fr"]="Scannez le QR pour payer" },
        ["SubOrManual"]        = new() { ["vi"]="Hoặc chuyển khoản thủ công:", ["en"]="Or transfer manually:", ["ko"]="또는 수동 이체:", ["zh"]="或手动转账:", ["ja"]="または手動振込:", ["th"]="หรือโอนเงินด้วยตนเอง:", ["fr"]="Ou transférer manuellement :" },
        ["SubBank"]            = new() { ["vi"]="Ngân hàng", ["en"]="Bank", ["ko"]="은행", ["zh"]="银行", ["ja"]="銀行", ["th"]="ธนาคาร", ["fr"]="Banque" },
        ["SubAccountNo"]       = new() { ["vi"]="Số tài khoản", ["en"]="Account No.", ["ko"]="계좌번호", ["zh"]="账号", ["ja"]="口座番号", ["th"]="หมายเลขบัญชี", ["fr"]="N° de compte" },
        ["SubAmount"]          = new() { ["vi"]="Số tiền", ["en"]="Amount", ["ko"]="금액", ["zh"]="金额", ["ja"]="金額", ["th"]="จำนวนเงิน", ["fr"]="Montant" },
        ["SubTransferRef"]     = new() { ["vi"]="Nội dung chuyển khoản", ["en"]="Transfer reference", ["ko"]="이체 내용", ["zh"]="转账备注", ["ja"]="振込内容", ["th"]="เนื้อหาการโอน", ["fr"]="Référence de virement" },
        ["SubRefImportant"]    = new() { ["vi"]="(quan trọng — không được thiếu)", ["en"]="(important — do not omit)", ["ko"]="(중요 — 생략 불가)", ["zh"]="(重要 — 不能省略)", ["ja"]="（重要 — 省略不可）", ["th"]="(สำคัญ — ห้ามละเว้น)", ["fr"]="(important — ne pas omettre)" },
        ["SubWarning"]         = new() { ["vi"]="Vui lòng ghi đúng nội dung chuyển khoản để chúng tôi xác nhận thanh toán của bạn.", ["en"]="Please enter the exact transfer reference so we can confirm your payment.", ["ko"]="결제 확인을 위해 이체 내용을 정확히 입력하세요.", ["zh"]="请正确填写转账备注以便我们确认您的付款。", ["ja"]="お支払いを確認するために振込内容を正確に入力してください。", ["th"]="กรุณากรอกเนื้อหาการโอนให้ถูกต้องเพื่อยืนยันการชำระเงิน", ["fr"]="Veuillez saisir la référence exacte pour confirmer votre paiement." },
        ["SubConfirmBtn"]      = new() { ["vi"]="✅  Tôi đã thanh toán", ["en"]="✅  I have paid", ["ko"]="✅  결제 완료", ["zh"]="✅  我已支付", ["ja"]="✅  支払い完了", ["th"]="✅  ฉันชำระแล้ว", ["fr"]="✅  J'ai payé" },
        ["SubConfirmNote"]     = new() { ["vi"]="Sau khi CK, nhấn nút để báo. Vào app sau khi Admin xác nhận — dùng nút bên dưới để kiểm tra.", ["en"]="After paying, tap to report. You enter after Admin confirms — use the button below to check.", ["ko"]="이체 후 버튼으로 알림. 관리자 확인 후에 앱 이용 — 아래에서 확인.", ["zh"]="转账后点按钮上报。管理员确认后方可进入应用 — 可用下方按钮查询。", ["ja"]="振込後にボタンで報告。管理者確認後にアプリへ — 下のボタンで確認。", ["th"]="หลังโอนแล้วกดแจ้ง เข้าแอปหลังแอดมินยืนยัน — ใช้ปุ่มด้านล่างเพื่อตรวจสอบ", ["fr"]="Après le virement, appuyez pour signaler. Accès après confirmation admin — bouton ci-dessous pour vérifier." },
        ["SubCheckEntitlementBtn"] = new() { ["vi"]="🔁  Kiểm tra đã duyệt", ["en"]="🔁  Check approval", ["ko"]="🔁  승인 확인", ["zh"]="🔁  查询是否已批准", ["ja"]="🔁  承認を確認", ["th"]="🔁  ตรวจสอบการอนุมัติ", ["fr"]="🔁  Vérifier l’approbation" },
        ["SubReportSentTitle"] = new() { ["vi"]="Đã gửi báo", ["en"]="Report sent", ["ko"]="알림 전송됨", ["zh"]="已上报", ["ja"]="報告を送信しました", ["th"]="ส่งแล้ว", ["fr"]="Signalement envoyé" },
        ["SubReportSentMsg"]   = new() { ["vi"]="Chúng tôi đã ghi nhận. Vui lòng chờ Admin đối soát CK — bạn vào app sau khi được xác nhận.", ["en"]="Recorded. Please wait for Admin to reconcile — you can enter the app after confirmation.", ["ko"]="접수되었습니다. 관리자 대조 확인을 기다려 주세요.", ["zh"]="已记录。请等待管理员对账 — 确认后即可进入应用。", ["ja"]="受付しました。管理者の照合確認をお待ちください。", ["th"]="บันทึกแล้ว รอแอดมินตรวจสอบ — ยืนยันแล้วจึงเข้าแอปได้", ["fr"]="Enregistré. Attendez la réconciliation du compte admin." },
        ["SubReportFailTitle"] = new() { ["vi"]="Không gửi được", ["en"]="Could not send", ["ko"]="전송 실패", ["zh"]="发送失败", ["ja"]="送信できませんでした", ["th"]="ส่งไม่สำเร็จ", ["fr"]="Envoi impossible" },
        ["SubReportFailMsg"]   = new() { ["vi"]="Kiểm tra kết nối mạng và thử lại.", ["en"]="Check your network and try again.", ["ko"]="네트워크를 확인하고 다시 시도하세요.", ["zh"]="请检查网络后重试。", ["ja"]="ネットワークを確認して再試行してください。", ["th"]="ตรวจสอบเครือข่ายแล้วลองอีกครั้ง", ["fr"]="Vérifiez le réseau et réessayez." },
        ["SubNotApprovedTitle"] = new() { ["vi"]="Chưa vào được", ["en"]="Not approved yet", ["ko"]="아직 승인 안 됨", ["zh"]="尚未通过", ["ja"]="まだ承認されていません", ["th"]="ยังไม่อนุมัติ", ["fr"]="Pas encore approuvé" },
        ["SubNotApprovedMsg"]  = new() { ["vi"]="Chưa thấy xác nhận từ Admin hoặc đang chờ đối soát. Thử lại sau khi đã chuyển khoản đúng nội dung.", ["en"]="No admin confirmation yet or pending reconciliation. Try again after a correct transfer.", ["ko"]="관리자 확인이 없거나 대기 중입니다. 올바른 이체 후 다시 시도하세요.", ["zh"]="尚无管理员确认或正在对账。请正确转账后再试。", ["ja"]="管理者の確認がないか照合待ちです。正しい振込後に再試行してください。", ["th"]="ยังไม่มีการยืนยันหรือรอตรวจสอบ ลองอีกครั้งหลังโอนถูกต้อง", ["fr"]="Pas encore confirmé ou en attente de réconciliation." },
    };

    // Helper: same text for all languages
    private static Dictionary<string, string> L(string text) =>
        new() { ["vi"]=text, ["en"]=text, ["ko"]=text, ["zh"]=text, ["ja"]=text, ["th"]=text, ["fr"]=text };
}
