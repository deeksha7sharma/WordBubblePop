using System.Collections.Generic;
using UnityEngine;

namespace WordBubblePop
{
    /// <summary>
    /// All words used in the game. Each word needs a matching picture in
    /// Assets/Resources/Pictures/WORD.png (for example Pictures/DOG.png).
    /// </summary>
    public static class WordData
    {
        public static readonly string[] ThreeLetters =
        {
            "CAT", "DOG", "SUN", "COW", "BUS", "EGG", "HAT", "CUP",
            "BEE", "FOX", "OWL", "PIG", "BED", "KEY", "ANT"
        };

        public static readonly string[] FourLetters =
        {
            "FISH", "BALL", "TREE", "MOON", "CAKE", "DUCK", "FROG", "LION",
            "BOOK", "STAR", "BEAR", "KITE", "MILK", "BOAT", "HAND"
        };

        public static readonly string[] FiveLetters =
        {
            "APPLE", "MANGO", "HORSE", "TIGER", "HOUSE", "TRAIN", "CHAIR",
            "GRAPE", "SNAKE", "ZEBRA", "CLOCK", "PLANT", "TEETH", "SHEEP"
        };

        /// <summary>Level 1 = 3 letters, 2 = 4 letters, 3 = 5 letters, 4+ = mixed.</summary>
        public static List<string> PickWords(int level, int count)
        {
            var pool = new List<string>();
            if (level <= 1) pool.AddRange(ThreeLetters);
            else if (level == 2) pool.AddRange(FourLetters);
            else if (level == 3) pool.AddRange(FiveLetters);
            else
            {
                pool.AddRange(ThreeLetters);
                pool.AddRange(FourLetters);
                pool.AddRange(FiveLetters);
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, Mathf.Min(count, pool.Count));
        }

        public static string LevelInfo(int level)
        {
            return level <= 3 ? (level + 2) + " letters" : "mixed words";
        }
    }
}
