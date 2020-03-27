using System.Collections.Generic;

namespace SolutionValidator.DependencyGraph
{
    public class Colour
    {
        public static readonly List<string> BuildInColours = new List<string> { "Default", "Invalid" };
        public static readonly Colour Default = new Colour("Default", "Default Colour if not specified.", 0);
        // Bitwise OR of Invalid colour will always produce an invalid colour
        public static readonly Colour Invalid = new Colour("Invalid", "Represents an invalid colour combination", -1);

        public string Name { get; }
        public string Description { get; }
        public int Value { get; }

        public bool IsValid => Value >= 0;
        public bool IsBaseColour => Value % 2 == 0;

        public Colour(string name, string description, int value)
        {
            Name = name;
            Description = description;
            Value = value;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public struct ColourValue
    {
        public int Value { get; }

        public ColourValue(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColourValue))
            {
                return false;
            }

            return (ColourValue)obj == this;
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(ColourValue a, ColourValue b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(ColourValue a, ColourValue b)
        {
            return a.Value != b.Value;
        }

        public static ColourValue operator +(ColourValue a, ColourValue b)
        {
            return new ColourValue(a.Value | b.Value);
        }
    }
}
