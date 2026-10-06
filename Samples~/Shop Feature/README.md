# Shop Feature

Sample của Dreamy Shop. Import từ Window > Package Manager > Dreamy Shop > Samples > Import. Unity chép nội dung vào Assets/Samples/Dreamy Shop/0.1.1/Shop Feature/.

## Cấu trúc và tích hợp

Giữ nguyên folder, .meta, asmdef và reference prefab khi chuyển vào project. Chỉ giữ một bản script/asmdef và một JSON cho mỗi key Resources/DataConfig. Bootstrap config/save/wallet/audio tại GameInstaller trước khi bật UI, theo [README package](../../README.md). Link tương đối này dùng trong source package; sau import, mở README package từ Package Manager.

ShopPanel hiển thị offer, ShopOfferItem phát sự kiện mua, ShopDemo là ví dụ bind presenter. Gán container/item/button và giữ một shopCatalog.json. Cài config/wallet/gateway tại root, bind ShopPresenter trước animation. Thay SimulatedShopPurchaseGateway bằng gateway thanh toán thật.

Assembly Dreamy.Feature.Shop.Integration.Runtime reference Dreamy.Shop.Runtime, Dreamy.Economy.Runtime, Dreamy.Core.Runtime, Dreamy.UI.Runtime, Unity.TextMeshPro, UnityEngine.UI, UniTask.

## Icon offer từ Sprite Atlas

Gán `Icon Atlas` trên ShopPanel trong Inspector. Sample nguồn để trống reference để host chọn atlas; prefab đã import trong sandbox được gán `Assets/_Project/SpriteAtlas/ShopOfferAtlas.spriteatlasv2`, chứa các sprite Gold/Gem. Khi import sample sang project khác, gán atlas của project đó. Phần icon dùng reference trực tiếp, không cần Addressables key hoặc Dreamy Assets loader.

Offer có field JSON tùy chọn `"iconKey": "Gem_0"`. Sample dùng key này làm tên sprite trong atlas; catalog cũ không có field vẫn hoạt động. Catalog có 6 offer đổi Gem lấy Gold (`Gold_0`–`Gold_5`), 1 offer đổi Coin lấy Gem và 6 gói Gem IAP (`Gem_0`–`Gem_5`). Giữ offer No Ads với iconKey trống và fallback vì atlas hiện chưa có icon No Ads. Các icon `Gold_Ads_0`/`Gem_Ads_0` dành cho rewarded ads nên chưa dùng; ShopPurchaseKind hiện chỉ hỗ trợ VirtualCurrency và Iap. Khi bổ sung ảnh, thêm vào atlas rồi gán iconKey đúng tên sprite.

ShopPanel lấy sprite đồng bộ khi Render, cache sprite clone theo key và truyền Sprite xuống ShopOfferItem. Gán `Fallback Icon` trên panel nếu muốn ảnh thay thế khi thiếu atlas, key trống hoặc không tìm thấy sprite; prefab dùng fallback sprite built-in của Unity; nếu bỏ fallback thì ẩn Image và vẫn hiển thị thông tin offer. ShopOfferItem dùng Image icon có sẵn trên BaseFeatureItem, khung 150×150. SetIcon bật preserveAspect khi gán sprite, không gọi SetNativeSize. Prefab có chỉnh text wrapping/overflow để giữ nội dung gọn.

Panel giữ sprite clone đến khi bị destroy và dọn chúng khi kết thúc. Atlas được giữ bằng serialized reference, không gọi unload/release thủ công. Nếu panel được cache, reference atlas và sprite cache cũng được giữ cùng panel. Panel vẫn có thể được tải bằng Addressables; atlas là dependency đi kèm prefab.

## Addressables Group và PanelAddress

1. Tạo variant từ sample Prefabs/ShopPanel.prefab, lưu tại Assets/_Project/Prefabs/Panel/ShopPanel.prefab.
2. Kiểm tra root có ShopPanel và đã gán close button, status label, offer container, ShopOfferItem prefab.
3. Mở Window > Asset Management > Addressables > Groups; tạo settings nếu cần.
4. Tạo group UI Panels, kéo prefab variant vào group.
5. Đặt cột Address thành Panel/ShopPanel.prefab. HomePanel dùng Panel/HomePanel.prefab.
6. Tạo class chung trong code game:

```csharp
public static class PanelAddress
{
    public const string Home = "Panel/HomePanel.prefab";
    public const string Shop = "Panel/ShopPanel.prefab";
}
```

