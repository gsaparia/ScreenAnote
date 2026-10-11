using System.Runtime.InteropServices;
namespace ScreenAnote;
internal sealed class RecorderForm:Form
{
    private Rectangle region;
    private RegionRecorder? recorder;
    private bool starting,finishing,pendingSave,closeRequested,recordingActive;
    private readonly Label details=new(){Dock=DockStyle.Top,Height=34,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label clock=new(){Dock=DockStyle.Top,Height=38,Font=new Font("Segoe UI",16,FontStyle.Bold),Text="Ready · 00:00",ForeColor=Theme.Ink};
    private readonly Label hint=new(){Dock=DockStyle.Bottom,Height=34,ForeColor=Theme.Muted,Text="Mic on enables voice preview · system audio is separate · limiter always on",TextAlign=ContentAlignment.MiddleLeft};
    private readonly ModernButton start=new(){Text="Start",Symbol="video",Primary=true,Width=95,Height=36};
    private readonly ModernButton pause=new(){Text="Pause",Width=85,Height=36,Enabled=false};
    private readonly ModernButton stop=new(){Text="Stop",Width=80,Height=36,Enabled=false};
    private readonly ModernButton choose=new(){Text="Region…",Width=90,Height=36};
    private readonly ModernButton mic=new(){Text="Mic off",Symbol="mic-off",Width=104,Height=36,AccessibleName="Toggle microphone recording and preview"};
    private readonly TrackBar micGain=new(){Minimum=0,Maximum=400,Value=200,SmallChange=25,LargeChange=50,TickStyle=TickStyle.None,Dock=DockStyle.Fill,AccessibleName="Microphone gain percent"};
    private readonly TrackBar systemVolume=new(){Minimum=0,Maximum=100,Value=50,SmallChange=5,LargeChange=10,TickStyle=TickStyle.None,Dock=DockStyle.Fill,AccessibleName="Recorded system audio volume percent"};
    private readonly Label gainValue=new(){Text="200%",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label systemValue=new(){Text="50%",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label levelValue=new(){Text="Mic off",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft};
    private readonly MicrophoneMeter meter=new(){Dock=DockStyle.Fill,Margin=new Padding(3,8,12,8),AccessibleName="Microphone level after gain, before limiter"};
    private WaveMicrophone? preview;
    private Task previewShutdown=Task.CompletedTask;
    private readonly ComboBox fps=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=85,AccessibleName="Recording frames per second"};
    private readonly CheckBox showCursor=new(){Text="Show cursor",Checked=true,AutoSize=true,Margin=new Padding(18,4,0,0),AccessibleName="Show mouse cursor in recording"};
    private int SelectedFps=>(int)(fps.SelectedItem??VideoFrames.Fps);
    private bool micEnabled;
    private readonly ModernButton save=new(){Text="Save MP4",Width=100,Height=36,Visible=false};
    private readonly System.Windows.Forms.Timer timer=new(){Interval=200};
    private bool pauseHotkey,stopHotkey;
    public RecorderForm(Rectangle selected)
    {
        region=selected;Icon=AppIcon.Load();Text="ScreenAnote · Region recording";ClientSize=new Size(640,326);Font=new Font("Segoe UI",10);BackColor=Color.White;Padding=new Padding(18,12,18,12);FormBorderStyle=FormBorderStyle.FixedToolWindow;MaximizeBox=false;TopMost=true;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,WrapContents=false};foreach(var b in new[]{start,pause,stop,choose,mic,save}){b.Margin=new Padding(0,0,7,0);buttons.Controls.Add(b);}
        fps.Items.AddRange(new object[]{15,24,30,60});fps.SelectedItem=VideoFrames.Fps;
        var options=new FlowLayoutPanel{Dock=DockStyle.Top,Height=36,WrapContents=false};
        options.Controls.Add(new Label{Text="FPS",AutoSize=true,Margin=new Padding(0,5,8,0)});options.Controls.Add(fps);
        options.Controls.Add(showCursor);showCursor.CheckedChanged+=(_,_)=>{if(recorder!=null&&!recorder.Completion.IsCompleted)recorder.ShowCursor=showCursor.Checked;};
        fps.SelectedIndexChanged+=(_,_)=>UpdateRegion();
        var audio=new TableLayoutPanel{Dock=DockStyle.Top,Height=108,ColumnCount=3,RowCount=3};
        audio.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));audio.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));audio.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,115));
        for(int row=0;row<3;row++)audio.RowStyles.Add(new RowStyle(SizeType.Absolute,36));
        void AudioRow(string label,Control control,Control value,int row){audio.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);audio.Controls.Add(control,1,row);audio.Controls.Add(value,2,row);}
        AudioRow("Mic gain",micGain,gainValue,0);AudioRow("System audio",systemVolume,systemValue,1);AudioRow("Mic level",meter,levelValue,2);
        Controls.Add(buttons);Controls.Add(audio);Controls.Add(options);Controls.Add(clock);Controls.Add(details);Controls.Add(hint);
        micGain.ValueChanged+=(_,_)=>{gainValue.Text=micGain.Value+"%";if(recorder!=null&&!recorder.Completion.IsCompleted)recorder.MicrophoneGainPercent=micGain.Value;};
        systemVolume.ValueChanged+=(_,_)=>{systemValue.Text=systemVolume.Value==0?"Muted":""+systemVolume.Value+"%";if(recorder!=null&&!recorder.Completion.IsCompleted)recorder.SystemAudioPercent=systemVolume.Value;};
        mic.Click+=async(_,_)=>
        {
            if(starting||finishing)return;
            micEnabled=!micEnabled;mic.Text=micEnabled?"Mic on":"Mic off";mic.Symbol=micEnabled?"mic":"mic-off";mic.Selected=micEnabled;mic.Invalidate();
            if(recorder!=null&&!recorder.Completion.IsCompleted)recorder.MicrophoneEnabled=micEnabled;
            else if(micEnabled)EnsurePreview();else await StopPreviewAsync();
        };
        start.Click+=async(_,_)=>await StartAsync();pause.Click+=(_,_)=>Pause();stop.Click+=async(_,_)=>await StopAsync();choose.Click+=(_,_)=>ChooseRegion();save.Click+=(_,_)=>SaveRecording();
        timer.Tick+=async(_,_)=>
        {
            EnsurePreview();
            float level=recordingActive&&recorder!=null?recorder.TakeMicrophonePeak():(preview?.TakePeak()??0)*micGain.Value/100f;
            meter.Level=level;levelValue.Text=!micEnabled?"Mic off":recorder?.Paused==true&&recordingActive?"Paused":level>0?$"{20*Math.Log10(level):0} dBFS":"Silent";
            if(preview?.TakeError() is string previewError)hint.Text=previewError;
            if(recorder==null||finishing||starting||!recordingActive)return;
            var elapsed=recorder.Duration;clock.Text=$"{(recorder.Paused?"Paused":"● Recording")} · {(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";clock.ForeColor=recorder.Paused?Theme.Muted:Color.Firebrick;
            if(recorder.TakeMicrophoneError() is string error){MessageBox.Show(this,error+"\nVideo recording continues. Check the default audio devices and Windows microphone permissions.","Microphone",MessageBoxButtons.OK,MessageBoxIcon.Information);}
            if(recorder.Completion.IsCompleted)await StopAsync();
        };
        FormClosing+=async(_,e)=>
        {
            if(starting){e.Cancel=true;closeRequested=true;return;}
            if(finishing){e.Cancel=true;closeRequested=true;return;}
            if(recorder!=null && !recorder.Completion.IsCompleted){e.Cancel=true;closeRequested=true;await StopAsync();return;}
            if(pendingSave && MessageBox.Show(this,"Discard the recording that has not been saved?","Unsaved recording",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK){e.Cancel=true;closeRequested=false;}
        };
        FormClosed+=(_,_)=>{timer.Stop();var old=preview;preview=null;if(old!=null)_=Task.Run(old.Dispose);recorder?.Dispose();recorder=null;};UpdateRegion();timer.Start();
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);WindowChrome.Round(this);
        pauseHotkey=RegisterHotKey(Handle,701,0x4006,0x78);stopHotkey=RegisterHotKey(Handle,702,0x4006,0x79);
        if(pauseHotkey&&stopHotkey)hint.Text="Limiter on · Ctrl+Shift+F9 pause · Ctrl+Shift+F10 stop · Mic on previews voice";
        // Supported Windows versions omit this controller from screen capture.
        SetWindowDisplayAffinity(Handle,0x11);
    }
    protected override void OnHandleDestroyed(EventArgs e){if(pauseHotkey)UnregisterHotKey(Handle,701);if(stopHotkey)UnregisterHotKey(Handle,702);base.OnHandleDestroyed(e);}
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);if(m.Msg!=0x312)return;if(m.WParam.ToInt32()==701)Pause();else if(m.WParam.ToInt32()==702)_=StopAsync();
    }
    private void UpdateRegion()
    {
        var output=VideoFrames.OutputSize(region.Size);details.Text=$"Region {region.Width} × {region.Height}  →  fragmented MP4 {output.Width} × {output.Height} · {SelectedFps} fps";
        var desktop=SystemInformation.VirtualScreen;
        // Prefer a position outside the chosen recording area, across any monitor.
        var candidates=Screen.AllScreens.SelectMany(s=>new[]{new Point(s.WorkingArea.Left+12,s.WorkingArea.Top+12),new Point(s.WorkingArea.Right-Width-12,s.WorkingArea.Bottom-Height-12)}).ToList();
        var point=candidates.FirstOrDefault(p=>!new Rectangle(p,Size).IntersectsWith(region),new Point(desktop.Right-Width-12,desktop.Top+12));Location=point;
    }
    private void ChooseRegion()
    {
        if(recorder!=null&&!recorder.Completion.IsCompleted||starting||finishing)return;
        Hide();try{var area=ScreenCapture.SelectRegion();if(area.HasValue){VideoFrames.OutputSize(area.Value.Size);region=area.Value;UpdateRegion();}}catch(Exception ex){MessageBox.Show(ex.Message,"Select recording region");}finally{Show();}
    }
    private async Task StartAsync()
    {
        if(starting||finishing||recorder!=null&&!recorder.Completion.IsCompleted)return;
        if(pendingSave && MessageBox.Show(this,"Discard the unsaved recording and start a new one?","New recording",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
        recorder?.Dispose();recorder=null;pendingSave=false;save.Visible=false;starting=true;start.Enabled=choose.Enabled=fps.Enabled=mic.Enabled=false;clock.Text="Preparing recorder…";
        try
        {
            await StopPreviewAsync();
            recorder=new RegionRecorder(region,micEnabled,SelectedFps,micGain.Value,systemVolume.Value,showCursor.Checked);await recorder.Ready;recordingActive=true;pause.Enabled=stop.Enabled=true;clock.Text="● Recording · 00:00";
        }
        catch(Exception ex)
        {
            if(recorder!=null){try{await recorder.StopAsync();}catch{}recorder.Dispose();recorder=null;}
            MessageBox.Show(this,"Recording could not start. "+ex.Message,"ScreenAnote recording",MessageBoxButtons.OK,MessageBoxIcon.Error);clock.Text="Ready · 00:00";start.Enabled=choose.Enabled=fps.Enabled=true;
        }
        finally{starting=false;mic.Enabled=true;if(recorder==null&&!closeRequested)EnsurePreview();}
        if(closeRequested){if(recorder!=null)await StopAsync();else Close();}
    }
    private void Pause()
    {
        if(recorder==null||starting||finishing||recorder.Completion.IsCompleted)return;recorder.TogglePause();pause.Text=recorder.Paused?"Resume":"Pause";
    }
    private async Task StopAsync()
    {
        if(recorder==null||starting||finishing||!recordingActive)return;
        recordingActive=false;finishing=true;pause.Enabled=stop.Enabled=mic.Enabled=micGain.Enabled=systemVolume.Enabled=false;clock.Text="Finishing MP4…";
        try
        {
            var completion=recorder.StopAsync();var elapsed=System.Diagnostics.Stopwatch.StartNew();
            while(!completion.IsCompleted)
            {
                clock.Text=recorder.ShutdownStage+"…";
                if(elapsed.Elapsed.TotalSeconds>10)hint.Text="Windows is still finishing the recording. Please keep this window open.";
                await Task.WhenAny(completion,Task.Delay(200));
            }
            await completion;pendingSave=true;save.Visible=true;clock.Text="Stopped · ready to save";
            if(recorder.TakeMicrophoneError() is string warning)hint.Text=warning;
            // Stop completes before opening any native save dialog.
            if(closeRequested)SaveRecording();
        }
        catch(Exception ex){pendingSave=false;recorder.Dispose();recorder=null;clock.Text="Recording stopped";MessageBox.Show(this,"Recording failed: "+ex.Message,"ScreenAnote recording",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{finishing=false;start.Enabled=choose.Enabled=fps.Enabled=mic.Enabled=micGain.Enabled=systemVolume.Enabled=true;pause.Text="Pause";if(!closeRequested)EnsurePreview();}
        if(closeRequested)Close();
    }
    private void SaveRecording()
    {
        if(!pendingSave||recorder==null)return;
        using var dialog=new SaveFileDialog{Title="Save recording",Filter="MP4 video|*.mp4",DefaultExt="mp4",AddExtension=true,OverwritePrompt=true,FileName="Recording_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".mp4"};
        if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{File.Copy(recorder.FilePath,dialog.FileName,true);pendingSave=false;save.Visible=false;clock.Text="✓ Recording saved";}
        catch(Exception ex){MessageBox.Show(this,"Could not save MP4: "+ex.Message+"\nUse Save MP4 to retry.","Save recording",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void EnsurePreview()
    {
        if(micEnabled&&preview==null&&previewShutdown.IsCompleted&&!starting&&!finishing&&!recordingActive&&!IsDisposed)preview=new WaveMicrophone(true,()=>false,false);
    }
    private async Task StopPreviewAsync()
    {
        var old=preview;preview=null;if(old!=null)previewShutdown=Task.Run(old.Dispose);await previewShutdown;
    }
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();var old=preview;preview=null;if(old!=null)_=Task.Run(old.Dispose);}base.Dispose(disposing);}
    private sealed class MicrophoneMeter:Control
    {
        private float level;
        public float Level{set{level=value;AccessibleDescription=value>=.95f?"Loud; limiter may reduce peaks":value>0?"Microphone signal present":"No microphone signal";Invalidate();}}
        public MicrophoneMeter(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);e.Graphics.Clear(Color.FromArgb(232,235,240));
            // Logarithmic display: -60 dBFS to 0 dBFS. Colour is independent of
            // bar length so quiet speech remains visible without implying clipping.
            double db=level>0?20*Math.Log10(level):-60;int width=(int)(Width*Math.Clamp((db+60)/60,0,1));
            using var fill=new SolidBrush(level>=.95f?Color.Firebrick:level>=.7f?Color.DarkOrange:Color.SeaGreen);e.Graphics.FillRectangle(fill,0,0,width,Height);
        }
    }
    [DllImport("user32.dll",SetLastError=true)]private static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]private static extern bool UnregisterHotKey(IntPtr window,int id);
    [DllImport("user32.dll")]private static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
}
