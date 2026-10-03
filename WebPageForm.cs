using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
namespace ScreenAnote;
internal sealed class WebPageForm:Form
{
    private readonly WebView2 browser=new(){Dock=DockStyle.Fill};
    private readonly TextBox address=new(){Width=540,PlaceholderText="https://example.com"};
    private readonly Button capture=new(){Text="Capture full page",AutoSize=true,Enabled=false};
    private readonly Label status=new(){Dock=DockStyle.Bottom,Height=30,Text="Initializing browser…"};
    public Bitmap? Result {get;private set;}
    public WebPageForm()
    {
        Text="ScreenAnote — Full web page";Width=1150;Height=800;StartPosition=FormStartPosition.CenterParent;
        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};var go=new Button{Text="Open",AutoSize=true};bar.Controls.AddRange([address,go,capture]);
        Controls.Add(browser);Controls.Add(status);Controls.Add(bar);
        go.Click+=(_,_)=>Navigate();address.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Navigate();}};
        capture.Click+=async(_,_)=>await CapturePage();
        Shown+=async(_,_)=>{
            try {
                var profile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ScreenAnote","BrowserProfile");
                var environment=await CoreWebView2Environment.CreateAsync(null,profile);
                await browser.EnsureCoreWebView2Async(environment);
                browser.CoreWebView2.NavigationStarting+=(_,_)=>{capture.Enabled=false;status.Text="Loading…";};
                browser.CoreWebView2.NavigationCompleted+=(_,e)=>{capture.Enabled=e.IsSuccess;status.Text=e.IsSuccess?"Scroll to load lazy content if needed, then capture. Login is supported in this session.":"Navigation failed: "+e.WebErrorStatus;};
                status.Text="Enter a URL and click Open.";
            }catch(Exception ex){status.Text="Browser unavailable. Install Microsoft Edge WebView2 Runtime.";MessageBox.Show(this,ex.Message,"Browser",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        };
    }
    private void Navigate()
    {
        if(browser.CoreWebView2==null)return;string text=address.Text.Trim();if(!text.Contains("://"))text="https://"+text;
        if(!Uri.TryCreate(text,UriKind.Absolute,out var uri) || (uri.Scheme!="https" && uri.Scheme!="http")){MessageBox.Show(this,"Enter an HTTP or HTTPS address.");return;}
        browser.CoreWebView2.Navigate(uri.AbsoluteUri);
    }
    private async Task CapturePage()
    {
        capture.Enabled=false;status.Text="Capturing complete page…";
        try
        {
            var core=browser.CoreWebView2;
            using var metrics=JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.getLayoutMetrics","{}"));
            var size=metrics.RootElement.GetProperty("cssContentSize");double width=Math.Ceiling(size.GetProperty("width").GetDouble()),height=Math.Ceiling(size.GetProperty("height").GetDouble());
            if(width<1 || height<1 || width>30000 || height>30000 || width*height>40_000_000)throw new InvalidOperationException("Page exceeds capture limit (30,000 pixels per side / 40 megapixels). Use a region capture.");
            var parameters=JsonSerializer.Serialize(new {format="png",fromSurface=true,captureBeyondViewport=true,clip=new{x=0,y=0,width,height,scale=1}});
            using var payload=JsonDocument.Parse(await core.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",parameters));
            using var stream=new MemoryStream(Convert.FromBase64String(payload.RootElement.GetProperty("data").GetString()!));using var decoded=new Bitmap(stream);Result=new Bitmap(decoded);
            DialogResult=DialogResult.OK;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Capture failed",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{if(!IsDisposed){capture.Enabled=true;status.Text="Ready.";}}
    }
}