Address là key tự đặt, không phải đường dẫn asset tự động. Constant phải khớp cột Address, kể cả chữ hoa/thường. Tên group không nằm trong key. Item prefab được panel reference trực tiếp nên không cần address riêng để spawn offer.

## Mở panel sau khi root đã Ready

Code UI cần namespace Dreamy.UI, Dreamy.Core, Dreamy.Shop và Dreamy.Feature.Shop.Integration. Reference Dreamy.UI.Runtime, Dreamy.Shop.Runtime, Dreamy.Core.Runtime, UniTask và Dreamy.Feature.Shop.Integration.Runtime trong asmdef.

Trong method async UniTask:

```csharp
ShopPanel panel = await PanelManager.Instance.Create<ShopPanel>(
    PanelAddress.Shop);

var presenter = new ShopPresenter(
    ServiceLocator.Get<IShopService>(), panel);
panel.Destroyed += presenter.Dispose;
presenter.Show();
await panel.Show();
```

Host giữ một presenter cho mỗi panel instance. Nếu panel được cache, dùng lại presenter và gọi Show() khi mở lại; không tạo thêm presenter mỗi lần mở cùng instance. Khi dùng luồng host này, bỏ ShopDemo trên instance nếu có để tránh hai presenter xử lý cùng button.

Scene cần Canvas có PanelManager và EventSystem. Nút Close được presenter xử lý; đóng từ code bằng:

```csharp
await PanelManager.Instance.Close<ShopPanel>();
```

Build Addressables content cho target trước khi test player. Đóng panel không unload cache prefab; chỉ unload khi không còn instance/consumer dùng asset.

## Import sample

Mở Window > Package Manager, chọn Dreamy Shop > Samples > Import. Unity chép vào Assets/Samples/Dreamy Shop/0.1.1/. Chuyển cả folder nếu tùy biến, giữ .meta và reference prefab; không giữ bản script/asmdef hoặc Resources document trùng.

- **Shop Feature**: nguồn `Samples~/Shop Feature`.
  Assembly `Dreamy.Feature.Shop.Integration.Runtime` reference Dreamy.Shop.Runtime, Dreamy.Economy.Runtime, Dreamy.Core.Runtime, Dreamy.UI.Runtime, Unity.TextMeshPro, UnityEngine.UI, UniTask.

## No Ads và điểm tích hợp SDK production

Offer `iap-remove-ads` giữ product ID `com.dreamy.sample.remove_ads` và reward `entitlement.remove-ads` với amount 1. Gateway chỉ xác nhận giao dịch; ShopModel cấp reward vào wallet. DatasaveResourceWallet lưu reward trước khi phát BalanceChanged.

`EntitlementEffectBinding` nhận IResourceBalanceSource, ResourceId và callback từ host. Binding đọc balance ngay khi tạo (startup với save đã có), lắng nghe thay đổi entitlement (mua mới hoặc restore cấp vào cùng wallet), và gọi callback một lần khi balance > 0. Constant ResourceId nằm trong EntitlementIds. Host sở hữu binding và Dispose trước khi kết thúc wallet/SDK.

Khi SDK đã sẵn sàng và wallet đã load, đăng ký tại root:

```csharp
noAdsBinding = new EntitlementEffectBinding(
    walletBalanceSource,
    EntitlementIds.RemoveAds,
    DreamySDK.SetNoAds,
    exception => Debug.LogException(exception));
```

GameInstaller trong sandbox dùng ApplyNoAdsDemo để log, chưa gọi SDK thật. Thay callback đó khi SDK được cài. Binding bắt lỗi callback và gửi đến reportError; callback báo lỗi phải không throw. Gọi Synchronize để thử lại nếu SDK lỗi tạm thời; binding không tự retry theo timer. Callback effect cần idempotent. Đây là effect một chiều khi đã sở hữu, chưa xử lý thu hồi quyền hoặc đổi tài khoản; host phải tạo binding mới theo owner/SDK session mới.

Có thể tạo thêm binding cho entitlement khác mà không sửa gateway. Production vẫn cần adapter IShopPurchaseGateway cho store thật, xử lý non-consumable và restore quyền sở hữu vào wallet. Sample chặn mua lại offer purchaseOnce theo wallet, chưa triển khai restore và còn dùng SimulatedShopPurchaseGateway; giá/product ID là dữ liệu demo.

## Ví dụ nhiều gói entitlement IAP

Catalog bổ sung 3 gói: VIP vĩnh viễn (`entitlement.vip.lifetime`), Starter Fund (`entitlement.fund.starter`) và premium Battle Pass mùa 1 (`entitlement.battle-pass.season-1.premium`). Product ID và giá là demo; dùng fallback icon vì atlas Gold/Gem chưa có hình phù hợp.

