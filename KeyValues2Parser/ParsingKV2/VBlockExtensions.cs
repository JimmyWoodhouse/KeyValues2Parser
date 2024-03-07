namespace KeyValues2Parser.ParsingKV2
{
	public static class VBlockExtensions
	{
		public static VBlock GetNewVBlock(string id, List<string> lines, int currentLineNum)
		{
			var allLinesInNewVBlock = new List<string>();
			
			var numOfBracketsInside = 0;

			int j = currentLineNum + 1;
			while (j < lines.Count)
			{
				var lineInNewVBlock = lines[j].Replace("\t", string.Empty);
				if (lineInNewVBlock.EndsWith(","))
					lineInNewVBlock = lineInNewVBlock.Substring(0, lineInNewVBlock.Length - 1);

				if (lineInNewVBlock.Trim().Replace("\"", string.Empty) == "}" ||
					lineInNewVBlock.Trim().Replace("\"", string.Empty) == "]")
				{
					numOfBracketsInside--;

					if (numOfBracketsInside == 0)
						break;
				}

				if (lineInNewVBlock.Trim().Replace("\"", string.Empty) == "{" ||
					lineInNewVBlock.Trim().Replace("\"", string.Empty) == "[")
				{
					numOfBracketsInside++;

					if (numOfBracketsInside == 1)
					{
						j++;
						continue;
					}
				}

				allLinesInNewVBlock.Add(lineInNewVBlock);

				j++;
			}

			return new VBlock(id, allLinesInNewVBlock);
		}

		public static VArray GetNewArrayValue(string id, List<string> lines, int currentLineNum)
		{
			var allLinesInArray = new List<string>();
			
			var numOfBracketsInside = 0;

			int j = currentLineNum + 1;
			while (j < lines.Count)
			{
				var lineInArray = lines[j].Replace("\t", string.Empty);
				if (lineInArray.EndsWith(","))
					lineInArray = lineInArray.Substring(0, lineInArray.Length - 1);

				if (lineInArray.Trim().Replace("\"", string.Empty) == "}" ||
					lineInArray.Trim().Replace("\"", string.Empty) == "]")
				{
					numOfBracketsInside--;

					if (numOfBracketsInside == 0)
						break;
				}

				if (lineInArray.Trim().Replace("\"", string.Empty) == "{" ||
					lineInArray.Trim().Replace("\"", string.Empty) == "[")
				{
					numOfBracketsInside++;

					if (numOfBracketsInside == 1)
					{
						j++;
						continue;
					}
				}

				allLinesInArray.Add(lineInArray);

				j++;
			}

			return new VArray(id, allLinesInArray);
		}

		public static KeyValuePair<string, string> GetNewVariable(List<string> linesSplit)
		{
			return new KeyValuePair<string, string>(linesSplit[0], linesSplit.Count > 2 ? linesSplit[2] : string.Empty);
		}
	}
}
