// Small shared helpers: a seeded random generator, money and ordinal formatting, playing cards.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CFDream
{
    // Seeded random numbers (same seed, same sequence): hands, placements, invites and meetups use it.
    public class Rng
    {
        uint a;
        public Rng(string seed) { a = Hash(seed) | 1u; }
        public Rng(uint seed) { a = seed | 1u; }
        public static uint Hash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s) { h ^= c; h *= 16777619; }
            return h;
        }
        // mulberry32
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return ((t ^ (t >> 14)) >> 0) / 4294967296.0;
            }
        }
        public int Int(int n) => n <= 0 ? 0 : (int)(Next() * n) % n;
        public T Pick<T>(IList<T> list) => list[Int(list.Count)];
    }

    public static class Fmt
    {
        static readonly NumberFormatInfo US = new NumberFormatInfo { NumberGroupSeparator = ",", NumberDecimalSeparator = ".", NumberGroupSizes = new[] { 3 } };
        public static string Money(double n) => Math.Round(n).ToString("#,0", US);
        public static string Short(double n)
        {
            if (n >= 1e6) return (n / 1e6).ToString(n >= 1e7 ? "0.#" : "0.##", US) + "M";
            if (n >= 1e4) return Math.Round(n / 1000).ToString("0", US) + "K";
            return Money(n);
        }
        public static string Ord(int n)
        {
            int v = n % 100;
            if (v >= 11 && v <= 13) return n + "th";
            switch (n % 10) { case 1: return n + "st"; case 2: return n + "nd"; case 3: return n + "rd"; default: return n + "th"; }
        }
        public static string Bb(double x)
        {
            double r = Math.Round(x * 10) / 10;
            return Math.Abs(r - Math.Round(r)) < 1e-9 ? Math.Round(r).ToString("0", US) : r.ToString("0.0", US);
        }
        public static string Pct(double x) => Math.Round(x).ToString("0", US) + "%";
    }

    // Cards are ints 0..51: rank = c >> 2 (2..A), suit = c & 3 (c, d, h, s)
    public static class Cards
    {
        public const string Ranks = "23456789TJQKA";
        public const string Suits = "cdhs";
        static readonly string[] Sym = { "♣", "♦", "♥", "♠" };
        public static string Rank(int c) => Ranks[c >> 2].ToString();
        public static char Suit(int c) => Suits[c & 3];
        public static string SuitSym(int c) => Sym[c & 3];
    }
}
