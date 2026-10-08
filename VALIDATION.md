## V6 scrolling workflow restoration (7.0.6)

- ScrollingCapture.cs and Capture.cs match the original V6 ZIP byte for byte.
- MainForm.DoCapture matches the original V6 method exactly.
- Both scrolling menu options select the original scroll mode.
- Removed BrowserCapture.cs and its browser-window selection wrapper.
- Application compiled without errors; Windows runtime execution remains unverified.
- Earlier recording updates remain included.

## Native recording bitmap update (7.0.5)

The user's error code 6 and region X=-1909, Y=230, Width=1248, Height=715
identify an invalid handle during a capture on a left-hand monitor. The initial
frame succeeded; the precise native handle failure remains unconfirmed.

Replaced the Graphics.GetHdc destination with CreateCompatibleDC/CreateDIBSection,
then copy completed native pixels into the managed bitmap. Preserve negative
screen coordinates and report each native stage separately. Application and
Windows smoke-test source compile without errors. Native Windows testing has not
been performed here.

On Windows: dotnet run --project WindowsTests/WindowsSmoke.csproj -- --captureframes
captures 100 frames on every monitor, including monitors with negative coordinates.
Then run --recorderstop and verify recording manually on the affected monitor.

## Recording startup capture update (7.0.4)

The reported exception at RegionRecorder.Record line 59 identifies the
Graphics.CopyFromScreen call. Replaced recording screen capture with explicit
per-frame desktop DC acquisition, BitBlt and same-thread handle release.
Startup readiness now follows the first successful capture. Application and
Windows smoke-test source compiled without errors. Native Windows capture has
not been run here; verify Start/Stop on the affected Windows machine, including
secondary-monitor regions. Existing --recorderstop integration checks cover the
new capture path when run on Windows.

## Stop recording update (7.0.3)

- Application and Windows smoke-test source compiled without errors.
- Portable geometry, clipboard, timing and audio mixing tests passed.
- New fake-device shutdown regressions passed for ordinary capture and startup
  failure while an in-flight callback runs during disposal; repeated stop/dispose
  checks also passed.
- Real Windows audio drivers and Media Foundation finalization were not run here.
  The reported freeze has not been reproduced on the user's Windows environment.

Windows check: record with Mic off, then with Mic on; pause/resume and Stop. Check
shutdown stages, then click Save MP4 and verify video plus microphone/speaker audio.
Optional integration test: dotnet run --project WindowsTests/WindowsSmoke.csproj
-- --recorderstop. It fails if the recorder cannot stop within 15 seconds.

## Browser scrolling update (7.0.2)

Browser capture now delegates directly to the original ScrollingCapture.Run engine.
It starts at the current browser position and does not restore the page afterward.
Compiled the application without errors. On Windows, verify that browser capture
starts at the selected position and finishes without any return scrolling.
The earlier restoration checks below apply only to the superseded 7.0.0 build.

## V7 source build correction (7.0.1)

Reproduced missing Path, File, MemoryStream and IOException compile errors using
the SDK's actual implicit imports (WPF removes System.IO). Added GlobalUsings.cs
with an explicit System.IO import and compiled again using those same imports.
The corrected source compiles without errors. Full MSBuild/Visual Studio execution
and Windows runtime checks remain unavailable in this environment.

# V7 validation — Features 07/10-1 and 07/10-2

Completed in this environment:
- Compiled the complete application with .NET 8 C# compiler and Windows Desktop
  reference assemblies: no errors. WebView2 framework compatibility warnings only.
- Generated a Windows x64 apphost carrying the embedded icon, version and manifest.
- Compiled the Windows smoke-test project: no errors.
- Ran the existing geometry/clipboard/timing suite plus new background round-trip,
  legacy transparency and PCM mixing/saturation checks: all passed.

The included executable has not been launched on Windows here. Browser capture,
real audio devices, native MP4 encoding and taskbar visuals need Windows checks:

