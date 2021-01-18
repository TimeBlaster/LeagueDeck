namespace LeagueDeck.Models
{
    public abstract class Asset<T>
    {
        public string Id { get; set; }
        public abstract T SetDefault();
    }
}
