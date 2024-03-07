namespace KeyValues2Parser.Constants
{
	public static class SelectionSetNames
    {
        public static readonly List<string> ExampleSelectionSetName = new() { "Example Selection Set Name" };


        public static string GetMainSelectionSetNameInList(List<string> selectionSetNamesList)
        {
            return selectionSetNamesList.First();
        }
    }
}