1. Run Start.cmd and verify the taskbar icon and button corners at 100%/150% DPI.
2. Add text; use Text box… to apply a solid background, then transparency. Copy,
   paste, duplicate, undo and save PNG; check that the appearance is preserved.
3. Confirm the bottom toolbar has icons only and readable tooltips.
4. In Chrome, Edge and Firefox, start halfway down a static long page. Use Full
   webpage from Browser; select only scrolling content. Check top/bottom content,
   seams and restored scroll position. Check the reported approximate-restoration
   fallback if the browser does not expose UI Automation scrolling.
5. Repeat with Escape, a short page, nested scroller and lazy-loading content.
6. Play sound on the default playback device while speaking; record with Mic on.
   Toggle Mic off/on, pause/resume and save. Confirm both sound sources, silence
   while muted and no audio from the paused interval.
7. Optional native checks: dotnet run --project WindowsTests/WindowsSmoke.csproj
   -- --microphone --systemaudio (speak and play speaker audio when prompted).

# Validation record

Performed in the Linux delivery environment:

- Release Windows x64 publish targeting .NET 8 succeeded using .NET SDK 10.0.100, with no
  emitted warnings or errors. The package contains the published executable
  and its dependencies.
- All main C# files parsed using the tree-sitter C# grammar: no syntax error or
  missing syntax nodes.
- Geometry checks executed successfully: movement transforms, scaling, four resize
  handles, inversion prevention, reversed arrows, segment hit distances, reverse
  drag bounds and pointer-anchored zoom. The net8.0 geometry test assembly was run
  using the installed .NET 10 host with major roll-forward in Linux.
- Project file and application manifest parsed as XML.
- Source reviewed for screen bounds, cancel paths, image ownership/disposal,
  undo/redo transitions, WebView2 protocol JSON, output size limits and export.
- Packaged ZIP tested for integrity and expected project files.

Not performed: executable launch, Windows screen capture, clipboard transfer,
Windows DPI tests, or browser rendering integration. The environment runs Linux;
the published Windows executable must be smoke-tested on Windows.

## Windows smoke test

1. Run Build.cmd: confirm successful restore and compilation. Run Run.cmd.
2. Capture region, fullscreen and a selected window; verify app is absent from
   capture. Cancel region selection with Escape. Test a second monitor at different
   DPI and a monitor with a negative desktop coordinate.
3. Apply each editing tool. Confirm marks follow the pointer at fitted preview
   scale. Crop, undo and redo. Check colours, stroke and text/icon size.
4. Open a PNG, add text and a smiley. Save PNG/JPEG/BMP/TIFF/GIF and reopen each
   file. Copy and paste in Paint; confirm annotations and redaction are flattened.
5. Capture a static long page with Scroll area, excluding sticky content. Check
   row continuity at joins, bottom detection and Escape cancellation. Test a page
   which cannot be matched and confirm the partial image and explanation appear.
6. Open a long URL in Full web page; capture and verify dimensions exceed the
   viewport, including the bottom of the page. Test failed navigation, login,
   lazy-loaded content and missing WebView2 Runtime.
7. Verify replacement/close prompts, no-image Save/Copy, and all keyboard shortcuts.
8. Run Publish.cmd, then launch publish/ScreenAnote.exe on Windows without the .NET
   runtime installed (with WebView2 Runtime for web capture).

## v2 editor smoke test additions (Windows)

- Zoom at different pointer positions using Ctrl+wheel, then pan with middle drag,
  Alt+left drag and the Pan tool. Fit must return the full screenshot to view.
- Place text, freehand, rectangles, arrows, highlights, redaction and pixelation;
  reselect each, move, resize all four handles, restyle and undo/redo.
- Double-click text, edit its contents, change font size and test narrow text boxes.
- Delete selected objects and verify they disappear from preview and export.
- Check arrow direction after resizing horizontal, vertical and reversed arrows.
- Import transparent PNG, ICO and an animated GIF; verify first-frame GIF placement,
  resizing, tint, and original transparency. Export PNG/GIF and copy to Paint.
