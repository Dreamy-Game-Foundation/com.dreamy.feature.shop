# Dreamy Shop

Reusable MVP Shop feature for casual and puzzle games.

## Runtime boundaries

- `ShopCatalogConfig` owns validated JSON design data.
- `ShopModel` performs atomic virtual-currency exchanges and grants IAP rewards only after a purchase gateway confirms a store transaction.
- `ShopPresenter` coordinates `IShopService` and `IShopView`.
- The host game owns `IResourceWallet`, persistence, analytics, localization, final visuals, and the store SDK adapter.
- Runtime contains no concrete `MonoBehaviour` view or project prefab dependency.

## Use the sample

Import **Shop Feature** from Package Manager. The imported folder contains:

- `ShopPanel.prefab`: a `BaseFeaturePanel` variant with safe area, backdrop fade, content scale, stagger control, status, offer list, and close button.
- `ShopOfferItem.prefab`: a `BaseFeatureItem` variant with fade/scale tween, title, reward, price, and purchase button. Dynamically spawned offers automatically join the panel's staggered tween sequence; no delay component is required on the item prefab.
- `shopCatalog.json`: copy-ready catalog data under `Resources/DataConfig`.
- `ShopPanel`, `ShopOfferItem`, and `ShopController`: replaceable host integration classes.

Register the catalog before initializing DataConfig, register an `IResourceWallet`, then install the feature:

```csharp
ShopInstaller.RegisterConfig(dataConfigService);
await dataConfigService.InitializeAsync(cancellationToken);

ServiceLocator.Register<IDataConfigService>(dataConfigService);
ServiceLocator.Register<IResourceWallet>(wallet);
ServiceLocator.Register<IShopPurchaseGateway>(purchaseGateway);
ShopInstaller.Install();
```

`purchaseKind: "VirtualCurrency"` offers use `costResourceId` and `costAmount`. `purchaseKind: "Iap"` offers use `storeProductId`, optional `displayPrice`, and rewards. The sample `SimulatedShopPurchaseGateway` confirms IAP offers immediately for Foundation testing. Replace it in the game with an `IShopPurchaseGateway` adapter over `DreamySDK.Purchase`; Shop Runtime has no SDK dependency.

The sample UI may be copied into a game feature folder and customized as prefab variants without changing package Runtime.
