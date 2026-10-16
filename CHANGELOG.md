# Changelog

## [0.2.0] - 2026-10-16

- Consolidate the current implementation for a local source release; clean UI consumer checks passed; native/mobile gates and the full-project UI test failures remain open.
- Require Economy 0.5.0, the tagged source baseline containing the transaction/recovery contracts; synchronize documented dependencies.

## Repeat insufficient-purchase prompts — 2026-10-16

- Every insufficient purchase requests recovery again, including the same offer while acquisition is pending. Remove the offer-ID suppression in ShopPresenter.
- The managed popup presentation replaces the old session and waits for its close before reopening the cached panel; maintain one active popup and ignore stale purchase UI completions.
- Add regression coverage for repeated same-offer purchases, session replacement and multiple popup close/reopen cycles.

## Unreleased follow-up — 2026-10-16

- Wire Shop shortage recovery in the sandbox composition root; use the shared Popup stack.
- NotEnoughResourcesPanel now uses UIPanel and a BaseFeaturePanel prefab variant with inherited SafeArea/tweens.
- Reuse the existing Shop during acquisition; allow another offer without duplicate shortage sessions or stale result writes.

# 0.2.0 — 2026-10-15

Use optional typed wallet capability for virtual exchanges; add shortage deficits without changing legacy bool fallback. Optional presenter recovery callback owns a cancellation token. Add semantic destination map/gateway and scroll-list sample navigator; no navigation path purchases or grants.

# Changelog

## [0.1.2] - 2026-10-12

- Await optional successful-purchase presentation before rendering balances; ignore stale presentation after the view closes.
- Synchronize Shop sample Feedback integration and serialized reward/HUD bindings.
- Require Economy 0.2.1 for icon and target access; Feedback remains an optional sample dependency.

