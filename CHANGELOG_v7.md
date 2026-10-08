# ScreenAnote v7

Feature 07/10-1: existing-browser full-page scrolling capture, shared stitching
engine, saved scroll position/visual-anchor restoration and partial-capture notes.

Feature 07/10-2: text background colour/transparency; icons-only bottom toolbar;
microphone plus WASAPI loopback system audio; complete rounded-button background
painting; embedded executable/window/taskbar icon.

Includes Windows x64 framework-dependent application and full .NET 8 source.
See README.md and VALIDATION.md for usage and validation scope.

## V7 build fix (7.0.1)

Added an explicit System.IO global import. Windows Desktop WPF SDK support
removes the implicit import, which previously broke Visual Studio source builds.

## Faster browser scrolling (7.0.2)

Restored original capture-from-current-position behaviour. Browser capture no
longer scrolls to the top or returns to the original position. Removed the UI
Automation/WPF dependency used for scroll restoration.

## Stop recording update (7.0.3)

Move speaker capture lifecycle to a background owner; remove disposal/callback
lock dependency. Bound audio shutdown waits and retain delayed native resources
until their owner exits. Show stop progress; save explicitly after completion.
Add callback-during-disposal regressions and optional Windows Stop integration test.

## Recording startup capture update (7.0.4)

Replace Graphics.CopyFromScreen in RegionRecorder with explicit per-frame GDI
handle acquisition/release and a single retry for handle/access errors. Confirm
startup only after a desktop frame is captured. Preserve earlier feature changes.

## Native recording bitmap update (7.0.5)

Use a native screen-compatible memory DC and top-down 32-bit DIB for capture.
Remove the Graphics.GetHdc capture destination; preserve negative monitor
coordinates. Include native operation/error/region in capture errors and an
optional 100-frame-per-monitor Windows capture check.

## V6 scrolling workflow restoration (7.0.6)

Restore V6 ScrollingCapture.cs, Capture.cs and DoCapture directly from the original
V6 ZIP. The engine itself was already unchanged; remove the added browser-window
selection wrapper. Both scrolling menu options now use the exact V6 scroll path.
