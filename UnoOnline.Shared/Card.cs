namespace UnoOnline.Shared;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CardColor Color { get; set; }
    public CardValue Value { get; set; }

    public Card(CardColor color, CardValue value)
    {
        if (color == CardColor.Wild && value != CardValue.Wild && value != CardValue.WildDrawFour)
        {
            throw new ArgumentException("Wild color can only have Wild or WildDrawFour value.");
        }
        if (color != CardColor.Wild && (value == CardValue.Wild || value == CardValue.WildDrawFour))
        {
            throw new ArgumentException("Non-wild colors cannot have Wild or WildDrawFour values.");
        }

        Color = color;
        Value = value;
    }

    public Card() { }

    public override string ToString()
    {
        return $"{Color} {Value}";
    }

}