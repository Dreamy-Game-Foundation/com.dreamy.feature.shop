# Local source release 0.2.0 — 2026-10-16

Annotated tag `v0.2.0` identifies the reviewed source, not production certification.
Fresh validation: targeted Unity EditMode 133/133 passed; Economy/Shop managed checks 88 passed; Progression managed checks 59 passed
(one Unity-only case is excluded by that harness). Connectivity clean runtime-only Unity consumer
and Android/iOS C# branch compiles passed. A separate clean consumer compiled all four packages
and their unmodified UI samples, validating required shortage bindings and prefab scripts.
Five tests in the unchanged Dreamy UI suite failed in the full-project EditMode run;
full-project green and native/device readiness are not certified. See the sandbox review report
`docs/evidence/resource-recovery-release/REVIEW.md` for Unity suite results and open gates.
The earlier implementation notes below describe historical validation and pre-tag working state.

---

# 0.2.0 implementation (not tagged/published)

Use optional typed wallet capability for virtual exchanges; add shortage deficits without changing legacy bool fallback. Optional presenter recovery callback owns a cancellation token. Add semantic destination map/gateway and scroll-list sample navigator; no navigation path purchases or grants.

Economy schema 3 cannot be loaded by old clients. Strict receipts live with the profile; do not prune for size. Custom stores must reload durable state on ambiguous write failures. Sandbox validation: 63 managed test cases plus Shop presenter harness, 127/127 Unity EditMode tests, Play Mode recovery demo, fresh sample import in sandbox, and StandaloneLinux64 managed player compilation (95 assemblies). Fresh external consumer, full native/mobile build/device and SDK billing gates remain unverified. Source validation evidence is recorded in docs/plans/DREAMY_ECONOMY_RESOURCE_RECOVERY_PLAN.md in the sandbox.

# Ghi chú phiên bản 0.1.3

Ngày: 2026-10-15. Phiên bản trước: 0.1.2.

Sửa cấu hình discovery test và Việt hoá hướng dẫn. Giữ virtual purchase là một TryExchange; không bổ sung durable IAP fulfillment.

## Tương thích và phạm vi

Giữ GUID asset/script và đường dẫn prefab cũ. Thư mục sample đã import trong
Assets/Samples giữ version/path lịch sử; import mới dùng version package mới.
Daily/Missions dùng Feature view contract: consumer phải thêm reference trực tiếp
Dreamy.Feature.Runtime ở asmdef dùng API và compile lại. Không coi đây là thay DLL
nhị phân mà không rebuild. Game có save cũ cần rehearsal migration trên bản copy.

Scope đã kiểm chứng là sandbox offline casual/puzzle bằng Unity Editor tests.
Không chứng nhận mọi consumer, production inventory, server/cloud authority,
SDK billing, player/device, Home/Addressables hoặc rollback rollout chưa chạy.
Tag source không thay các gate đó. Xem README và evidence của sandbox.
