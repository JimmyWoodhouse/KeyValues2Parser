namespace KeyValues2Parser
{
	public static class DirectoryAndFileHelpers
	{
        public static void CreateDirectoryIfDoesntExist(string filepath)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
        }
	}
}
