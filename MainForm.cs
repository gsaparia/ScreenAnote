using System.Drawing.Imaging;
namespace ScreenAnote;
internal sealed class MainForm:Form
{
    private readonly ImageEditor editor=new();
    private readonly Panel workspace=new(){Dock=DockStyle.Fill,BackColor=Theme.Surface};
    private readonly RoundedCard palette=new(){Height=86},context=new(){Height=58},zoomCard=new(){Width=232,Height=48},drawer=new(){Width=266,Height=330,Visible=false};
    private readonly Label status=new(){AutoSize=false,ForeColor=Theme.Muted,BackColor=Theme.Surface,Text="Capture a screen or open an image to begin.",TextAlign=ContentAlignment.MiddleRight};
    private readonly NumericUpDown delay=new(){Minimum=0,Maximum=10,Value=1,Width=55};
    private readonly NumericUpDown stroke=new(){Minimum=1,Maximum=30,Value=4,Width=48};
    private readonly NumericUpDown size=new(){Minimum=6,Maximum=1000,Value=24,Width=60};
    private readonly Label zoomLabel=new(){Width=62,Height=30,Text="100%",TextAlign=ContentAlignment.MiddleCenter};
    private readonly Label strokeLabel=new(){Text="Width",AutoSize=true,Padding=new Padding(0,7,0,0)},sizeLabel=new(){Text="Size",AutoSize=true,Padding=new Padding(0,7,0,0)};
    private readonly ModernButton customColour=new(){Text="Colour",Width=64,Height=34};
    private readonly NumericUpDown counterDiameter=new(){Minimum=12,Maximum=400,Value=42,Width=55};
    private readonly Label counterSizeLabel=new(){Text="Size",AutoSize=true,Padding=new Padding(0,7,0,0)};
    private readonly ModernButton borderButton=new(){Text="Text box…",Width=82,Height=28};
    private readonly ContextMenuStrip objectMenu=new();
    private readonly List<(ModernButton Button,EditTool Tool)> toolButtons=new();
    private readonly ToolTip tips=new(){AutoPopDelay=7000};
    private readonly ContextMenuStrip captureMenu=new(),moreMenu=new(),shapeMenu=new(),privacyMenu=new();
    private bool syncing;
    private readonly GraphicStickers graphicStickers=new();
    private readonly List<GraphicStickers.Sticker> importedStickers=new();
    private readonly List<ModernButton> stickerTabs=new();
    private string stickerTab="Featured";
    private string selectedSticker="";
    private int stickerPage;
    private readonly ComboBox stickerCategory=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top};
    private readonly Label stickerCount=new(){Width=110,Height=26,TextAlign=ContentAlignment.MiddleCenter};
    private readonly ModernButton stickerPrevious=new(){Text="‹",Width=36,Height=26},stickerNext=new(){Text="›",Width=36,Height=26};
    
    private readonly Label counterLabel=new(){Width=55,Height=30,TextAlign=ContentAlignment.MiddleCenter};
    private readonly FlowLayoutPanel stickerGrid=new(){Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(2)};
    private readonly TextBox stickerSearch=new(){PlaceholderText="Search smile, arrow, heart…",Width=218};
    public MainForm()
    {
        Icon=AppIcon.Load();Text="ScreenAnote — Compact Canvas";FormBorderStyle=FormBorderStyle.None;Padding=new Padding(1);AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);Width=1440;Height=850;MinimumSize=new Size(880,620);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);ForeColor=Theme.Ink;BackColor=Theme.Surface;KeyPreview=true;
        BuildHeader();BuildPalette();BuildContext();BuildZoom();BuildDrawer();
        editor.BackColor=Theme.Surface;editor.ForeColor=Theme.Muted;workspace.Controls.Add(editor);workspace.Controls.Add(palette);workspace.Controls.Add(context);workspace.Controls.Add(zoomCard);workspace.Controls.Add(drawer);
        Controls.Add(workspace);workspace.BringToFront();
        editor.SendToBack();workspace.Resize+=(_,_)=>Arrange();
        editor.RequestText=initial=>Prompt("Annotation text",initial);
        editor.SelectionChanged+=(_,_)=>SyncSelection();
        editor.Changed+=(_,_)=>{zoomLabel.Text=$"{editor.ZoomPercent:0}%";status.Text=editor.Image==null?"Ctrl+O open  ·  New capture to begin":$"{editor.Image.Width} × {editor.Image.Height} px  ·  Ctrl+wheel zoom  ·  Hold Space to pan";if(!editor.GestureActive){UpdateTools();PlaceContext();}};
        bool EntryFocused()=>ActiveControl is TextBoxBase or NumericUpDown or ComboBox;
        KeyDown+=(_,e)=>{
            if(e.KeyCode==Keys.Space && !EntryFocused()){editor.SetSpacePan(true);e.Handled=true;return;}
            if(EntryFocused() && e.KeyCode is Keys.C or Keys.V)return;
            if(e.KeyCode==Keys.Delete && editor.Focused){editor.DeleteSelected();e.SuppressKeyPress=true;return;}
            if(e.KeyCode==Keys.Escape){drawer.Visible=false;editor.ClearSelection();Arrange();e.SuppressKeyPress=true;return;}
            if(!e.Control)return;switch(e.KeyCode){case Keys.S:Safe(Save);break;case Keys.C:Safe(()=>{if(!editor.CopySelected())Copy();else status.Text="Annotation copied — Ctrl+V to paste.";});break;case Keys.V:Safe(PasteClipboard);break;case Keys.Z:editor.Undo();break;case Keys.Y:editor.Redo();break;case Keys.O:Safe(Open);break;case Keys.D0:editor.FitView();break;default:return;}e.SuppressKeyPress=true;
        };
        KeyPress+=(_,e)=>{if(e.KeyChar==' ' && !EntryFocused())e.Handled=true;};
        KeyUp+=(_,e)=>{if(e.KeyCode==Keys.Space){editor.SetSpacePan(false);e.Handled=true;}};
        Deactivate+=(_,_)=>editor.SetSpacePan(false);
        FormClosing+=(_,e)=>{if(editor.Image!=null && MessageBox.Show(this,"Close ScreenAnote? Save or copy your screenshot first if needed.","Close",MessageBoxButtons.OKCancel)==DialogResult.Cancel)e.Cancel=true;};
        Shown+=(_,_)=>Arrange();
    }
    private ModernButton Button(string text,string symbol,int width,Action action,bool vertical=false,bool primary=false)
    {
        var b=new ModernButton{Text=vertical?"":text,Symbol=symbol,Width=width,Height=vertical?62:38,Vertical=vertical,Primary=primary,AccessibleName=text,Margin=new Padding(2)};b.Click+=(_,_)=>Safe(action);if(vertical)Hint(b,text.Length>0?text:symbol);return b;
    }
    private void Hint(Control c,string text){tips.SetToolTip(c,text);c.AccessibleDescription=text;}
    private void BuildHeader()
    {
        var header=new Panel{Dock=DockStyle.Top,Height=68,BackColor=Color.FromArgb(245,248,253),Padding=new Padding(20,12,12,10)};
        var brandPanel=new Panel{Dock=DockStyle.Left,Width=200};
        var brandIcon=Button("","capture",34,()=>{});brandIcon.Primary=true;brandIcon.Height=34;brandIcon.Location=new Point(0,4);
        var brand=new Label{Text="ScreenAnote",Font=new Font("Segoe UI",12,FontStyle.Bold),ForeColor=Theme.Ink,Location=new Point(46,3),Size=new Size(154,38),TextAlign=ContentAlignment.MiddleLeft};
        brandPanel.Controls.Add(brandIcon);brandPanel.Controls.Add(brand);
        void Drag(object? sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)WindowChrome.Drag(this);}
        header.MouseDown+=Drag;brand.MouseDown+=Drag;brandIcon.MouseDown+=Drag;
        header.DoubleClick+=(_,_)=>ToggleMaximize();brand.DoubleClick+=(_,_)=>ToggleMaximize();
        var commands=new FlowLayoutPanel{Dock=DockStyle.Right,Width=602,WrapContents=false};
        ModernButton? capture=null;capture=Button("New capture ▾","capture",156,()=>captureMenu.Show(capture!,new Point(0,capture!.Height)));capture.Outline=true;commands.Controls.Add(capture);
        commands.Controls.Add(Button("Record","video",96,()=>_ = RecordVideo()));
        var save=Button("Save","save",96,Save,primary:true);commands.Controls.Add(save);var copy=Button("Copy","copy",96,Copy);copy.Outline=true;commands.Controls.Add(copy);
        commands.Controls.Add(Button("−","",38,()=>WindowState=FormWindowState.Minimized));commands.Controls.Add(Button("□","",38,ToggleMaximize));commands.Controls.Add(Button("×","",38,Close));
        header.Controls.Add(brandPanel);header.Controls.Add(commands);Controls.Add(header);
        async Task Run(Func<Task> action){try{await action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"ScreenAnote",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
        void CaptureItem(string text,string mode){captureMenu.Items.Add(text,null,async(_,_)=>await Run(()=>DoCapture(mode)));}
        CaptureItem("Selected region","region");CaptureItem("Window","window");CaptureItem("Full screen (all monitors)","full");CaptureItem("Scrolling browser area","scroll");CaptureItem("Full webpage from Browser","scroll");
        captureMenu.Items.Add("Full web page from URL",null,(_,_)=>Safe(()=>{if(!ConfirmReplace())return;using var form=new WebPageForm();if(form.ShowDialog(this)==DialogResult.OK && form.Result!=null)Replace(form.Result);}));
        captureMenu.Items.Add("Open image…",null,(_,_)=>Safe(Open));captureMenu.Items.Add("Import clipboard image (Ctrl+V)",null,(_,_)=>Safe(PasteClipboard));captureMenu.Items.Add("Record selected region…",null,async(_,_)=>await Run(RecordVideo));captureMenu.Items.Add(new ToolStripSeparator());captureMenu.Items.Add(new ToolStripLabel("Capture delay (seconds)"));captureMenu.Items.Add(new ToolStripControlHost(delay));
        foreach(var menu in new[]{captureMenu,moreMenu,shapeMenu,privacyMenu,objectMenu}){menu.Font=Font;menu.BackColor=Color.White;menu.ForeColor=Theme.Ink;}
    }
    private void ToggleMaximize(){MaximizedBounds=Screen.FromHandle(Handle).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);WindowChrome.Round(this);}
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);var old=Region;
        if(WindowState==FormWindowState.Maximized)Region=null;
        else if(ClientSize.Width>24 && ClientSize.Height>24){using var shape=Theme.Round(new RectangleF(0,0,ClientSize.Width,ClientSize.Height),14);Region=new Region(shape);}
        old?.Dispose();
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var border=new Pen(Theme.Line);using var path=Theme.Round(new RectangleF(.5f,.5f,ClientSize.Width-1,ClientSize.Height-1),WindowState==FormWindowState.Maximized?1:14);e.Graphics.DrawPath(border,path);}
    protected override void WndProc(ref Message message)
    {
        const int hitTest=0x84;base.WndProc(ref message);
        if(message.Msg!=hitTest || WindowState!=FormWindowState.Normal)return;
        var p=PointToClient(Cursor.Position);int edge=Math.Max(6,DeviceDpi/16);
        bool l=p.X<edge,r=p.X>=ClientSize.Width-edge,t=p.Y<edge,b=p.Y>=ClientSize.Height-edge;
        int hit=t&&l?13:t&&r?14:b&&l?16:b&&r?17:l?10:r?11:t?12:b?15:0;if(hit!=0)message.Result=(IntPtr)hit;
    }
    private void SetTool(EditTool tool)
    {
        if(tool!=EditTool.Select)editor.ClearSelection();editor.Tool=tool;UpdateTools();PlaceContext();editor.Focus();
        status.Text=tool is EditTool.Text or EditTool.Sticker?"Click on the screenshot to place "+tool.ToString().ToLowerInvariant():tool==EditTool.Select?"Click an annotation to select; drag its body or corner handles.":"Drag on the screenshot to use "+tool.ToString().ToLowerInvariant();
    }
    private void BuildPalette()
    {
        palette.Width=790;palette.Padding=new Padding(11);var row=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Padding=Padding.Empty};palette.Controls.Add(row);
        void Tool(string name,string icon,EditTool tool){var b=Button(name,icon,60,()=>SetTool(tool),true);toolButtons.Add((b,tool));Hint(b,name+" — select a tool, then click or drag on the screenshot.");row.Controls.Add(b);}
        Tool("Select","select",EditTool.Select);Tool("Pen","pen",EditTool.Pen);
        ModernButton? shapes=null;shapes=Button("Rectangle","shape",64,()=>shapeMenu.Show(shapes!,new Point(0,-shapeMenu.PreferredSize.Height)),true);row.Controls.Add(shapes);toolButtons.Add((shapes,EditTool.Rectangle));
        shapeMenu.Items.Add("Rectangle",null,(_,_)=>SetTool(EditTool.Rectangle));shapeMenu.Items.Add("Ellipse",null,(_,_)=>SetTool(EditTool.Ellipse));
        Tool("Arrow","arrow",EditTool.Arrow);Tool("Text","text",EditTool.Text);Tool("Highlight","highlight",EditTool.Highlight);
        ModernButton? privacy=null;privacy=Button("Redact","redact",60,()=>privacyMenu.Show(privacy!,new Point(0,-privacyMenu.PreferredSize.Height)),true);row.Controls.Add(privacy);toolButtons.Add((privacy,EditTool.Redact));
        privacyMenu.Items.Add("Redact (opaque block)",null,(_,_)=>SetTool(EditTool.Redact));privacyMenu.Items.Add("Pixelate",null,(_,_)=>SetTool(EditTool.Pixelate));
        row.Controls.Add(Button("Stickers","sticker",60,()=>{drawer.Visible=!drawer.Visible;Arrange();},true));
        Tool("Counter","counter",EditTool.Counter);
        row.Controls.Add(new Panel{Width=1,Height=52,BackColor=Theme.Line,Margin=new Padding(6,5,6,0)});
        row.Controls.Add(Button("","undo",36,editor.Undo,true));row.Controls.Add(Button("","redo",36,editor.Redo,true));
        ModernButton? more=null;more=Button("More","⋯",40,()=>moreMenu.Show(more!,new Point(0,-moreMenu.PreferredSize.Height)),true);row.Controls.Add(more);
        moreMenu.Items.Add("Reset counter to 1",null,(_,_)=>editor.ResetCounter());moreMenu.Items.Add("Pan tool",null,(_,_)=>SetTool(EditTool.Pan));moreMenu.Items.Add("Crop",null,(_,_)=>SetTool(EditTool.Crop));moreMenu.Items.Add("Import PNG / GIF / icon",null,(_,_)=>Safe(ImportSticker));moreMenu.Items.Add("Fit screenshot",null,(_,_)=>editor.FitView());
        Hint(more,"Pan, crop, image/GIF import and fit controls");
        moreMenu.Items.Add("Import clipboard image",null,(_,_)=>Safe(PasteClipboard));
    }
    private void BuildContext()
    {
        context.Width=520;context.Padding=new Padding(12);var row=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};context.Controls.Add(row);
        foreach(var color in new[]{Color.Red,Color.Gold,Color.RoyalBlue,Color.SeaGreen,Color.Black})
        {
            var swatch=new ModernButton{Width=24,Height=26,BackColor=color,Text="",AccessibleName=color.Name,Margin=new Padding(3,2,3,2)};swatch.Click+=(_,_)=>{editor.Ink=color;customColour.BackColor=color;customColour.Invalidate();editor.Focus();};Hint(swatch,"Change selected annotation colour to "+color.Name);row.Controls.Add(swatch);
        }
        customColour.Text="…";customColour.Width=30;customColour.Height=28;
        customColour.Click+=(_,_)=>Safe(()=>{using var dialog=new ColorDialog{Color=editor.Selected?.Color??editor.Ink};if(dialog.ShowDialog(this)==DialogResult.OK){editor.Ink=dialog.Color;customColour.BackColor=dialog.Color;customColour.Invalidate();}});row.Controls.Add(customColour);
        row.Controls.Add(strokeLabel);row.Controls.Add(stroke);row.Controls.Add(sizeLabel);row.Controls.Add(size);
        stroke.ValueChanged+=(_,_)=>{if(!syncing)editor.Stroke=(int)stroke.Value;};size.ValueChanged+=(_,_)=>{if(!syncing)editor.FontSize=(int)size.Value;};
        borderButton.Click+=(_,_)=>EditTextBorder();row.Controls.Add(borderButton);Hint(borderButton,"Configure text box background, border style, width and colour");
        row.Controls.Add(counterSizeLabel);row.Controls.Add(counterDiameter);counterDiameter.ValueChanged+=(_,_)=>{if(!syncing)editor.CounterSize=(int)counterDiameter.Value;};Hint(counterDiameter,"Counter diameter in original image pixels (12–400)");row.Controls.Add(counterLabel);
        ModernButton? actions=null;actions=Button("","⋯",34,()=>objectMenu.Show(actions!,new Point(0,actions!.Height)));actions.Height=28;row.Controls.Add(actions);Hint(actions,"Edit text, text border, bring to front, delete and reset counter");
        var edit=objectMenu.Items.Add("Edit text…",null,(_,_)=>Safe(editor.EditSelectedText));var border=objectMenu.Items.Add("Text box border…",null,(_,_)=>EditTextBorder());var background=objectMenu.Items.Add("Text box background…",null,(_,_)=>EditTextBackground());var front=objectMenu.Items.Add("Bring selection to front",null,(_,_)=>editor.BringSelectedToFront());var delete=objectMenu.Items.Add("Delete selection",null,(_,_)=>editor.DeleteSelected());objectMenu.Items.Add(new ToolStripSeparator());objectMenu.Items.Add("Reset counter to 1",null,(_,_)=>editor.ResetCounter());
        objectMenu.Opening+=(_,_)=>{edit.Enabled=editor.SelectionCount==1&&editor.Selected?.Kind==EditTool.Text;border.Enabled=editor.SelectionHasText||editor.Tool==EditTool.Text;background.Enabled=editor.Selected?.Kind==EditTool.Text||editor.Tool==EditTool.Text;front.Enabled=delete.Enabled=editor.Selected!=null;};
        Hint(stroke,"Line width in original image pixels");Hint(size,"Font size in original image pixels");context.Visible=false;
    }
    private void EditTextBackground()
    {
        var color=editor.Selected?.Kind==EditTool.Text?editor.Selected.TextBackground:editor.DefaultTextBackground;
        using var form=new Form{Text="Text box background",ClientSize=new Size(310,150),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font};
        var transparent=new CheckBox{Text="Transparent background",Checked=color.A==0,Location=new Point(20,18),AutoSize=true};
        var choose=new ModernButton{Text="Choose colour…",Location=new Point(20,50),Size=new Size(160,34),BackColor=color.A==0?Color.White:color};
        choose.Click+=(_,_)=>{using var dialog=new ColorDialog{FullOpen=true,Color=color.A==0?Color.White:color};if(dialog.ShowDialog(form)==DialogResult.OK){color=dialog.Color;transparent.Checked=false;choose.BackColor=color;choose.Invalidate();}};
        var apply=new ModernButton{Text="Apply",Primary=true,Location=new Point(110,100),Size=new Size(80,32),DialogResult=DialogResult.OK};
        var cancel=new ModernButton{Text="Cancel",Location=new Point(205,100),Size=new Size(80,32),DialogResult=DialogResult.Cancel};form.Controls.AddRange([transparent,choose,apply,cancel]);form.AcceptButton=apply;form.CancelButton=cancel;
        if(form.ShowDialog(this)==DialogResult.OK)editor.SetTextBackground(transparent.Checked?Color.Transparent:color.A==0?Color.White:color);
    }
    private void EditTextBorder()
    {
        var a=editor.Selected?.Kind==EditTool.Text?editor.Selected:null;
        using var form=new Form{Text="Text box appearance",ClientSize=new Size(320,290),Font=Font,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.White};
        var styles=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(110,20),Width=185};styles.Items.AddRange(Enum.GetNames<TextBorderStyle>());styles.SelectedIndex=(int)(a?.BorderStyle??editor.DefaultBorder);
        var width=new NumericUpDown{Minimum=1,Maximum=20,Value=(decimal)(a?.BorderWidth??editor.DefaultBorderWidth),Location=new Point(110,66),Width=80};
        var color=a?.BorderColor??editor.DefaultBorderColor;var swatch=new ModernButton{Text="Colour…",Location=new Point(110,108),Width=120,Height=32,BackColor=color};swatch.Click+=(_,_)=>{using var dialog=new ColorDialog{Color=color};if(dialog.ShowDialog(form)==DialogResult.OK){color=dialog.Color;swatch.BackColor=color;swatch.Invalidate();}};
        foreach(var (label,y) in new[]{("Style",23),("Width",69),("Colour",113)})form.Controls.Add(new Label{Text=label,Location=new Point(20,y),AutoSize=true});
        var background=a?.TextBackground??editor.DefaultTextBackground;
        var transparent=new CheckBox{Text="Transparent background",Checked=background.A==0,Location=new Point(20,153),AutoSize=true};
        var bg=new ModernButton{Text="Background…",Location=new Point(110,187),Size=new Size(150,32),BackColor=background.A==0?Color.White:background};bg.Click+=(_,_)=>{using var d=new ColorDialog{FullOpen=true,Color=background.A==0?Color.White:background};if(d.ShowDialog(form)==DialogResult.OK){background=d.Color;transparent.Checked=false;bg.BackColor=background;bg.Invalidate();}};
        var apply=new ModernButton{Text="Apply",Primary=true,Location=new Point(110,238),Width=90,Height=34,DialogResult=DialogResult.OK};var cancel=new ModernButton{Text="Cancel",Location=new Point(210,238),Width=85,Height=34,DialogResult=DialogResult.Cancel};
        form.Controls.AddRange([styles,width,swatch,transparent,bg,apply,cancel]);form.AcceptButton=apply;form.CancelButton=cancel;
        if(form.ShowDialog(this)==DialogResult.OK){editor.SetTextBorder((TextBorderStyle)styles.SelectedIndex,(float)width.Value,color);editor.SetTextBackground(transparent.Checked?Color.Transparent:background.A==0?Color.White:background);}
    }
    private void BuildZoom()
    {
        zoomCard.Padding=new Padding(5);var row=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=Padding.Empty};zoomCard.Controls.Add(row);
        var minus=Button("−","",32,()=>editor.ZoomBy(1/1.2f));minus.Height=30;row.Controls.Add(minus);row.Controls.Add(zoomLabel);
        var plus=Button("+","",32,()=>editor.ZoomBy(1.2f));plus.Height=30;row.Controls.Add(plus);row.Controls.Add(new Panel{Width=1,Height=24,BackColor=Theme.Line,Margin=new Padding(2,5,2,0)});
        var fit=Button("Fit","fit",72,editor.FitView);fit.Height=30;row.Controls.Add(fit);Hint(fit,"Fit screenshot — Ctrl+0");
    }
    private void BuildDrawer()
    {
        drawer.Padding=new Padding(16,14,16,18);var title=new Label{Text="Stickers",Font=new Font("Segoe UI",10,FontStyle.Bold),Dock=DockStyle.Top,Height=32};
        var close=Button("×","",24,()=>{drawer.Visible=false;Arrange();});close.Height=24;close.Location=new Point(drawer.Width-44,14);Hint(close,"Close sticker drawer");
        var tabs=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38,WrapContents=false};
        foreach(var (category,icon) in new[]{("Featured","sticker"),("Browse","heart"),("Objects","star"),("Imported","flag")})
        {var tab=Button("",icon,48,()=>{stickerTab=category;stickerPage=0;FillStickers();});tab.Height=32;tab.Underline=true;tab.Tag=category;Hint(tab,category);stickerTabs.Add(tab);tabs.Controls.Add(tab);}
        var filters=new Panel{Dock=DockStyle.Top,Height=30};stickerSearch.PlaceholderText="Search stickers…";stickerSearch.Width=210;filters.Controls.Add(stickerSearch);stickerSearch.TextChanged+=(_,_)=>{stickerPage=0;FillStickers();};
        stickerCategory.Items.Add("All categories");stickerCategory.Items.AddRange(graphicStickers.Items.Select(s=>s.Category).Distinct().Order().Cast<object>().ToArray());stickerCategory.SelectedIndex=0;stickerCategory.SelectedIndexChanged+=(_,_)=>{stickerPage=0;FillStickers();};
        var navigation=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=29,WrapContents=false};navigation.Controls.AddRange([stickerPrevious,stickerCount,stickerNext]);stickerPrevious.Click+=(_,_)=>{stickerPage--;FillStickers();};stickerNext.Click+=(_,_)=>{stickerPage++;FillStickers();};
        var importButton=Button("Import PNG / GIF / icon","",220,ImportSticker);importButton.Dock=DockStyle.Bottom;importButton.Height=32;
        drawer.Controls.Add(stickerGrid);drawer.Controls.Add(stickerCategory);drawer.Controls.Add(filters);drawer.Controls.Add(tabs);drawer.Controls.Add(title);drawer.Controls.Add(navigation);drawer.Controls.Add(importButton);drawer.Controls.Add(close);close.BringToFront();FillStickers();
    }
    private void FillStickers()
    {
        stickerGrid.SuspendLayout();foreach(Control c in stickerGrid.Controls.Cast<Control>().ToArray())c.Dispose();stickerGrid.Controls.Clear();
        foreach(var tab in stickerTabs){tab.Selected=(string?)tab.Tag==stickerTab;tab.Invalidate();}
        string search=stickerSearch.Text.Trim();
        var featured=new[]{"Smile","Heart","Thumb up","Confetti","Check","Cross","Gold star","Fire","Light bulb","Push pin","Target","Arrow right","Speech bubble","Information","Question","Laptop"};
        stickerCategory.Visible=stickerTab=="Browse";
        var items=stickerTab=="Imported"?importedStickers:stickerTab=="Featured"?featured.Select(name=>graphicStickers.Items.First(s=>s.Name==name)).Concat(graphicStickers.Items.Where(s=>s.Category=="Smileys"&&s.Name!="Smile")).ToList():stickerTab=="Browse"?graphicStickers.Items.Where(s=>stickerCategory.SelectedIndex<=0||s.Category==(string?)stickerCategory.SelectedItem).ToList():graphicStickers.Items.Where(s=>s.Category==stickerTab).ToList();
        if(search.Length>0&&stickerTab=="Featured")items=graphicStickers.Items;
        var matches=items.Where(s=>search.Length==0||s.Name.Contains(search,StringComparison.OrdinalIgnoreCase)||s.Category.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();
        const int pageSize=32;stickerPage=Math.Clamp(stickerPage,0,Math.Max(0,(matches.Count-1)/pageSize));stickerPrevious.Enabled=stickerPage>0;stickerNext.Enabled=(stickerPage+1)*pageSize<matches.Count;stickerCount.Text=matches.Count==0?"0 stickers":$"{stickerPage*pageSize+1}–{Math.Min(matches.Count,(stickerPage+1)*pageSize)} / {matches.Count}";
        foreach(var item in matches.Skip(stickerPage*pageSize).Take(pageSize))
        {
            var b=new ModernButton{Artwork=item.Image,Selected=item.Name==selectedSticker,Text="",Width=46,Height=46,BackColor=Color.FromArgb(240,244,250),AccessibleName=item.Name,Margin=new Padding(3)};
            b.Click+=(_,_)=>Safe(()=>{if(editor.Image==null){MessageBox.Show(this,"Open or capture an image first.");return;}selectedSticker=item.Name;foreach(var tile in stickerGrid.Controls.OfType<ModernButton>())tile.Selected=tile.AccessibleName==selectedSticker;editor.SetImageSticker(new Bitmap(item.Image));UpdateTools();PlaceContext();editor.Focus();status.Text="Click the screenshot to place: "+item.Name;});Hint(b,item.Name);stickerGrid.Controls.Add(b);
        }
        if(stickerGrid.Controls.Count==0)stickerGrid.Controls.Add(new Label{Text=stickerTab=="Imported"?"Import an image or GIF to add a sticker.":"No matching stickers",MaximumSize=new Size(200,80),AutoSize=true,ForeColor=Theme.Muted});
        stickerGrid.ResumeLayout();
    }
    private void RegisterImported(Bitmap image,string name)
    {
        if(importedStickers.Count>=16){importedStickers[0].Image.Dispose();importedStickers.RemoveAt(0);}
        // Retain a bounded 512px illustration for the imported picker.
        float scale=Math.Min(1,512f/Math.Max(image.Width,image.Height));var copy=new Bitmap(Math.Max(1,(int)(image.Width*scale)),Math.Max(1,(int)(image.Height*scale)));using(var g=Graphics.FromImage(copy))g.DrawImage(image,new Rectangle(Point.Empty,copy.Size));
        importedStickers.Add(new(name,"Imported",copy));if(stickerTab=="Imported")FillStickers();
    }
    private void SyncSelection()
    {
        if(editor.GestureActive)return;
        syncing=true;try{var a=editor.Selected;customColour.BackColor=a?.Color??editor.Ink;stroke.Value=Math.Clamp((decimal)(a?.Stroke??editor.Stroke),stroke.Minimum,stroke.Maximum);size.Value=Math.Clamp((decimal)(a?.FontSize??editor.FontSize),size.Minimum,size.Maximum);counterDiameter.Value=Math.Clamp((decimal)(a?.Kind==EditTool.Counter?a.Bounds.Width:editor.CounterSize),counterDiameter.Minimum,counterDiameter.Maximum);}finally{syncing=false;}UpdateTools();PlaceContext();
    }
    private void UpdateTools()
    {
        foreach(var (button,tool) in toolButtons){button.Selected=editor.Tool==tool || tool==EditTool.Rectangle && editor.Tool==EditTool.Ellipse || tool==EditTool.Redact && editor.Tool==EditTool.Pixelate;}
    }
    private void Arrange()
    {
        int width=workspace.ClientSize.Width,height=workspace.ClientSize.Height;
        float dpi=DeviceDpi/96f;var area=new Size((int)(workspace.Width/dpi),(int)(workspace.Height/dpi));
        palette.Bounds=CanvasLayout.Scale(CanvasLayout.Palette(area),dpi);zoomCard.Bounds=CanvasLayout.Scale(CanvasLayout.Zoom(area),dpi);
        status.SetBounds(260,Math.Max(8,height-54),Math.Max(0,width-276),40);
        drawer.Bounds=CanvasLayout.Scale(CanvasLayout.Drawer(area),dpi);PlaceContext();
        palette.BringToFront();zoomCard.BringToFront();drawer.BringToFront();context.BringToFront();editor.Invalidate();
    }
    private void PlaceContext()
    {
        if(editor.GestureActive)return;
        bool editable=editor.Image!=null && (editor.Selected!=null || editor.Tool is EditTool.Counter or EditTool.Pen or EditTool.Arrow or EditTool.Rectangle or EditTool.Ellipse or EditTool.Text or EditTool.Highlight or EditTool.Sticker);
        context.Visible=editable;if(!editable)return;
        bool text=editor.SelectionHasText || editor.Selected==null && editor.Tool is EditTool.Text or EditTool.Sticker;
        bool counting=editor.Tool==EditTool.Counter || editor.SelectionHasCounter;counterDiameter.Visible=counterSizeLabel.Visible=counting;counterLabel.Text="Next: "+editor.NextCounter;counterLabel.Visible=counting&&editor.SelectionCount<=1;
        borderButton.Visible=editor.SelectionCount<=1&&(editor.Selected?.Kind==EditTool.Text||editor.Tool==EditTool.Text);size.Visible=sizeLabel.Visible=text;stroke.Visible=strokeLabel.Visible=(!text||editor.SelectionCount>1) && !counting;
        float dpi=DeviceDpi/96f;var area=new Size((int)(workspace.Width/dpi),(int)(workspace.Height/dpi));var selected=editor.SelectedScreenBounds;
        RectangleF? logical=editor.Selected==null?null:new RectangleF(selected.X/dpi,selected.Y/dpi,selected.Width/dpi,selected.Height/dpi);
        context.Bounds=CanvasLayout.Scale(CanvasLayout.Context(area,drawer.Visible,logical),dpi);
    }
    protected override void Dispose(bool disposing){if(disposing){tips.Dispose();captureMenu.Dispose();moreMenu.Dispose();shapeMenu.Dispose();privacyMenu.Dispose();objectMenu.Dispose();}base.Dispose(disposing);if(disposing){graphicStickers.Dispose();foreach(var item in importedStickers)item.Image.Dispose();}}
    private void Safe(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"ScreenAnote");}}
    private bool ConfirmReplace()=>editor.Image==null || MessageBox.Show(this,"Replace the current screenshot? Save or copy it first if needed.","New image",MessageBoxButtons.OKCancel)==DialogResult.OK;
    private void Replace(Bitmap bitmap){editor.LoadImage(bitmap);}
    private bool recordingWindowOpen;
    private async Task RecordVideo()
    {
        if(recordingWindowOpen)return;recordingWindowOpen=true;Hide();
        try
        {
            await Task.Delay(350);var area=ScreenCapture.SelectRegion();if(!area.HasValue)return;
            VideoFrames.OutputSize(area.Value.Size);using var form=new RecorderForm(area.Value);form.ShowDialog();
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"ScreenAnote recording",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{recordingWindowOpen=false;Show();Activate();}
    }
    private async Task DoCapture(string mode)
    {
        if(!ConfirmReplace())return;
        if(mode=="scroll")MessageBox.Show(this,"Open your browser and position the page at the starting point. Select only scrolling content, excluding browser bars, scrollbars and fixed headers. Keep the browser unobstructed. Press Escape to stop. The page will remain at the final scroll position.","Scrolling capture");
        IntPtr window=mode=="window"?ScreenCapture.PickWindow():IntPtr.Zero;if(mode=="window" && window==IntPtr.Zero)return;
        Hide();
        try
        {
            if(window!=IntPtr.Zero){Native.ShowWindow(window,9);Native.SetForegroundWindow(window);}
            await Task.Delay(Math.Max(350,(int)delay.Value*1000));
            Rectangle? area;
            if(mode=="full")area=SystemInformation.VirtualScreen;
            else if(mode=="window"){if(!Native.GetWindowRect(window,out var bounds))throw new InvalidOperationException("Window is no longer available.");area=Rectangle.Intersect(bounds.Bounds,SystemInformation.VirtualScreen);}
            else area=ScreenCapture.SelectRegion();
            if(area==null)return;
            await Task.Delay(200);
            if(mode=="scroll"){var result=await ScrollingCapture.Run(area.Value);Replace(result.Image);status.Text=result.Note;if(result.Note.Contains("partial",StringComparison.OrdinalIgnoreCase)||result.Note.Contains("could not",StringComparison.OrdinalIgnoreCase))MessageBox.Show(this,result.Note,"Scrolling capture");}
            else Replace(ScreenCapture.Screen(area.Value));
        }
        finally{Show();Activate();}
    }
    private void Open()
    {
        if(!ConfirmReplace())return;using var d=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff"};if(d.ShowDialog(this)!=DialogResult.OK)return;
        using var source=System.Drawing.Image.FromFile(d.FileName);if((long)source.Width*source.Height>40_000_000)throw new InvalidOperationException("Image exceeds 40 megapixels.");Replace(new Bitmap(source));
    }
    private void Save()
    {
        if(editor.Image==null)return;using var d=new SaveFileDialog{Filter="PNG image|*.png|JPEG image|*.jpg|Bitmap image|*.bmp|TIFF image|*.tif|GIF image|*.gif",FileName="Screenshot_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"),AddExtension=true};
        if(d.ShowDialog(this)!=DialogResult.OK)return;var formats=new[]{ImageFormat.Png,ImageFormat.Jpeg,ImageFormat.Bmp,ImageFormat.Tiff,ImageFormat.Gif};using var rendered=editor.RenderImage();rendered.Save(d.FileName,formats[d.FilterIndex-1]);status.Text="Saved: "+d.FileName;
    }
    private void PasteClipboard()
    {
        if(editor.PasteAnnotation())return;
        if(!Clipboard.ContainsImage())return;
        using var source=Clipboard.GetImage();if(source==null)return;
        if((long)source.Width*source.Height>40_000_000)throw new InvalidOperationException("Clipboard image exceeds 40 megapixels.");
        if(!ConfirmReplace())return;Replace(new Bitmap(source));status.Text="Clipboard image imported — ready to annotate.";
    }
    private void Copy(){if(editor.Image==null)return;using var rendered=editor.RenderImage();Clipboard.SetImage(rendered);status.Text="Screenshot copied to clipboard.";}
    private void ImportSticker()
    {
        if(editor.Image==null){MessageBox.Show(this,"Capture or open a screenshot first.");return;}
        using var dialog=new OpenFileDialog{Filter="Sticker/image/icon|*.png;*.gif;*.jpg;*.jpeg;*.bmp;*.ico"};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        if(Path.GetExtension(dialog.FileName).Equals(".ico",StringComparison.OrdinalIgnoreCase)){using var icon=new Icon(dialog.FileName,new Size(128,128));using var bitmap=icon.ToBitmap();RegisterImported(bitmap,Path.GetFileNameWithoutExtension(dialog.FileName));editor.SetImageSticker(new Bitmap(bitmap));status.Text="Click image to place imported icon.";return;}
        using var source=System.Drawing.Image.FromFile(dialog.FileName);
        if((long)source.Width*source.Height>4_000_000)throw new InvalidOperationException("Sticker exceeds 4 megapixels.");
        if(Path.GetExtension(dialog.FileName).Equals(".gif",StringComparison.OrdinalIgnoreCase) && source.FrameDimensionsList.Contains(FrameDimension.Time.Guid) && source.GetFrameCount(FrameDimension.Time)>1)source.SelectActiveFrame(FrameDimension.Time,0);
        using var loaded=new Bitmap(source);RegisterImported(loaded,Path.GetFileNameWithoutExtension(dialog.FileName));editor.SetImageSticker(new Bitmap(loaded));status.Text="Click image to place imported sticker. GIF exports use the first frame.";
    }
    private string? Prompt(string title,string? initial=null)
    {
        using var form=new Form{Text=title,Width=450,Height=190,StartPosition=FormStartPosition.CenterParent};var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,Text=initial??""};var ok=new Button{Text="Add text",Dock=DockStyle.Bottom,DialogResult=DialogResult.OK};form.Controls.Add(text);form.Controls.Add(ok);form.AcceptButton=ok;return form.ShowDialog(this)==DialogResult.OK?text.Text:null;
    }
}
