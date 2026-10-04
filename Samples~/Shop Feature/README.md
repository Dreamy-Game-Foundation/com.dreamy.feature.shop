# Shop Feature

Sample của Dreamy Shop. Import từ Window > Package Manager > Dreamy Shop > Samples > Import. Unity chép nội dung vào Assets/Samples/Dreamy Shop/0.1.1/Shop Feature/.

## Cấu trúc và tích hợp

Giữ nguyên folder, .meta, asmdef và reference prefab khi chuyển vào project. Chỉ giữ một bản script/asmdef và một JSON cho mỗi key Resources/DataConfig. Bootstrap config/save/wallet/audio tại GameInstaller trước khi bật UI, theo [README package](../../README.md). Link tương đối này dùng trong source package; sau import, mở README package từ Package Manager.

ShopPanel hiển thị offer, ShopOfferItem phát sự kiện mua, ShopDemo là ví dụ bind presenter. Gán container/item/button và giữ một shopCatalog.json. Cài config/wallet/gateway tại root, bind ShopPresenter trước animation. Thay SimulatedShopPurchaseGateway bằng gateway thanh toán thật.

Assembly Dreamy.Feature.Shop.Integration.Runtime reference Dreamy.Shop.Runtime, Dreamy.Economy.Runtime, Dreamy.Core.Runtime, Dreamy.UI.Runtime, Unity.TextMeshPro, UnityEngine.UI, UniTask.

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