- Crop an annotated image and confirm retained objects stay selectable, shift into
  the new coordinates, and undo restores the previous document.
- Open a previously flattened screenshot and confirm only newly added annotations
  can be selected; no object metadata is inferred from existing pixels.

## v3 Compact Canvas validation

- Release publish of the .NET 8 Windows x64 application succeeded.
- Geometry tests passed, including zoom and annotation transforms.
- Floating-layout tests passed for 864×520, 1280×760 and 1920×1000 workspaces,
  including offscreen selections and avoidance of sticker-drawer/palette overlap.
- Sticker catalogue checked for at least 120 unique symbols and searchable names.
- Windows visual and interaction testing remains pending. On Windows, verify
  rounded cards, vector icons, keyboard tab order, 100/150/200% display scaling,
  menu opening, sticker search/category filters, and contextual controls while
  moving/resizing objects near canvas edges.

## v4 fixes and new-feature validation

- Layout updated against Compact Canvas option 3: integrated single header,
  floating palette, rounded panels/shadows, contextual swatches and compact
  illustrated sticker grid. Windows visual equivalence is not verified in Linux.
- All 124 PNG assets (128 catalogue entries) validated for dimensions/transparency; the Featured 16
  artwork tiles visually inspected as a contact sheet.
- Data-model tests passed for counter increment/reset/invalid reset, independent
  duplicate transforms and editable clipboard roundtrips for text, arrows,
  rectangles, counters, pixelation and freehand. Image encoding/Windows clipboard
  integration is excluded from the portable data-model tests.
- Layout tests passed at multiple workspace sizes and 100/150/200% scale factors;
  real Windows DPI/font/rendering behavior still requires verification.
- Windows smoke checks: hold Space and drag; release Space while holding the
  mouse; type spaces in search; Alt-tab while holding Space; place/reset/undo
  counters; Ctrl-drag shapes/text/image stickers; Ctrl-click without movement
  must not duplicate; Ctrl+C/V between windows; paste after opening another
  image; export and verify all objects are flattened without application chrome.

## v5 validation and required Windows checks

Completed in Linux:

- Release .NET 8 Windows x64 publish, 0 warnings / 0 errors.
- Portable tests passed for all four proportional resize anchors, minimum sizes,
  group bounding/transforms, full-box marquee selection and group clipboard JSON.
- Portable video tests passed for even output sizes/1080p bounds, repeated
  pause/resume timing and BGRA → NV12 spatial/luma/chroma conversion.
- The Windows-only smoke-test project compiled with 0 warnings / 0 errors.
  It exercises native H.264 encoding, sample timing, MP4 finalization/container
  structure and transparent PNG serialization. It was **not executed** here.
- UI source reviewed to remove recursive parent/canvas painting, reparent floating
  cards as opaque canvas siblings, avoid tool-state invalidation on every frame,
  freeze the contextual bar during gestures and cache sticker thumbnails.

Not claimed: Windows encoder playback, actual clipboard integration, pixel-exact
mockup equivalence, flicker elimination observed on Windows, or real DPI rendering.

On Windows:

1. Run `dotnet run --project WindowsTests/WindowsSmoke.csproj -c Release`.
   Confirm two PASS messages; N editions need the Media Feature Pack.
2. Copy an image in Paint/browser, Ctrl+V into an empty editor and annotate it.
   Repeat with an existing document; Cancel must preserve it. Paste text in the
   sticker search box normally. Copy/paste editable groups between app instances.
3. Ctrl-click several objects and toggle one off. Move, resize, restyle and delete
   the group. Ctrl-drag must produce one group copy; Ctrl-click must not duplicate.
   Undo/redo restores objects and group selection. Shift-drag in both directions
   selects fully enclosed objects. Test overlapping objects and blank regions.
