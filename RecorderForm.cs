using System.Runtime.InteropServices;
namespace ScreenAnote;
internal sealed class RecorderForm:Form
{
    private Rectangle region;
    private RegionRecorder? recorder;
    private bool starting,finishing,pendingSave,closeRequested,recordingActive;
    private readonly Label details=new(){Dock=DockStyle.Top,Height=34,TextAlign=ContentAlignment.MiddleLeft};
    private readonly Label clock=new(){Dock=DockStyle.Top,Height=38,Font=new Font("Segoe UI",16,FontStyle.Bold),Text="Ready · 00:00",ForeColor=Theme.Ink};
    private readonly Label hint=new(){Dock=DockStyle.Bottom,Height=34,ForeColor=Theme.Muted,Text="MP4 · toggle mic for voice · 15 fps · up to 1920 × 1080",TextAlign=ContentAlignment.MiddleLeft};
    private readonly ModernButton start=new(){Text="Start",Symbol="video",Primary=true,Width=95,Height=36};
    private readonly ModernButton pause=new(){Text="Pause",Width=85,Height=36,Enabled=false};
    private readonly ModernButton stop=new(){Text="Stop",Width=80,Height=36,Enabled=false};
    private readonly ModernButton choose=new(){Text="Region…",Width=90,Height=36};
    private readonly ModernButton mic=new(){Text="Mic off",Symbol="mic-off",Width=104,Height=36,AccessibleName="Toggle microphone recording"};
    private bool micEnabled;
    private readonly ModernButton save=new(){Text="Save MP4",Width=100,Height=36,Visible=false};
    private readonly System.Windows.Forms.Timer timer=new(){Interval=200};
    private bool pauseHotkey,stopHotkey;
    public RecorderForm(Rectangle selected)
    {
        region=selected;Text="ScreenAnote · Region recording";ClientSize=new Size(640,182);Font=new Font("Segoe UI",10);BackColor=Color.White;Padding=new Padding(18,12,18,12);FormBorderStyle=FormBorderStyle.FixedToolWindow;MaximizeBox=false;TopMost=true;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42,WrapContents=false};foreach(var b in new[]{start,pause,stop,choose,mic,save}){b.Margin=new Padding(0,0,7,0);buttons.Controls.Add(b);}
        Controls.Add(buttons);Controls.Add(clock);Controls.Add(details);Controls.Add(hint);
        mic.Click+=(_,_)=>{micEnabled=!micEnabled;mic.Text=micEnabled?"Mic on":"Mic off";mic.Symbol=micEnabled?"mic":"mic-off";mic.Selected=micEnabled;mic.Invalidate();if(recorder!=null&&!recorder.Completion.IsCompleted)recorder.MicrophoneEnabled=micEnabled;};
        start.Click+=async(_,_)=>await StartAsync();pause.Click+=(_,_)=>Pause();stop.Click+=async(_,_)=>await StopAsync();choose.Click+=(_,_)=>ChooseRegion();save.Click+=(_,_)=>SaveRecording();
        timer.Tick+=async(_,_)=>{if(recorder==null||finishing||starting)return;var elapsed=recorder.Duration;clock.Text=$"{(recorder.Paused?"Paused":"● Recording")} · {(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";clock.ForeColor=recorder.Paused?Theme.Muted:Color.Firebrick;if(recorder.TakeMicrophoneError() is string error){micEnabled=false;mic.Text="Mic off";mic.Symbol="mic-off";mic.Selected=false;mic.Invalidate();MessageBox.Show(this,error+"\nVideo recording continues with the microphone off. Check your default input device and Windows microphone permissions.","Microphone",MessageBoxButtons.OK,MessageBoxIcon.Information);}if(recorder.Completion.IsCompleted)await StopAsync();};
        FormClosing+=async(_,e)=>
        {
            if(starting){e.Cancel=true;closeRequested=true;return;}
            if(finishing){e.Cancel=true;closeRequested=true;return;}
            if(recorder!=null && !recorder.Completion.IsCompleted){e.Cancel=true;closeRequested=true;await StopAsync();return;}
            if(pendingSave && MessageBox.Show(this,"Discard the recording that has not been saved?","Unsaved recording",MessageBoxButtons.OKCancel,MessageBoxIcon.Question)!=DialogResult.OK){e.Cancel=true;closeRequested=false;}
        };
        FormClosed+=(_,_)=>{timer.Stop();recorder?.Dispose();recorder=null;};UpdateRegion();
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);WindowChrome.Round(this);
        pauseHotkey=RegisterHotKey(Handle,701,0x4006,0x78);stopHotkey=RegisterHotKey(Handle,702,0x4006,0x79);
        if(pauseHotkey&&stopHotkey)hint.Text="MP4 · toggle mic for voice · Ctrl+Shift+F9 pause · Ctrl+Shift+F10 stop";
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
        var output=VideoFrames.OutputSize(region.Size);details.Text=$"Region {region.Width} × {region.Height}  →  MP4 {output.Width} × {output.Height} · 15 fps";
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
        recorder?.Dispose();recorder=null;pendingSave=false;save.Visible=false;starting=true;start.Enabled=choose.Enabled=false;clock.Text="Preparing recorder…";
        try
        {
            recorder=new RegionRecorder(region,micEnabled);await recorder.Ready;recordingActive=true;pause.Enabled=stop.Enabled=true;timer.Start();clock.Text="● Recording · 00:00";
        }
        catch(Exception ex)
        {
            if(recorder!=null){try{await recorder.StopAsync();}catch{}recorder.Dispose();recorder=null;}
            MessageBox.Show(this,"Recording could not start. "+ex.Message+"\nWindows N editions need the Media Feature Pack.","ScreenAnote recording",MessageBoxButtons.OK,MessageBoxIcon.Error);clock.Text="Ready · 00:00";start.Enabled=choose.Enabled=true;
        }
        finally{starting=false;}
        if(closeRequested){if(recorder!=null)await StopAsync();else Close();}
    }
    private void Pause()
    {
        if(recorder==null||starting||finishing||recorder.Completion.IsCompleted)return;recorder.TogglePause();pause.Text=recorder.Paused?"Resume":"Pause";
    }
    private async Task StopAsync()
    {
        if(recorder==null||starting||finishing||!recordingActive)return;
        recordingActive=false;finishing=true;timer.Stop();pause.Enabled=stop.Enabled=false;clock.Text="Finishing MP4…";
        try{await recorder.StopAsync();pendingSave=true;save.Visible=true;clock.Text="Stopped · ready to save";SaveRecording();}
        catch(Exception ex){pendingSave=false;recorder.Dispose();recorder=null;clock.Text="Recording stopped";MessageBox.Show(this,"Recording failed: "+ex.Message,"ScreenAnote recording",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{finishing=false;start.Enabled=choose.Enabled=true;pause.Text="Pause";}
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
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    [DllImport("user32.dll",SetLastError=true)]private static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]private static extern bool UnregisterHotKey(IntPtr window,int id);
    [DllImport("user32.dll")]private static extern bool SetWindowDisplayAffinity(IntPtr window,uint affinity);
}
