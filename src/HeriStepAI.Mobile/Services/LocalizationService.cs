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
    };

    // Helper: same text for all languages
    private static Dictionary<string, string> L(string text) =>
        new() { ["vi"]=text, ["en"]=text, ["ko"]=text, ["zh"]=text, ["ja"]=text, ["th"]=text, ["fr"]=text };
}