4. Hold Shift before resizing, press/release it during resizing, and test all
   four corners for text, image stickers, rectangles, arrows, counters and groups.
5. Keep sticker and contextual panels open. Drag/resize continuously, zoom and
   pan, then check that the bottom palette and drawer remain stable without
   flashing. The contextual panel should move once after release. Repeat at
   100%, 150% and 200% DPI and on differently scaled monitors.
6. Record a moving clock in a selected region for 3 seconds, pause for 3 seconds,
   resume for 3 seconds, stop/save MP4, and play it. Duration should be about
   6 seconds, colour/orientation correct, and the pause gap excluded. Confirm
   Stop completes the file and no controller UI is recorded on supported builds.
7. Cancel region selection, reselect before Start, record odd-sized/portrait/4K
   regions, test hotkeys, stop immediately, cancel Save then retry, and close
   while running. Confirm no hidden recording worker remains and the editor
   returns with its screenshot/annotations intact.
8. Test Save to a read-only folder: a clear error must leave Save MP4 available.
   Closing an unsaved recording or starting again must ask before discarding it.
   Start/Stop repeatedly; monitor GDI handles and temporary files for leaks.

## v6 checks

Completed in Linux:

- .NET 8 Release publish and Windows smoke-test compilation: 0 errors/warnings.
- Portable tests for each border style, independent border colour/width, padding
  stability, annotation cloning/copy/paste and legacy clipboard field defaults.
- Counter sizing tests verify circular diameter, centre preservation and 12–400
  limits. Existing geometry/group/undo model checks remain in the test suite.
- PCM tests verify sample/timestamp conversion over 10 minutes, ordered chunk
  stitching, silence on underflow, and discard of queued audio on mute/pause.
- All 365 PNGs checked for 192×192 dimensions and transparency; 18 categories,
  unique catalogue keys/names and matching embedded resources checked.
- A sample contact sheet of animals, food, technology, weather, transport, health,
  sports, callouts and smileys was visually inspected.
- Recorder source reviewed for pause/mute gate, default-off mic, prepared WinMM
  buffer lifetimes, bounded PCM queue, capture on a separate worker, hardware
  closure before MP4 finalization and audio/video timestamp alignment.

Pending Windows verification:

1. Select text; test None/Solid/Dashed/Dotted/Rounded borders, widths 1 and 20,
   contrasting text/border colours, multiline text, editing, scaling, duplication,
   clipboard transfers and undo/redo. Verify padding and export consistency.
2. Set Counter size before placement, then resize an existing counter using Size
   and handles. Check centre preservation, numbers 1/10/100, group operations,
   copying, and undo/redo. Reset remains available in ⋯ and More.
3. Run the native H.264/AAC smoke test. It generates tone+synthetic video, verifies
   avc1/mp4a descriptions and finalized container structure, then deletes the MP4.
   It was compiled but **not run** in Linux.
4. Run the --microphone smoke check and speak when prompted. Check mute yields
   silence. No default microphone or Windows permission integration was executed
   in the delivery environment.
5. Record with Mic off; turn on and speak; mute; resume mic; pause for 3 seconds;
   resume and speak; stop/save/play. Check voice clarity and synchronization,
   silence in muted intervals and exclusion of paused time. Allow small capture/
   AAC buffering latency at transitions; system audio must not be recorded.
6. Rapidly toggle mic and pause/resume, unplug the default mic, deny microphone
   permission, start/stop repeatedly, and close while recording. Failure should
   report a clear message and preserve video recording with the mic off. Confirm
   Windows microphone activity ends on mute/pause/stop and no worker remains.
7. Browse all sticker categories and pages, search by name/category, select tiles,
   and import a transparent image. Check startup/opening response, high DPI,
   empty search results, category/page reset, panel stability and export.

No Windows runtime/UI/microphone playback testing is claimed by this delivery.
