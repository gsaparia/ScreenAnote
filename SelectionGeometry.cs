using System.Drawing;
namespace ScreenAnote;
internal static class SelectionGeometry
{
    public static RectangleF Bounds(IEnumerable<RectangleF> boxes)
    {
        using var iterator=boxes.GetEnumerator();if(!iterator.MoveNext())return RectangleF.Empty;
        var result=iterator.Current;while(iterator.MoveNext())result=RectangleF.Union(result,iterator.Current);return result;
    }
    public static RectangleF MapBounds(RectangleF box,RectangleF from,RectangleF to)
    {
        var point=ShapeGeometry.Map(box.Location,from,to);
        return new RectangleF(point,new SizeF(box.Width/Math.Max(1,from.Width)*to.Width,box.Height/Math.Max(1,from.Height)*to.Height));
    }
}
