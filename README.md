# Dreamy Shop

Package thuộc Dreamy Game Studio. Hướng dẫn dưới đây mô tả cấu trúc, cách cài vào project và tích hợp ở root/scene.

## Cài package

Dùng Unity 6000.0 trở lên. Sandbox đã tham chiếu package bằng `file:../LocalPackages/com.dreamy.feature.shop`. Project khác dùng Package Manager > + > Install package from disk và chọn package.json, hoặc Git URL của repository nội bộ. Cài cả dependency Dreamy/Git vào manifest của game; version dependency không tự cấu hình registry riêng.

Dependency trực tiếp theo package.json:

- `com.dreamy.core` (1.1.2)
- `com.dreamy.dataconfig` (0.2.0)
- `com.dreamy.feature.economy` (0.2.0)
- `com.dreamy.feature` (0.1.0)
- `com.dreamy.ui` (0.2.0)
- `com.cysharp.unitask` (2.5.10)

## Cấu trúc và asmdef

| Assembly | Reference | Phạm vi |
| --- | --- | --- |
| `Dreamy.Shop.Runtime` | Dreamy.Core.Runtime, Dreamy.DataConfig.Runtime, Dreamy.Economy.Runtime, UniTask | Runtime |

Trong asmdef của game, thêm assembly chứa API trực tiếp sử dụng. Code bootstrap reference thêm Core/DataConfig/Datasave/Economy theo nhu cầu; code async reference UniTask. Code gọi type sample reference assembly sample. Giữ Editor reference trong asmdef Editor-only.

## Cấu trúc package và luồng dữ liệu

| Thư mục | Vai trò |
| --- | --- |
| Runtime/Config | ShopCatalogConfig: đọc và validate catalog JSON. |
| Runtime/Contracts | IShopService, IShopView và IShopPurchaseGateway. |
| Runtime/Domain | ShopModel: exchange wallet, xác nhận purchase và tạo state/result. |
| Runtime/Presentation | ShopPresenter: nối service với panel, xử lý mua/đóng. |
| Runtime/Installation | ShopInstaller: đăng ký config, cài IShopService. |
| Samples~/Shop Feature | Panel, item, ShopFeatureInstaller, gateway adapter của host, prefab và JSON. |

Luồng: GameInstaller → config/wallet/gateway → IShopService → ShopPresenter → ShopPanel. Game sở hữu service dùng chung và tích hợp store thật.

## Cài service trong root GameInstaller

Ghép method sau vào bootstrap hiện có và await trước khi đánh dấu game Ready hoặc mở Shop. Đây là ví dụ root tối thiểu; nếu game đã có Datasave/DataConfig/wallet, dùng lại instance và thêm phần Shop vào đúng bước.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Core;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Economy;
using Dreamy.Shop;

