using KeyValues2Parser.Constants;

namespace KeyValues2Parser.ParsingKV2
{
	public class VBlock
    {
        public string Id;
        public IDictionary<string, string> Variables = new Dictionary<string, string>();
        public List<VBlock> InnerBlocks = new();
        public List<VArray> Arrays = new();

        public int numberOfVmapLinesInside { get; private set; }

        public VBlock(string id)
        {
            Id = id;
        }

        public VBlock(string id, List<string> lines)
        {
            numberOfVmapLinesInside = lines.Count;

            Id = id;

            var insideBinaryMultiLineValue = false; // binary values are skipped

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (string.IsNullOrWhiteSpace(line.Trim()))
                    continue;

                if (insideBinaryMultiLineValue)
                {
                    if (line.Trim() == $"\"")
                        insideBinaryMultiLineValue = false;

                    continue;
                }

                var numOfLinesSplit = line.Trim().Split("\"").ToList().Count; // needs to be calculated separately since some values can be empty ""
                var linesSplit = line.Trim().Split("\"").Where(x => !string.IsNullOrWhiteSpace(x.Trim())).ToList();
                //linesSplit.RemoveAll(x => string.IsNullOrWhiteSpace(x.Trim()));

                //Variables.Add(linesSplit[0], linesSplit[2]);

                if (line.Contains("[ ]"))
                {
                    Arrays.Add(new VArray(linesSplit[0], new List<string>()));
                }
                else if (numOfLinesSplit <= 3)
                {
                    if (i >= lines.Count - 1)
                        continue;

                    if (lines[i+1].Trim() == "{")
                    {
                        InnerBlocks.Add(VBlockExtensions.GetNewVBlock(linesSplit[0], lines, i));
                        i += InnerBlocks.Last().numberOfVmapLinesInside + 2; // skips past the array after parsing it + it's two brackets lines
                    }
                    else if (lines[i+1].Trim() == "[")
                    {
                        var vArray = VBlockExtensions.GetNewArrayValue(linesSplit[0], lines, i);
                        Arrays.Add(vArray);
                        i += vArray.NumberOfVmapLinesInside + 2; // skips past the inner block after parsing it + it's two brackets lines
                    }
                }
                else if (numOfLinesSplit <= 5)
                {
                    if (linesSplit[1] == "binary") // skips binary
                    {
                        insideBinaryMultiLineValue = true;
                        i++; // skips the opening quote line
                        continue;
                    }

                    if (linesSplit[1].ToLower().Contains("_array"))
                    {
                        var vArray = VBlockExtensions.GetNewArrayValue(linesSplit[0], lines, i);
                        Arrays.Add(vArray);
                        i += vArray.NumberOfVmapLinesInside + 2; // skips past the array after parsing it + it's two brackets lines
                    }
                    else
                    {
                        InnerBlocks.Add(VBlockExtensions.GetNewVBlock(linesSplit[0], lines, i));
                        i += InnerBlocks.Last().numberOfVmapLinesInside + 2; // skips past the inner block after parsing it + it's two brackets lines
                    }
                }
                else if (numOfLinesSplit <= 7)
                {
                    Variables.Add(VBlockExtensions.GetNewVariable(linesSplit));
                }
                else
                {
                    /* should never (I think) be entered for a VMAP, but will be for a Model */

                    // everything is on 1 line, needs splitting up
                    if (linesSplit[1].Contains("_array"))
                    {
                        var arrayKeyName = linesSplit[0];

                        linesSplit.RemoveRange(0, 2);
                        linesSplit.RemoveAll(x => x == ", ");
                        linesSplit.RemoveAll(x => x == ",");
                        linesSplit.RemoveAll(x => x.Contains("["));
                        linesSplit.RemoveAll(x => x.Contains("]"));

                        var vArray = new VArray(arrayKeyName, linesSplit);
                        Arrays.Add(vArray);
                        // note: doesn't need to skip any lines because everything is on 1 line
                    }
                }
            }
        }

        // Used when found in instances
        public VBlock(VBlock vblock)
        {
            Id = vblock.Id.ToString();

            foreach (var variable in vblock.Variables)
            {
                Variables.Add(variable.Key.ToString(), variable.Value.ToString());
            }

            Variables.Add("fake_instance_id", Guid.NewGuid().ToString()); // set to differentiate when a VArray is created from a template (rather than just having the Id in the instance group, which will be shared across all instances

            foreach (var innerBlock in vblock.InnerBlocks)
            {
                InnerBlocks.Add(new VBlock(innerBlock));
            }

            foreach (var array in vblock.Arrays)
            {
                Arrays.Add(new VArray(array));
            }

            numberOfVmapLinesInside = int.Parse(vblock.numberOfVmapLinesInside.ToString(), Globalization.Style, Globalization.Culture);
        }
    }
}
