namespace ScreenAnote;
internal static class StickerCatalog
{
    internal sealed record Sticker(string Glyph,string Name,string Category);
    public static readonly List<Sticker> Items=Create();
    private static List<Sticker> Create()
    {
        var items=new List<Sticker>();
        void Add(string category,string glyphs,string names){var symbols=glyphs.Split('|');var labels=names.Split('|');for(int i=0;i<symbols.Length;i++)items.Add(new(symbols[i],i<labels.Length?labels[i]:category+" "+(i+1),category));}
        Add("Smileys","☺|☻|😀|😃|😄|😁|😆|😅|😂|🤣|🙂|🙃|😉|😊|😍|🥰|😘|😎|🤓|🤔|😐|😶|😴|😢|😭|😡|🤯|🥳|😇|🤩|😱|🤗|🤭|🫡","smile|smiley|grin|happy|laugh|beam|laughing|sweat smile|tears joy|rolling laugh|slight smile|upside down|wink|blush|heart eyes|love|kiss|cool sunglasses|nerd|thinking|neutral|silent|sleep|sad|cry|angry|mind blown|party|angel|star eyes|shock|hug|giggle|salute");
        Add("Icons","👋|👍|👎|👌|👏|🙌|🙏|💪|✌|🤝|♥|❤|💙|💚|💛|💜|💔|★|⭐|🌟|✨|✓|✔|✗|❌|⚠|⛔|✅|❗|❓|ℹ|?|!|→|←|↑|↓|↔|↕|↗|↘","wave|thumb up|thumb down|okay|clap|hands|thanks|strong|peace|handshake|heart|red heart|blue heart|green heart|yellow heart|purple heart|broken heart|star|gold star|glowing star|sparkles|check|tick|cross|error|warning|stop|success|exclamation|question|information|question mark|attention|arrow right|arrow left|arrow up|arrow down|arrow horizontal|arrow vertical|arrow northeast|arrow southeast");
        Add("Objects","♫|🎵|⚑|🚩|🎯|🏆|🎉|🎁|💡|🔍|🔒|🔑|🔔|📌|📎|📝|📁|📷|💻|📱|🛠|⚙|⏰|⏳|🚀|✈|🚗|🏠|🌍|💰|📊|📈|📉","music|note|flag|red flag|target|trophy|celebration|gift|idea light bulb|search|lock|key|bell|pin|paperclip|note|folder|camera|computer|phone|tools|gear|clock|hourglass|rocket|plane|car|home|earth|money|chart|growth|decline");
        Add("Nature","☀|🌙|☁|🌈|⚡|🔥|💧|❄|🌸|🌳|🍀|🐶|🐱|🐼|🦊|🦋|🐝|🍎|🍕|☕|🍰","sun|moon|cloud|rainbow|lightning|fire|water|snow|flower|tree|clover|dog|cat|panda|fox|butterfly|bee|apple|pizza|coffee|cake");
        return items;
    }
}
