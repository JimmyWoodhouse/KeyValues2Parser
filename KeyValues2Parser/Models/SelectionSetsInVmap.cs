using KeyValues2Parser.Constants;
using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser.Models
{
	public class SelectionSetsInVmap
    {
        public VSelectionSet? ExampleSelectionSet;


        public SelectionSetsInVmap()
        { }


		public List<VSelectionSet?> GetAllInList()
		{
            List<VSelectionSet?> list = new()
            {
                ExampleSelectionSet
            };

            return list;
		}


		public VSelectionSet? GetSelectionSet(string selectionSetName)
        {
            if (SelectionSetNames.ExampleSelectionSetName.Equals(selectionSetName))
                return ExampleSelectionSet;

            //Console.WriteLine("Could not find selection set ID using selection set name");

            return null;
        }


        public Guid? GetSelectionSetId(string selectionSetName)
        {
            var selectionSet = GetSelectionSet(selectionSetName);

            if (selectionSet == null)
            {
                //Console.WriteLine("Could not find selection set ID using selection set name");

                return null;
            }

            return selectionSet.Id;
        }


        public string? GetSelectionSetName(Guid selectionSetId)
        {
            if (selectionSetId.Equals(ExampleSelectionSet))
                return SelectionSetNames.GetMainSelectionSetNameInList(SelectionSetNames.ExampleSelectionSetName);

            Console.WriteLine("Could not find selection set name using selection set ID");

            return null;
        }
    }
}
