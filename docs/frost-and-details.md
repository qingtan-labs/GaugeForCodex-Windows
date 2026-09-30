# Frost and information hierarchy refinement

## Identity

The taskbar and card header share the app icon's circular C and rounded terminal
mark. At small sizes, the square tile and quota ring are omitted rather than
shrinking the entire busy application icon. The card header is a WPF vector,
colored like the title text, with no bitmap dependency or dark square backdrop.
The taskbar retains independently hinted frames and system light/dark ink.

## Actual glass, not a transparent white screenshot

The old system Acrylic backdrop could contribute a thick tint before the WPF
overlay was applied. This refinement probes native blur-behind, then applies a
balanced neutral gradient (default top alpha 147/255, 58%). A faint
bright edge separates the card from its background. Only the optional desktop
widget has native background blur. The click-to-open tray flyout has a fully
opaque neutral surface and a zero-thickness glass frame: it does not use blur
or depend on the widget tint. No desktop screenshots, capture loop or fake
blurred wallpaper are used.

The compatibility blur uses a dynamically discovered composition function and
an accent policy. This policy is not a stable public Windows contract.
[Microsoft does not recommend this API and prefers DWM attributes](https://learn.microsoft.com/en-us/windows/win32/dwm/setwindowcompositionattribute).
Consequently failure falls back to documented system Acrylic, then a readable
high-opacity surface. Windows transparency disabled or high contrast selected
disables the blur. Older Windows uses the existing fallback. Effect changes
are reapplied when system preferences change.

Both the overly thin 16% style and the white-looking 80% style are migrated to
58%, without moving the widget. Balanced customized values are retained.
The desktop-only slider now spans 56–70%, preserving backdrop visibility as
well as contrast. Primary and
secondary text use opaque dark inks; smaller labels are enlarged. Text has no
blur, glow, opacity fade or shadow effect. Layout is rounded to device pixels.

The solid flyout permits ClearType; the translucent widget uses grayscale
antialiasing, not forcibly enabled subpixel rendering. Microsoft cautions that
[ClearType needs an opaque text background](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.renderoptions.cleartypehint?view=windowsdesktop-10.0).

The audit separately tests widget inks over the dimmest possible composited
surface (black RGB backdrop, 56% scrim, deep #12251A and #3D170F inks). Its calculated contrast is at least
4.5:1; this is a color-model check, not certification of every monitor or DWM
composition mode. Synthetic dark/light layout previews supplement the check.

## Information, once

- If an actual 300-minute window exists, show it on the main card, irrespective
  of which server field contains it. Do not create a missing five-hour quota.
- Otherwise show the tightest available window.
- Details show only additional windows. Single-window/no-data states have no
  disclosure button and cannot expand by clicking the card body.
- A live change to single-window data closes any old detail area immediately.
- Repeated fields do not create repeated windows; inconsistent copies of a
  known window use the stricter usage value.
- A low/exhausted additional window has a warning on the collapsed main card,
  so a healthy short window does not conceal an exhausted weekly allowance.
- Mouse focus does not leave a blue rectangle. Keyboard navigation retains a
  quiet neutral focus adorner instead of losing keyboard accessibility.

## Verified scope

Data tests cover single, dual, empty, repeated and inconsistent windows. The
app-native WPF harness checks disclosure visibility, collapse-on-refresh,
weekly exhaustion warning, vector header and default tint. The installed host
accepted blur-behind. Asset galleries and layout previews are not desktop
captures and cannot prove the actual DWM appearance; physical background,
pointer/keyboard and multi-monitor visual testing remain limited. ARM64 is
cross-compiled, not tested on native ARM hardware. No claim of code signing or
full accessibility certification is made.
