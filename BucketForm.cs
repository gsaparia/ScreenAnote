namespace ScreenAnote;
internal sealed class BucketForm:Form
{
    private readonly CaptureBucket bucket;
    private readonly ListView list=new(){Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false};
    private readonly ImageList thumbnails=new(){ImageSize=new Size(80,50),ColorDepth=ColorDepth.Depth32Bit};
    private readonly List<Bitmap> thumbnailCopies=new();
    private readonly Label note=new(){Dock=DockStyle.Top,Height=30,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Button open=new(){Text="Open",Width=85},rename=new(){Text="Rename",Width=85},delete=new(){Text="Delete",Width=85},undo=new(){Text="Undo delete",Width=110};
    public Guid? SelectedId{get;private set;}
    public event Action<Guid>? ItemDeleted;
    public event Action? ItemsChanged;
    public BucketForm(CaptureBucket bucket)
    {
        this.bucket=bucket;Text="ScreenAnote · Capture bucket";Icon=AppIcon.Load();Size=new Size(740,500);MinimumSize=new Size(550,350);Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterParent;BackColor=Color.White;Padding=new Padding(14);
        list.SmallImageList=thumbnails;list.Columns.Add("Capture",350);list.Columns.Add("Created",180);list.Columns.Add("Updated",180);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=42};buttons.Controls.AddRange([open,rename,delete,undo]);
        var saveBucket=new Button{Text="Save bucket…",Width=130};var openBucket=new Button{Text="Open bucket…",Width=130};
        var archiveButtons=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};archiveButtons.Controls.AddRange([saveBucket,openBucket,new Label{Text="Saved items only",AutoSize=true,Padding=new Padding(10,7,0,0)}]);
        Controls.Add(list);Controls.Add(note);Controls.Add(buttons);
        Controls.Add(archiveButtons);
        void Safe(Action action){try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Capture bucket",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
        saveBucket.Click+=(_,_)=>Safe(()=>
        {
            using var dialog=new SaveFileDialog{Title="Save entire bucket (saved items)",Filter="ScreenAnote editable bucket|*.screenanote-bucket",DefaultExt="screenanote-bucket",AddExtension=true,OverwritePrompt=true,FileName="CaptureBucket_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")};
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            var task=Task.Run(()=>bucket.Export(dialog.FileName));using(var progress=new BucketSavingForm(task,"Saving entire bucket","Saving all saved captures and editable annotations…"))progress.ShowDialog(this);task.GetAwaiter().GetResult();
            note.Text="Bucket saved. Add/update your canvas before saving to include its latest changes.";
        });
        openBucket.Click+=(_,_)=>Safe(()=>
        {
            using var dialog=new OpenFileDialog{Title="Open saved bucket (adds independent copies)",Filter="ScreenAnote editable bucket|*.screenanote-bucket"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            var task=Task.Run(()=>bucket.Import(dialog.FileName));using(var progress=new BucketSavingForm(task,"Opening saved bucket","Loading editable captures into your bucket…"))progress.ShowDialog(this);int count=task.GetAwaiter().GetResult();RefreshItems();ItemsChanged?.Invoke();note.Text=$"Opened {count} items · Existing items retained · Double-click an item to edit";
        });
        Guid? Selection()=>list.SelectedItems.Count==0?null:((BucketEntry)list.SelectedItems[0].Tag!).Id;
        void Open(){if(Selection() is Guid id){SelectedId=id;DialogResult=DialogResult.OK;}}
        open.Click+=(_,_)=>Open();list.DoubleClick+=(_,_)=>Open();
        rename.Click+=(_,_)=>Safe(()=>{if(Selection() is Guid id){var entry=bucket.Read(id);using var prompt=new BucketNameForm("Rename bucket item",entry.Name);if(prompt.ShowDialog(this)==DialogResult.OK){bucket.Rename(id,prompt.ItemName);RefreshItems();ItemsChanged?.Invoke();}}});
        delete.Click+=(_,_)=>Safe(()=>{if(Selection() is Guid id){bucket.Delete(id);ItemDeleted?.Invoke(id);RefreshItems();ItemsChanged?.Invoke();}});
        undo.Click+=(_,_)=>Safe(()=>{bucket.UndoDelete();RefreshItems();ItemsChanged?.Invoke();});
        list.SelectedIndexChanged+=(_,_)=>open.Enabled=rename.Enabled=delete.Enabled=list.SelectedItems.Count>0;
        Shown+=(_,_)=>Safe(RefreshItems);AcceptButton=open;
    }
    private void RefreshItems()
    {
        list.Items.Clear();thumbnails.Images.Clear();foreach(var image in thumbnailCopies)image.Dispose();thumbnailCopies.Clear();var items=bucket.List();
        foreach(var entry in items)
        {
            int icon=-1;
            try{using var image=Image.FromFile(Path.Combine(bucket.DataFolder(entry),"thumbnail.png"));var copy=new Bitmap(image);thumbnailCopies.Add(copy);thumbnails.Images.Add(copy);icon=thumbnails.Images.Count-1;}catch(Exception ex) when(ex is IOException or ArgumentException or UnauthorizedAccessException){}
            var row=new ListViewItem(entry.Name,icon){Tag=entry};row.SubItems.Add(entry.Created.ToLocalTime().ToString("dd MMM yyyy HH:mm"));row.SubItems.Add(entry.Updated.ToLocalTime().ToString("dd MMM yyyy HH:mm"));list.Items.Add(row);
        }
        note.Text=$"{items.Count} items · Double-click to open · Delete can be undone"+(bucket.UnreadableItems>0?$" · {bucket.UnreadableItems} unreadable":"");
        open.Enabled=rename.Enabled=delete.Enabled=false;undo.Enabled=bucket.HasDeleted;
    }
    protected override void Dispose(bool disposing){if(disposing){thumbnails.Dispose();foreach(var image in thumbnailCopies)image.Dispose();}base.Dispose(disposing);}
}
internal sealed class BucketNameForm:Form
{
    private readonly TextBox name=new(){Dock=DockStyle.Top,MaxLength=100};
    public string ItemName=>name.Text.Trim();
    public BucketNameForm(string title,string initial)
    {
        Text=title;Size=new Size(420,155);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;Font=new Font("Segoe UI",10);Padding=new Padding(12);name.Text=initial;
        var ok=new Button{Text="Save",DialogResult=DialogResult.None,Width=85};var cancel=new Button{Text="Cancel",DialogResult=DialogResult.Cancel,Width=85};
        var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=35,FlowDirection=FlowDirection.RightToLeft};row.Controls.Add(cancel);row.Controls.Add(ok);Controls.Add(name);Controls.Add(row);AcceptButton=ok;CancelButton=cancel;
        ok.Click+=(_,_)=>{if(ItemName.Length==0){name.Focus();return;}DialogResult=DialogResult.OK;};Shown+=(_,_)=>{name.Focus();name.SelectAll();};
    }
}
internal sealed class BucketSavingForm:Form
{
    private bool complete;
    public BucketSavingForm(Task work,string title="Saving to capture bucket",string message="Saving the image and editable annotations…")
    {
        Text=title;Size=new Size(390,145);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;ControlBox=false;Font=new Font("Segoe UI",10);Padding=new Padding(14);
        Controls.Add(new Label{Text=message,Dock=DockStyle.Fill});Controls.Add(new ProgressBar{Dock=DockStyle.Bottom,Height=20,Style=ProgressBarStyle.Marquee});
        Shown+=async(_,_)=>{try{await work;}catch{}finally{complete=true;Close();}};FormClosing+=(_,e)=>{if(!complete)e.Cancel=true;};
    }
}
