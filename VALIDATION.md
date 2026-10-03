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
