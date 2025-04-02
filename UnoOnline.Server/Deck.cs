using UnoOnline.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoOnline.Server
{
    public class Deck
    {
        private List<Card> _cards;
        private Random _random;

        public int CardsRemaining => _cards.Count;

        public Deck()
        {
            _random = new Random();
            InitializeDeck();
        }

        // Initializes the deck with all 108 Uno cards.
        private void InitializeDeck()
        {
            _cards = new List<Card>();
            var colors = new[] { CardColor.Red, CardColor.Yellow, CardColor.Green, CardColor.Blue };

            foreach (var color in colors)
            {
                // Every color: 1x 0 (zero) card
                _cards.Add(new Card(color, CardValue.Zero));

                // Every color: 2x 1-9 card
                for (int i = 1; i <= 9; i++)
                {
                    _cards.Add(new Card(color, (CardValue)i));
                    _cards.Add(new Card(color, (CardValue)i));
                }

                // Every color: 2x Skip, Reverse, DrawTwo card
                _cards.Add(new Card(color, CardValue.Skip));
                _cards.Add(new Card(color, CardValue.Skip));
                _cards.Add(new Card(color, CardValue.Reverse));
                _cards.Add(new Card(color, CardValue.Reverse));
                _cards.Add(new Card(color, CardValue.DrawTwo));
                _cards.Add(new Card(color, CardValue.DrawTwo));
            }

            // 4 Wild
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new Card(CardColor.Wild, CardValue.Wild));
            }

            // 4 Wild Draw Four
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new Card(CardColor.Wild, CardValue.WildDrawFour));
            }
            // Sum: 4 * (1 + 2*9 + 2*3) + 4 + 4 = 4 * (1 + 18 + 6) + 8 = 4 * 25 + 8 = 100 + 8 = 108 card
        }

        public void Shuffle()
        {
            _cards = _cards.OrderBy(c => _random.Next()).ToList();
            Console.WriteLine("--- Deck Shuffled ---");
        }

        public Card? DrawCard()
        {
            if (CardsRemaining > 0)
            {
                Card drawnCard = _cards[CardsRemaining - 1];
                _cards.RemoveAt(CardsRemaining - 1);
                return drawnCard;
            }
            else
            {
                Console.WriteLine("--- Deck is empty! Cannot draw. ---");
                return null;
            }
        }

        public void PrintDeckToConsole()
        {
            Console.WriteLine($"--- Current Deck ({CardsRemaining} cards) ---");
            foreach (var card in _cards)
            {
                Console.WriteLine(card.ToString());
            }
            Console.WriteLine("------------------------------------");
        }

        /// Adds a card back into the deck and reshuffles.
        /// Used for specific rules like putting a Wild Draw Four back during setup.
        public void AddCardBackAndShuffle(Card card)
        {
            if (card != null)
            {
                _cards.Add(card);
                Shuffle(); 
                Console.WriteLine($"Deck: Added {card} back and reshuffled.");
            }
        }
    }
}