GameInstaller đăng ký cả 4 binding trong InstallEntitlementEffects, giữ list và Dispose toàn bộ khi bị destroy. Đăng ký tại installer là phù hợp vì đây là composition root: chỉ nối wallet với callback, không xử lý nghiệp vụ VIP/Fund/Pass. Callback demo chỉ ghi log. Production thay bằng SDK/service có sẵn, sau khi chúng đã sẵn sàng. Không đăng ký binding theo mỗi lần mở ShopPanel.

VIP ở đây là quyền vĩnh viễn, không phải subscription. Fund mới mở khóa, chưa có mốc nhận/claim. Battle Pass có ID theo season để quyền mùa trước không tự mở mùa sau; service production phải kiểm tra mùa đang hoạt động, tiến độ và claim save riêng. Binding không xử lý thời hạn, revoke hoặc account switch.

### Đăng ký tại installer của project sử dụng package

Sau khi wallet đã load và SDK/service đã sẵn sàng, giữ các binding tại root:

```csharp
private readonly List<EntitlementEffectBinding> entitlementBindings = new();

private void InstallEntitlementEffects(IResourceBalanceSource wallet)
{
    Bind(wallet, EntitlementIds.RemoveAds, ApplyNoAds);
    Bind(wallet, EntitlementIds.LifetimeVip, ApplyLifetimeVip);
    Bind(wallet, EntitlementIds.StarterFund, UnlockStarterFund);
    Bind(wallet, EntitlementIds.Season1PremiumPass, UnlockSeason1PremiumPass);
}

private void Bind(IResourceBalanceSource wallet, ResourceId id, Action effect)
{
    entitlementBindings.Add(new EntitlementEffectBinding(
        wallet, id, effect, exception => Debug.LogException(exception)));
}

private void DisposeEntitlementEffects()
{
    foreach (var binding in entitlementBindings) binding.Dispose();
    entitlementBindings.Clear();
}
```

Đoạn trên dùng System, System.Collections.Generic, Dreamy.Economy, Dreamy.Feature.Shop.Integration và UnityEngine. Các callback ApplyNoAds/ApplyLifetimeVip/UnlockStarterFund/UnlockSeason1PremiumPass do host triển khai; ví dụ ApplyNoAds gọi DreamySDK.SetNoAds khi SDK đã có. Gọi DisposeEntitlementEffects khi root kết thúc, trước khi hủy wallet/SDK. Nếu installer đã có OnDestroy, thêm cleanup vào method đó. Sample không phụ thuộc SDK cụ thể.

## Gói mua một lần

No Ads, VIP lifetime, Starter Fund và premium pass mùa 1 khai báo `purchaseOnce: true` và `ownershipResourceId` trỏ tới entitlement reward. Balance > 0 nghĩa là đã sở hữu: ShopModel trả AlreadyOwned trước khi gọi gateway/trừ tiền; UI hiển thị Owned và khóa nút. ShopOfferItem vẫn giữ khóa Owned khi presenter bật lại các nút sau giao dịch. Gold/Gem không có purchaseOnce nên mua nhiều lần. Catalog cũ mặc định purchaseOnce = false.

Host phải cung cấp IResourceBalanceProvider (truyền vào model/installer hoặc wallet implement interface). Model từ chối khởi tạo nếu có gói mua một lần mà không đọc được balance. Quyền từ save/restore được đọc trực tiếp khi GetState/Purchase. Request IAP cùng offer đang chờ trả PurchaseInProgress để tránh gọi gateway lần thứ hai trên cùng model. Ownership resource phải được grant đúng một lần và là reward cuối của offer, để việc cấp reward trước đó bị lỗi không đánh dấu sở hữu quá sớm. Production vẫn phải xác thực non-consumable và restore qua store; chính sách này là kiểm tra quyền local, không thay thế store.

## Reward text

`rewardText` trong mỗi offer là nhãn hiển thị ngắn, ví dụ `"rewardText": "1,000 Gold"`, `"VIP"` hoặc `"Premium Pass"`. ShopOfferItem chỉ bind Offer.RewardText, không suy ra nhãn từ resource ID. Khi chỉnh reward amount, cập nhật rewardText tương ứng. Field này tùy chọn cho catalog cũ; thiếu field thì nhãn trống. Resource ID, amount và ownershipResourceId vẫn là dữ liệu cấp quyền/reward, độc lập với text hiển thị.
