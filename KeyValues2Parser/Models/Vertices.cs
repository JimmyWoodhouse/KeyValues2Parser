using KeyValues2Parser.Constants;

namespace KeyValues2Parser.Models
{
	public class Vertices : IEquatable<Vertices>
    {
        public float x;
        public float y;
        public float? z;

        public Vertices(Vertices vertices)
        {
            x = vertices.x;
            y = vertices.y;
            z = vertices.z;
        }

        public Vertices(string vertices)
        {
            if (string.IsNullOrWhiteSpace(vertices))
            {
                Console.WriteLine("vertices string was null or white space when creating a new Vertices, skipping.");
                return;
            }

            var verticesSplit = vertices.Replace($"\"", string.Empty).Split(" ");

            if (verticesSplit.Count() < 2 || verticesSplit.Count() > 3)
            {
                Console.WriteLine($"Incorrect number of vertices found after splitting string: {verticesSplit.Count()}");
                return;
            }

            float.TryParse(verticesSplit[0], Globalization.Style, Globalization.Culture, out x);
            float.TryParse(verticesSplit[1], Globalization.Style, Globalization.Culture, out y);

            if (verticesSplit.Count() == 3)
            {
                float.TryParse(verticesSplit[2], Globalization.Style, Globalization.Culture, out float zTemp);

                z = zTemp;
            }
        }

        public Vertices(string vertices, string origin)
        {
            if (string.IsNullOrWhiteSpace(vertices))
            {
                Console.WriteLine("vertices string was null or white space when creating a new Vertices, skipping.");
                return;
            }
            if (string.IsNullOrWhiteSpace(origin))
            {
                Console.WriteLine("origin string was null or white space when creating a new Vertices, skipping.");
                return;
            }

            var verticesSplit = vertices.Replace($"\"", string.Empty).Split(" ");
            var originSplit = origin.Replace($"\"", string.Empty).Split(" ");

            if (verticesSplit.Count() != 3)
            {
                Console.WriteLine($"Incorrect number of vertices found after splitting vertices string when creating a new Vertices: {verticesSplit.Count()}");
                return;
            }
            if (originSplit.Count() != 3)
            {
                Console.WriteLine($"Incorrect number of vertices found after splitting origin string when creating a new Vertices: {originSplit.Count()}");
                return;
            }

            float.TryParse(verticesSplit[0], Globalization.Style, Globalization.Culture, out float xVertices);
            float.TryParse(verticesSplit[1], Globalization.Style, Globalization.Culture, out float yVertices);
            float.TryParse(verticesSplit[2], Globalization.Style, Globalization.Culture, out float zVertices);

            float.TryParse(originSplit[0], Globalization.Style, Globalization.Culture, out float xOrigin);
            float.TryParse(originSplit[1], Globalization.Style, Globalization.Culture, out float yOrigin);
            float.TryParse(originSplit[2], Globalization.Style, Globalization.Culture, out float zOrigin);

            x = xVertices + xOrigin;
            y = yVertices + yOrigin;
            z = zVertices + zOrigin;
        }

        public Vertices(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vertices(float x, float y)
        {
            this.x = x;
            this.y = y;
        }


		public override bool Equals(object obj)
		{
			return Equals(obj as Vertices);
		}

        public bool Equals(Vertices other)
        {
            if (x == other.x && y == other.y && z == other.z)
                return true;

            return false;
        }


        public static Vertices operator +(Vertices a, Vertices b) => new Vertices((a.x + b.x), (a.y + b.y), ((a.z ?? 0) + (b.z ?? 0)));

        public static Vertices operator -(Vertices a, Vertices b) => new Vertices((a.x - b.x), (a.y - b.y), ((a.z ?? 0) - (b.z ?? 0)));

        public static Vertices operator *(Vertices a, Vertices b) => new Vertices((a.x * b.x), (a.y * b.y), ((a.z ?? 0) * (b.z ?? 0)));

        public static Vertices operator /(Vertices a, Vertices b) => new Vertices((a.x / b.x), (a.y / b.y), ((a.z ?? 0) / (b.z ?? 0)));

        public static Vertices operator *(Vertices a, int b) => new Vertices((a.x * b), (a.y * b), ((a.z ?? 1) * b));

        public static Vertices operator /(Vertices a, int b) => new Vertices((a.x / b), (a.y / b), ((a.z ?? 1) / b));

        public static Vertices operator *(Vertices a, float b) => new Vertices((a.x * b), (a.y * b), ((a.z ?? 1) * b));

        public static Vertices operator /(Vertices a, float b) => new Vertices((a.x / b), (a.y / b), ((a.z ?? 1) / b));


        public override int GetHashCode()
        {
            int hashX = x == 0 ? 0 : x.GetHashCode();
            int hashY = y == 0 ? 0 : y.GetHashCode();
            int hashZ = z == 0 ? 0 : z.GetHashCode();

            return hashX ^ hashY ^ hashZ;
        }


        public string GetPlaneFormatForSingleVertices()
        {
            return "(" + GetStringFormat() + ")";
        }


        public string GetStringFormat()
        {
            return z == null ? $"{ConfigurationSorter.GetFloatInEnglishFormatString(x)} {ConfigurationSorter.GetFloatInEnglishFormatString(y)} 0" : $"{ConfigurationSorter.GetFloatInEnglishFormatString(x)} {ConfigurationSorter.GetFloatInEnglishFormatString(y)} {ConfigurationSorter.GetFloatInEnglishFormatString(z)}";
        }
	}
}
