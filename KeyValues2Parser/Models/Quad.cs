namespace KeyValues2Parser.Models
{
	public class Quad
	{
		public Vertices Vertices1 { get; set; }
		public Vertices Vertices2 { get; set; }
		public Vertices Vertices3 { get; set; }
		public Vertices Vertices4 { get; set; }
		public List<Vertices> Vertices { get { return new() { Vertices1, Vertices2, Vertices3, Vertices4 }; } set { SetVertices(value); } }

		public Quad(List<Vertices> vertices, bool calculateCorrectOrder, bool flipAxisY = false)
		{
			if (vertices.Count != 4)
			{
				Console.WriteLine($"Quad found with {vertices.Count} vertices but is required to have exactly 4, aborting.");
				return;
			}

			if (calculateCorrectOrder)
			{
				CalculateCorrectOrder(vertices.ElementAt(0), vertices.ElementAt(1), vertices.ElementAt(2), vertices.ElementAt(3), flipAxisY);
			}
			else
			{
				SetInOrderGivenIn(vertices.ElementAt(0), vertices.ElementAt(1), vertices.ElementAt(2), vertices.ElementAt(3));
			}
		}

		public Quad(Vertices vert1, Vertices vert2, Vertices vert3, Vertices vert4, bool calculateCorrectOrder, bool flipAxisY = false)
		{
			if (calculateCorrectOrder)
			{
				CalculateCorrectOrder(vert1, vert2, vert3, vert4, flipAxisY);
			}
			else
			{
				SetInOrderGivenIn(vert1, vert2, vert3, vert4);
			}
		}

		private void SetVertices(List<Vertices> vertices)
		{
			if (vertices == null ||
				!vertices.Any() ||
				vertices.Count() != 4 ||
				vertices[0] == null ||
				vertices[1] == null ||
				vertices[2] == null ||
				vertices[3] == null)
			{
				Console.WriteLine($"Cannot set Quad's vertices, aborting.");
				return;
			}

			Vertices1 = vertices[0];
			Vertices2 = vertices[1];
			Vertices3 = vertices[2];
			Vertices4 = vertices[3];
		}

        public Vertices? GetMiddleOfAllVertices()
        {
            if (Vertices == null || Vertices.Any(x => x == null))
			{
				Console.WriteLine($"Quad found with a null vertices, aborting.");
				return null;
			}

            Vertices middleOfAllVertices = new(0,0,0);

            foreach (var vert in Vertices)
            {
                middleOfAllVertices += vert;
            }

            middleOfAllVertices /= Vertices.Count;

            return middleOfAllVertices;
        }


		private void SetInOrderGivenIn(Vertices vert1, Vertices vert2, Vertices vert3, Vertices vert4)
		{
			if (vert1 == null ||
				vert2 == null ||
				vert3 == null ||
				vert4 == null)
			{
				Console.WriteLine($"Quad found with a null vertices, aborting.");
				return;
			}

			Vertices1 = vert1;
			Vertices2 = vert2;
			Vertices3 = vert3;
			Vertices4 = vert4;
		}

		/**
		 * Attempts to calculate the correct order for setting the values
		 */
		private void CalculateCorrectOrder(Vertices vert1, Vertices vert2, Vertices vert3, Vertices vert4, bool flipAxisY)
		{
			if (vert1 == null ||
				vert2 == null ||
				vert3 == null ||
				vert4 == null)
			{
				Console.WriteLine($"Quad found with a null vertices, aborting.");
				return;
			}

			List<Vertices> list = new() { vert1, vert2, vert3, vert4 };

			var furthestRightThenBottomFirst = flipAxisY ? list.OrderByDescending(a => a.x).ThenByDescending(a => a.y).ToList() : list.OrderByDescending(a => a.x).ThenBy(a => a.y).ToList(); // for subdiv segment texcoords and displacement values, y axis goes bottom up, not top down. However, vertices are the normal way (top down for Y axis)
			var furthestBottomThenRightFirst = flipAxisY ? list.OrderByDescending(a => a.y).ThenByDescending(a => a.x).ToList() : list.OrderBy(a => a.y).ThenByDescending(a => a.x).ToList();

			if (furthestRightThenBottomFirst.ElementAt(0) == furthestBottomThenRightFirst.ElementAt(0))
				Vertices1 = furthestRightThenBottomFirst.ElementAt(0);
			if (furthestRightThenBottomFirst.ElementAt(1) == furthestBottomThenRightFirst.ElementAt(1))
				Vertices2 = furthestRightThenBottomFirst.ElementAt(1);
			if (furthestRightThenBottomFirst.ElementAt(2) == furthestBottomThenRightFirst.ElementAt(2))
				Vertices3 = furthestRightThenBottomFirst.ElementAt(2);
			if (furthestRightThenBottomFirst.ElementAt(3) == furthestBottomThenRightFirst.ElementAt(3))
				Vertices4 = furthestRightThenBottomFirst.ElementAt(3);

			if (Vertices1 != null && Vertices2 != null && Vertices3 != null && Vertices4 != null)
				return;

			Vertices1 = furthestRightThenBottomFirst.ElementAt(0);
			Vertices2 = furthestRightThenBottomFirst.ElementAt(1);
			Vertices3 = furthestRightThenBottomFirst.ElementAt(2);
			Vertices4 = furthestRightThenBottomFirst.ElementAt(3);

			// attempts to get the right order of verts
			var result = CheckDistanceDiffs(Vertices1, Vertices2, flipAxisY);
			Vertices1 = result.ElementAt(0);
			Vertices2 = result.ElementAt(1);

			result = CheckDistanceDiffs(Vertices1, Vertices3, flipAxisY);
			Vertices1 = result.ElementAt(0);
			Vertices3 = result.ElementAt(1);

			result = CheckDistanceDiffs(Vertices1, Vertices4, flipAxisY);
			Vertices1 = result.ElementAt(0);
			Vertices4 = result.ElementAt(1);

			result = CheckDistanceDiffs(Vertices2, Vertices3, flipAxisY);
			Vertices2 = result.ElementAt(0);
			Vertices3 = result.ElementAt(1);

			result = CheckDistanceDiffs(Vertices2, Vertices4, flipAxisY);
			Vertices2 = result.ElementAt(0);
			Vertices4 = result.ElementAt(1);

			result = CheckDistanceDiffs(Vertices3, Vertices4, flipAxisY);
			Vertices3 = result.ElementAt(0);
			Vertices4 = result.ElementAt(1);
		}

		public static List<Vertices> CheckDistanceDiffs(Vertices vert1, Vertices vert2, bool flipAxisY)
		{
			var xDiff = vert1.x - vert2.x;
			var yDiff = flipAxisY ? vert2.y - vert1.y : vert1.y - vert2.y;

			if (xDiff >= 0 && yDiff >= 0)
				return new List<Vertices>() { vert1, vert2 };
			else if (xDiff <= 0 && yDiff <= 0)
				return new List<Vertices>() { vert2, vert1 };

			if (Math.Abs(xDiff) >= Math.Abs(yDiff))
			{
				if (xDiff > 0)
					return new List<Vertices>() { vert1, vert2 };
				else
					return new List<Vertices>() { vert2, vert1 };
			}
			else
			{
				if ((yDiff > 0 && flipAxisY) || (yDiff < 0 && !flipAxisY))
					return new List<Vertices>() { vert1, vert2 };
				else
					return new List<Vertices>() { vert2, vert1 };
			}
		}
	}
}