// Method bên trong GameInstaller.
private async UniTask InstallShopAsync(
    IShopPurchaseGateway purchaseGateway,
    CancellationToken cancellationToken)
{
    var datasave = new DatasaveService();
    ServiceLocator.Register<IDatasaveService>(datasave);

    var wallet = new DatasaveResourceWallet(datasave);
    ServiceLocator.Register<IResourceWallet>(wallet);
    ServiceLocator.Register<IResourceBalanceProvider>(
        (IResourceBalanceProvider)wallet);

    var dataConfig = new DataConfigService(
        new ResourcesJsonConfigSource());
    ShopInstaller.RegisterConfig(dataConfig);
    await dataConfig.InitializeAsync(cancellationToken);
    ServiceLocator.Register<IDataConfigService>(dataConfig);

    ServiceLocator.Register<IShopPurchaseGateway>(purchaseGateway);
    ShopInstaller.Install();
}
```

Truyền gateway thanh toán của game vào method. ShopPurchaseGateway trong namespace Dreamy.Feature.Shop.Integration nhận callback thanh toán/receipt validation của host, không tự trả Purchased. Wallet ví dụ không seed tiền; thiết lập balance ban đầu theo game.

Giữ đúng một Resources/DataConfig/shopCatalog.json. Đăng ký catalog trước InitializeAsync; cài Shop sau khi config và wallet sẵn sàng. Nếu root đã khởi tạo DataConfig, thêm RegisterConfig vào bước đăng ký chung thay vì load lại service.

Asmdef của GameInstaller cần Core, DataConfig, Datasave, Economy, Shop Runtime và UniTask. Root unregister IShopService/IShopPurchaseGateway khi teardown; service config/save/wallet dùng chung được dọn theo lifecycle root.

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

Đăng ký tại composition root sau khi cài service, cùng factory với các feature khác:

```csharp
ShopFeatureInstaller.Install(factory, shopService);
PanelManager.Instance.PresenterFactory = factory;
```

Sau đó mở ở bất kỳ caller nào:

```csharp
await PanelManager.Instance.Show<ShopPanel>(PanelAddress.Shop);
// Hoặc await PanelManager.Instance.Transition<ShopPanel>(PanelAddress.Shop);
```

ShopPresenter implement IPanelPresenter (Show/Dispose) trong assembly không phụ thuộc UnityEngine `Dreamy.UI.Presentation`. Thêm reference này vào assembly dùng factory/presenter. UI host sở hữu một presenter cho mỗi lần mở, cleanup khi đóng/disable/destroy hoặc show lỗi; cached reopen tạo mới. Sample không còn ShopDemo và không cần caller tự quản lý presenter. Kết quả mua hàng về sau khi đóng UI không truy cập view; service vẫn sở hữu giao dịch/reward đang chạy.

Scene cần Canvas có PanelManager và EventSystem. Nút Close được presenter xử lý; đóng từ code bằng:

```csharp
await PanelManager.Instance.Close<ShopPanel>();
```

Build Addressables content cho target trước khi test player. Đóng panel không unload cache prefab; chỉ unload khi không còn instance/consumer dùng asset.

## Import sample

Mở Window > Package Manager, chọn Dreamy Shop > Samples > Import. Unity chép vào Assets/Samples/Dreamy Shop/0.1.1/. Chuyển cả folder nếu tùy biến, giữ .meta và reference prefab; không giữ bản script/asmdef hoặc Resources document trùng.

- **Shop Feature**: nguồn `Samples~/Shop Feature`.
  Assembly `Dreamy.Feature.Shop.Integration.Runtime` reference Dreamy.Shop.Runtime, Dreamy.Economy.Runtime, Dreamy.Core.Runtime, Dreamy.UI.Runtime, Unity.TextMeshPro, UnityEngine.UI, UniTask.

Kiểm tra trong sandbox bằng `python3 LocalPackages/com.dreamy.feature.settings/Tests~/validate-settings.py --shop`. Lệnh compile UI/Settings/Shop/template và chạy các regression presenter/model managed; không thay thế kiểm tra Unity lifecycle hoặc store trên device.

## Production feature installer

```csharp
ShopFeatureInstaller.RegisterConfig(dataConfig); // Before dataConfig.InitializeAsync.
// After config/save/wallet initialization:
ShopFeatureInstaller.Install(factory, catalog, wallet, purchaseGateway, balanceProvider);
```

Install cài IShopService và đăng ký ShopPanel/ShopPresenter cùng một entry point. Overload Install(factory, shopService) dùng service đã được host cài. ShopInstaller runtime vẫn dành cho custom UI, không cần gọi riêng trong luồng tích hợp này.

ShopPurchaseGateway nhận callback `Func<ShopGatewayPurchaseRequest, CancellationToken, UniTask<ShopGatewayPurchaseResult>>`. Callback gọi SDK/backend thật, xác thực giao dịch và trả transaction ID ổn định; adapter chuyển tiếp kết quả, không tạo giao dịch giả. Nếu chưa có gateway, IAP trả IapGatewayUnavailable; virtual-currency exchange vẫn hoạt động. GameInstaller không tự đăng ký gateway giả. Tên product/giá/catalog vẫn cần cấu hình theo game trước khi phát hành.
