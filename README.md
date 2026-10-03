# ScreenAnote v6 — Compact Canvas screenshot editor and recorder

Windows x64 application plus complete C# Windows Forms source project. No AI services or screenshot uploads.
Local annotation and screen capture; URL browsing makes ordinary web requests.

## Run the included Windows application

1. Extract the entire ZIP.
2. Install **.NET 8 Desktop Runtime (Windows x64)** if absent:
   https://dotnet.microsoft.com/download/dotnet/8.0
3. Double-click **Start.cmd** or **App/ScreenAnote.exe**. Keep all files in App together.
4. URL capture also needs Microsoft Edge WebView2 Evergreen Runtime.

The included application was compiled successfully, but has not been launched on
Windows in the delivery environment.

## Build from source

1. Install the **.NET 8 SDK** on Windows (https://dotnet.microsoft.com/download/dotnet/8.0).
2. Extract this entire ZIP to a writable folder.
3. Double-click **Run.cmd**, or open **ScreenAnote.csproj** in a Visual Studio version supporting .NET 8 and run it.
4. The first build restores the Microsoft.Web.WebView2 NuGet package and needs internet access.
5. For URL capture, install **Microsoft Edge WebView2 Evergreen Runtime** if absent:
   https://developer.microsoft.com/microsoft-edge/webview2/

Build.cmd builds Release. Publish.cmd creates a self-contained Windows x64
executable in `publish` (no .NET installation required on the destination machine;
WebView2 Runtime is still required for URL capture). Keep any publish support files
if produced. The included App folder is framework-dependent; Publish.cmd produces a separate self-contained build.

## Compact Canvas interface

- **New capture:** region, window, all monitors, scrolling area, full web page and
  capture delay are grouped in the top command menu.
- **Floating bottom palette:** Select, Pen, Shapes (rectangle/ellipse), Arrow,
  Text, Highlight, Privacy (redact/pixelate), Stickers, Undo, Redo and More.
- **More:** Pan, Crop, imported PNG/GIF/icons and Fit.
- **Object controls:** selecting an annotation displays a floating appearance
  toolbar near it. Colour swatches/custom colour, stroke width, text size, Edit
  text, To front and Delete remain available. Drawing tools also show default
  appearance settings before placement.
- **Stickers:** opens a compact floating right drawer containing illustrated PNG
  stickers. The Featured tab starts with the mockup’s 16 artwork types: smile,
  heart, thumb, confetti, check, cross, star, fire, bulb, pin, target, arrow, speech
  bubble, information, question and laptop. Browse, Objects and Imported tabs show
  additional artwork. Search uses descriptive names.
  The drawer also provides image/GIF/icon import. Escape closes the drawer.
- **Zoom pill:** bottom-left minus, percentage, plus and Fit. Ctrl+wheel zoom and
  middle-button/Alt drag panning remain available.
- Integrated window header with minimize/maximize/close controls, drag-to-move,
  resize borders, keyboard focus indicators, labels and tooltips.
  Screenshot exports contain no floating application controls.

## v6 additions

- **Text box border:** select a text annotation and click **Border…** in its
  appearance toolbar. Choose None, Solid, Dashed, Dotted or Rounded, an independent
  border colour, and width from 1–20 original image pixels. Border padding keeps
  text clear of the outline. These settings also become defaults for new text.
  Text colour remains independent. Borders move/resize with text and survive
  duplicate, copy/paste and undo/redo. For a group use the appearance toolbar’s
  **⋯ → Text box border…**; it applies to text objects in the selection.
- **Counter size:** select Counter before placing a badge, or select an existing
  counter and change **Size** in the appearance toolbar (12–400 pixels). The
  selected badge remains centred and circular; the setting also controls new
  counters. Corner handles remain available for manual resize. Counter numbers
  scale with the badge. Size changes apply to all selected counters and undo/redo.
- **Mic toggle:** the recording controller has a microphone icon with **Mic off /
  Mic on**. It starts off; click to enable voice before or during recording. Click
  again to mute while continuing video. It records the default Windows input
  device, in mono 44.1 kHz, encoded as AAC inside the MP4. Muted intervals are
  silent. Pause stops microphone capture and removes paused time from both tracks.
  Stop closes the device before MP4 finalization. All processing remains local.
  If the microphone is unavailable or permission is denied, a message explains
  the problem and video continues with the mic off. Check Windows Settings →
  Microphone access and your default input device. System/speaker audio is not
  recorded. Recordings contain an audio track even when the mic stays off.
- **365 illustrated stickers / 18 categories:** smileys, icons, objects, animals,
  food, nature, weather, transport, travel, technology, work, sports, celebrations,
  health, arrows, callouts, shapes and symbols. Open Stickers → **Browse**, choose
  a category or All categories, search by name/category, and use ‹ / › to page.
  Featured keeps the compact mockup assortment; searching Featured searches the
  whole catalogue. There are no font-dependent tiles or sticker downloads.
  PNG artwork loads on demand, is cached locally in memory, and the picker creates
  only 32 tiles per page to keep opening and browsing responsive.
- The appearance toolbar keeps primary colour/width/font/counter controls compact.
  **⋯** contains Edit text, Text box border, Bring to front, Delete and Reset counter.

## v5 editing and UI improvements

- **Import clipboard image / Ctrl+V:** image data from Paint, browsers or other
  applications opens as a new screenshot ready to annotate. The existing document
  replacement prompt protects your current work. Editable ScreenAnote annotations
  take priority when that custom clipboard format is present. Text/search inputs
  keep ordinary text paste. New capture → Import clipboard image is also available.
- **Shift + resize:** hold Shift while dragging a selected corner handle to keep
  the original width/height ratio. Works for individual annotations and groups.
  Shift can be held before starting the resize or pressed during the drag.
- **Ctrl + click:** add annotations to a selection; Ctrl-click a selected object
  without dragging to remove it. A normal click on an unselected object selects
  it alone. An ordinary click inside the current selection keeps the group ready
  to move. Click blank canvas or press Escape to clear selection.
- **Shift + drag:** in Select mode, drag a selection box. All annotation bounds
  completely contained in that box become selected. Shift on a resize handle
  performs proportional resizing instead.
- **Group operations:** move/resize using the group outline and corner handles;
  colour, line width, font size, delete, bring to front and undo/redo work across
  the selection. Ctrl-drag duplicates the group; Ctrl+C/V copies/pastes all selected
  editable annotations while preserving their spacing.
- **Stable floating panels:** cards are opaque siblings of the canvas, with rounded
  clipping and canvas-painted shadows. Buttons never recursively paint their
  parents or the screenshot. The contextual toolbar stays fixed while you drag
  and relocates after release. Tool buttons repaint only when their state changes;
  sticker tiles cache their thumbnails. All sticker artwork is embedded locally.
- **Styling:** refined rounded cards, soft shadows, toolbar separators, circular
  selection handles and an accent outline on the selected illustrated sticker.
  The header adds a compact Record command while preserving the Compact Canvas
  layout. Actual Windows visual comparison remains pending.

## Region video recording

1. Click **Record** in the header (or New capture → Record selected region).
   The editor hides, preserving your document. Drag a region; Escape cancels.
2. The floating controller displays the region and MP4 dimensions. **Region…**
   reselects it before starting. Click **Start** to record visible screen pixels.
3. **Pause / Resume** excludes paused time from the saved video. **Stop** finishes
   encoding and opens an MP4 save dialog. Cancelling the save keeps the recording
   in this controller; **Save MP4** retries. Starting again or closing an unsaved
   recording asks before discarding it.
4. Close the controller to return to your screenshot editor.

The recorder produces H.264 MP4 with optional microphone voice at a target 15 fps, scaled to fit within
1920 × 1080 without upscaling. Dimensions are rounded down to even pixel counts
for H.264; odd-sized selections can have a tiny aspect difference. Very slow
capture/encoding can reduce frame rate; sample timestamps preserve elapsed time.
System audio and cursor overlays are not recorded. Microphone voice is optional. Keep the recorded content unobstructed.
Protected content may appear blank.

Windows Media Foundation supplies the encoder; no FFmpeg, codec download or
network service is used. Windows N editions require the Media Feature Pack.
Ctrl+Shift+F9 pauses/resumes and Ctrl+Shift+F10 stops when those global hotkeys can
be registered. The controller advertises them only when both are available.
It is positioned outside the region where possible and asks supported Windows
versions to exclude it from capture; verify exclusion on your Windows build.
The MP4 is encoded to a unique temporary file, copied to your chosen destination
on Save, then the session's temporary file is deleted when you close the controller
or start another recording. Abrupt termination can leave an incomplete temporary
file. No video frames are uploaded.

Native implementation follows Microsoft's [sink writer tutorial](https://learn.microsoft.com/en-us/windows/win32/medfound/tutorial--using-the-sink-writer-to-encode-video)
and [H.264 encoder documentation](https://learn.microsoft.com/en-us/windows/win32/medfound/h-264-video-encoder).

## Earlier editing shortcuts

- **Hold Space + left-drag:** temporary pan. Releasing Space stops this temporary
  gesture and restores the current drawing/select tool. It also resets when the
  application loses focus. Space typing in search/text inputs is preserved.
- **Counter:** select Counter in the bottom palette; each click adds 1, 2, 3… in
  a coloured circular badge. Counter mode stays active for repeated placement.
  ⋯ → Reset counter to 1 in the contextual bar, or More → Reset counter to 1, restarts the
  sequence. Existing badges stay unchanged. Moves, resizes and colour changes
  apply to badges. Undo/redo includes the sequence state.
- **Ctrl + drag:** in Select mode, hold Ctrl while dragging an annotation’s body
  to duplicate it. The original stays in place. Dragging corner handles resizes.
- **Ctrl+C / Ctrl+V:** with annotations selected, copy/paste those editable objects
  through the Windows clipboard. Repeated pastes offset the copies. Without a
  selection Ctrl+C copies the entire screenshot. The header Copy button always
  copies the flattened screenshot. Text/search inputs keep normal text copy/paste.
  The custom annotation format works between ScreenAnote windows; it is not a
  general vector format for other applications. Copied counters keep their number.

## Capture

- **Region:** delay, then drag over any monitor. Escape cancels.
- **Window:** choose a titled desktop window; it is restored and brought to front,
  then its visible screen rectangle is captured. Obscured parts, offscreen content,
  protected video, and elevated windows may not capture correctly. This mode uses
  visible pixels, not background window rendering.
- **Full screen:** all monitors including their virtual desktop arrangement.
- **Capture delay (inside New capture):** 0–10 seconds, useful for preparing a menu or popup. The app hides
  during screen capture. Minimum settling delay is 350 ms.
- **Scroll area:** start at the desired position in Chrome, Edge, Firefox or another
  scrolling application. Select only the scrollable viewport, excluding toolbars,
  scrollbars, sticky headers and overlays. ScreenAnote sends mouse wheel ticks, matches
  image overlaps and appends newly exposed rows. Escape stops and keeps the partial
  capture. It scrolls downward from the current position and leaves the page there.
  Maximum 100 steps; 40 megapixel output and tile-memory limits. No match or no
  movement stops capture and reports the reason. Keep other windows out of the way.
  Scroll areas with animation, repetitive rows, smooth scrolling, sticky elements,
  nested frames, or large wheel jumps can prevent reliable stitching. This is
  best-effort visible-pixel capture, not a guaranteed export of arbitrary pages.
- **Full web page:** opens a built-in Edge/WebView2 browser. Enter HTTP/HTTPS URL,
  wait for loading to complete, log in if needed and click **Capture full page**.
  It captures the full document using the DevTools screenshot API; it does not
  connect to or reuse Chrome/Firefox/Edge tabs or their login cookies. Login cookies
  persist in this app's own local browser profile. For lazy-loading pages, scroll
  through the page yourself before capture. Internal scroll containers, infinite
  feeds, video and some dynamic pages may require region capture. Limit: 30,000
  pixels per side and 40 megapixels. Browser profile is under
  `%LOCALAPPDATA%\ScreenAnote\BrowserProfile`.

## Edit

- **Ctrl + mouse wheel:** zoom in/out around the mouse pointer, up to 3200%.
- **Pan:** middle-button drag, Alt + left-button drag, or select Pan and left-drag.
  The wheel pans vertically; Shift + wheel pans horizontally.
- **Fit / Ctrl+0:** return to the fitted preview.

Exports retain original pixel resolution and contain no selection handles. Stroke
and text sizes are measured in original image pixels.

**Editable objects:** new annotations remain individual objects during the current
editing session. Select a tool to create an object; the editor returns to Select
after placement. Click an object in Select mode, drag its body to move, or drag
one of its four corner handles to resize. Use Colour, Stroke and Text/icon size to
restyle the selected object. Resizing text also scales its font; narrow text boxes
may wrap or clip text. Double-click text or use Edit text to change its content.
Delete removes the selected object; To front changes the stacking order. Escape
clears selection. Undo/redo includes moves, resize, restyling and deletion.

Only annotations added in this editing session are selectable. Opening a saved
PNG/JPEG/GIF imports its flattened pixels; it cannot recover annotation objects.
There is no editable-project save format in this version.

- **Pen, Arrow, Rectangle, Ellipse:** drag on the image.
- **Text:** click the image, enter multiline text, then Add text.
- **Highlight:** choose a colour (yellow is useful), then drag a translucent box.
- **Pixelate:** drag a box to obscure details with a mosaic.
- **Redact:** drag a box for an opaque black block. Prefer this for confidential
  content; pixelation may leave information recognizable. Exported files and
  clipboard images contain flattened edited pixels, with no undo history.
- **Crop:** drag the part to retain.
- **Stickers:** choose an illustrated sticker from the searchable drawer and click to place. Includes
  smileys, hearts, stars, check/cross, warning/info, arrows and more. Colour and
  text/icon size apply. The primary picker now contains 365 original illustrated PNG assets;
  their appearance does not depend on emoji fonts. Legacy text-symbol objects
  remain supported internally for existing editing behavior.
- **Import PNG/GIF/icon:** choose PNG, GIF, JPEG, BMP or ICO and click to place.
  Transparent PNGs retain their transparency. Imported images can be moved and
  resized; Colour applies a solid-colour tint preserving their alpha. Animated
  GIFs use their first frame; this is a still-image editor and GIF export is static.
  Imported stickers are limited to 4 megapixels each and 32 MB total per document.
- **Undo/Redo:** buttons or Ctrl+Z / Ctrl+Y. Undo snapshots are capped around
  160 MB, while retaining at least one undo operation; large images may require
  more memory. The screenshot background and annotation object list are restored together.

## Export

- **Save / Ctrl+S:** PNG, JPEG, BMP, TIFF or GIF. PNG preserves image fidelity;
  JPEG is lossy and GIF has a limited palette.
- **Copy / Ctrl+C:** copies the flattened image to Windows clipboard.
- **Open / Ctrl+O:** edit an existing image, up to 40 megapixels.

## Validation

See VALIDATION.md for the checks performed in the delivery environment and a
Windows smoke-test checklist. Windows capture, clipboard, DPI and browser behavior
must be verified on Windows; no Windows executable has been run in the delivery
Linux environment.

## Project layout

Program.cs — startup; MainForm.cs — UI and export; Capture.cs — screen/window/region;
ImageEditor.cs — viewport, group selection and undo; SelectionGeometry.cs — group bounds; Annotation.cs — object rendering;
ShapeGeometry.cs — geometry and hit testing; CanvasLayout.cs — floating layout bounds;
ModernControls.cs — theme, vector icons and floating controls; WindowChrome.cs — integrated titlebar;
GraphicStickers.cs — illustrated PNG catalogue; CounterSequence.cs — counter state; AudioFrames.cs — PCM timing/queue; WaveMicrophone.cs — default microphone capture;
AnnotationTransfer.cs — editable clipboard format; StickerCatalog.cs — named symbols; ScrollingCapture.cs — overlap stitching;
WebPageForm.cs — embedded browser and full-document screenshot; RecorderForm.cs — recording controller;
RegionRecorder.cs — capture worker; Mp4Writer.cs — native encoding; VideoFrames.cs — colour conversion/timing.

Geometry checks: `dotnet run --project Tests/GeometryTests.csproj -c Release`.

The illustrated assets are embedded in the executable. SVG sources and a PNG
regeneration script are included in Assets (CairoSVG is required only to regenerate
assets, never to run ScreenAnote). Tests include data-only annotation clone and
clipboard serialization; bitmap/GDI and Windows key/clipboard integration require
Windows. Tests suppress the platform analyzer for unused Windows drawing methods.

Windows native smoke test: `dotnet run --project WindowsTests/WindowsSmoke.csproj -c Release`.
This generates synthetic frames, exercises the built-in H.264/AAC encoders and MP4
finalization, validates the resulting container, then deletes the test MP4.
It also checks transparent sticker PNG serialization. It has been compiled but
cannot run in the Linux delivery environment. Play an actual saved recording to
verify decoded colours/orientation and pause timing on Windows.

For a Windows microphone smoke check, run:
`dotnet run --project WindowsTests/WindowsSmoke.csproj -c Release -- --microphone`
and speak when prompted. This checks real capture and mute in addition to the
synthetic audio/video test. Neither native test was run in the Linux environment.
Final microphone/video sync, device changes and Windows UI require Windows testing.
