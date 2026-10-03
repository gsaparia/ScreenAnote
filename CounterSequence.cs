namespace ScreenAnote;
internal sealed class CounterSequence
{
    public int Next{get;private set;}=1;
    public int Take(){if(Next==int.MaxValue)throw new InvalidOperationException("Reset the counter before continuing.");return Next++;}
    public void Reset(int value=1){if(value<1)throw new ArgumentOutOfRangeException(nameof(value));Next=value;}
}
