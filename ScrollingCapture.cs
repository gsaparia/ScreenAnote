namespace ScreenAnote;
internal static class ScrollingCapture
{
    // Match previous bottom against current top, excluding scrollbar edges.
    internal static double Difference(Bitmap previous,Bitmap current,int shift)
    {
        int overlap=previous.Height-shift;double sum=0;int count=0;
        for(int y=8;y<overlap-8;y+=Math.Max(1,overlap/24))
        for(int x=12;x<previous.Width-20;x+=Math.Max(1,previous.Width/32))
        {var a=previous.GetPixel(x,y+shift);var b=current.GetPixel(x,y);sum+=Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);count+=3;}
        return count==0?255:sum/count;
    }
    internal static int FindShift(Bitmap previous,Bitmap current)
    {
        int best=-1;double score=255;
        for(int shift=5;shift<=previous.Height*3/4;shift++)
        {double value=Difference(previous,current,shift);if(value<score){score=value;best=shift;}}
        return score<9?best:-1;
    }
    public static async Task<(Bitmap Image,string Note)> Run(Rectangle area)
    {
        if(area.Width<80 || area.Height<120)throw new InvalidOperationException("Choose a browser content area at least 80 × 120 pixels.");
        var parts=new List<(Bitmap Image,int Added)>();string note="Stopped at page bottom.";
        Point original=Cursor.Position;
        try
        {
            parts.Add((ScreenCapture.Screen(area),area.Height));long height=area.Height;
            for(int i=0;i<100;i++)
            {
                if((Native.GetAsyncKeyState(27)&0x8000)!=0){note="Stopped with Escape; partial capture.";break;}
                Cursor.Position=new Point(area.Left+area.Width/2,area.Top+area.Height/2);
                Native.mouse_event(0x0800,0,0,-120,UIntPtr.Zero);
                await Task.Delay(550);
                if((Native.GetAsyncKeyState(27)&0x8000)!=0){note="Stopped with Escape; partial capture.";break;}
                var next=ScreenCapture.Screen(area);var previous=parts[^1].Image;
                if(Difference(previous,next,0)<1.2){next.Dispose();break;}
                int shift=FindShift(previous,next);
                if(shift<0){next.Dispose();note="Stopped: overlapping content could not be matched. Use Full web page for this page.";break;}
                if((height+shift)*area.Width>40_000_000 || (long)parts.Count*area.Width*area.Height>40_000_000){next.Dispose();note="Stopped at memory limit; partial capture.";break;}
                parts.Add((next,shift));height+=shift;
                if(i==99)note="Stopped at 100 scroll steps; partial capture.";
            }
            var result=new Bitmap(area.Width,(int)height);
            try {using var g=Graphics.FromImage(result);int y=0;
                foreach(var part in parts){var source=new Rectangle(0,part.Image.Height-part.Added,area.Width,part.Added);g.DrawImage(part.Image,new Rectangle(0,y,area.Width,part.Added),source,GraphicsUnit.Pixel);y+=part.Added;}
                return(result,note);
            }catch{result.Dispose();throw;}
        }
        finally {foreach(var part in parts)part.Image.Dispose();Cursor.Position=original;}
    }
}
