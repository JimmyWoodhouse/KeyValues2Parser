using KeyValues2Parser.Constants;
using KeyValues2Parser.Models;
using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser
{
	public static class ParsedVMapDataGatherer
	{
        internal static SelectionSetsInVmap selectionSetsInMainVmap { get; private set; }
        internal static Dictionary<Guid, SelectionSetsInVmap> selectionSetsInPrefabByPrefabEntityId { get; private set; } = new();


		public static ParsedVMapData? GetParsedVMapData(string[] args)
        {
            var successfullyHandledArgs = ConfigurationSorter.HandleArgs(args);
            if (!successfullyHandledArgs)
                return null;

			ConfigurationSorter.dmxConvertFilepath = string.Concat(GameConfigurationValues.binFolderPath, @"win64\dmxconvert.exe");

			VMapContents? vmapContents = ParseVmap();
            if (vmapContents == null)
                return null;

            return new ParsedVMapData(selectionSetsInMainVmap, selectionSetsInPrefabByPrefabEntityId, vmapContents);
		}

		private static VMapContents? ParseVmap()
		{
			VMapContents? vmapContents = ConfigurationSorter.SetVmapAndContentsAndConfigurationValues(ConfigurationSorter.dmxConvertFilepath, ConfigurationSorter.vmapDecodedFolderPath);
			if (vmapContents == null)
            {
                return null;
            }

			Console.WriteLine("Fixing up main VMAP...");
            SortMainVmap(vmapContents.AllEntities, vmapContents.AllWorldMeshes, vmapContents.AllInstanceGroups);
            Console.WriteLine("Finished fixing up main vmap");

			// get instances
            List<VBlock> allInstanceGroups = ConfigurationSorter.GetAllInstanceGroupsInSpecificVmap(ConfigurationSorter.vmap, ConfigurationSorter.hiddenElementIdsInMainVmap);
            List<VBlock> allInstances = ConfigurationSorter.GetAllInstancesInSpecificVmap(ConfigurationSorter.vmap, ConfigurationSorter.hiddenElementIdsInMainVmap);

            Console.WriteLine("Parsing prefabs...");
            var successfullyParsedPrefabs = ConfigurationSorter.SortPrefabs(GameConfigurationValues.vmapName, vmapContents.AllPrefabs, false, allInstanceGroups, allInstances);
            if (!successfullyParsedPrefabs)
            {
                return null;
            }

            Console.WriteLine();
            Console.WriteLine("Finished parsing prefabs");

            SetSelectionSetIdsInMainVmap();
            SetSelectionSetIdsInPrefabsByPrefabEntityId();

            ConfigurationSorter.LinkAllChildrenElementsIdsThatNeedSearchingFor(vmapContents.AllEntities, vmapContents.AllWorldMeshes, vmapContents.AllInstanceGroups, vmapContents.AllInstances);

			return vmapContents;
		}


        private static void CorrectOverlayOriginsAndAngles(IEnumerable<VBlock> entityList)
        {
            foreach (var entity in entityList)
            {
                var entityProperties = entity.InnerBlocks.First(x => x.Id == "entity_properties");
                if (entityProperties != null)
                {
                    // overlays (before the fake mesh is created, the vertices need rotating)
                    if (entityProperties.Variables.ContainsKey("classname") && (entityProperties.Variables["classname"].ToLower() == "info_overlay"))
                    {
                        var originVBlock = entity.Variables["origin"];
                        var anglesVBlock = entity.Variables["angles"];

                        var width = entityProperties.Variables["width"];
                        var height = entityProperties.Variables["height"];

                        if (width != null && height != null)
                        {
                            var origin = new Vertices(originVBlock);
                            var angles = anglesVBlock == null ? new Angle(0, 0, 0) : new Angle(anglesVBlock);

                            var pos1 = (origin + new Vertices(float.Parse(width, Globalization.Style, Globalization.Culture) / 2, float.Parse(height, Globalization.Style, Globalization.Culture) / 2, 0)).GetStringFormat();
                            var pos2 = (origin + new Vertices(-float.Parse(width, Globalization.Style, Globalization.Culture) / 2, float.Parse(height, Globalization.Style, Globalization.Culture) / 2, 0)).GetStringFormat();
                            var pos3 = (origin + new Vertices(-float.Parse(width, Globalization.Style, Globalization.Culture) / 2, -float.Parse(height, Globalization.Style, Globalization.Culture) / 2, 0)).GetStringFormat();
                            var pos4 = (origin + new Vertices(float.Parse(width, Globalization.Style, Globalization.Culture) / 2, -float.Parse(height, Globalization.Style, Globalization.Culture) / 2, 0)).GetStringFormat();

                            var allVerticesOffsetsInOverlay = new List<string>() { pos1, pos2, pos3, pos4 };
                            for (int i = 0; i < allVerticesOffsetsInOverlay.Count(); i++) // allVerticesOffsetsInOverlay.Count() should be 4
                            {
                                var overlayVerticesOffset = allVerticesOffsetsInOverlay[i];

							    var verticesString = MeshAndEntityAdjuster.GetRotatedVerticesNewPositionAsString(new Vertices(overlayVerticesOffset), origin, angles.yaw, 'y', false); // removes the rotation

							    MeshAndEntityAdjuster.AddSingleFakeVertices(entityProperties, i, verticesString);
                            }
                        }
                    }
                }
            }
        }


        private static void SortMainVmap(List<VBlock> allEntities, List<VBlock> allWorldMeshes, List<VBlock> allInstanceGroups)
        {
            // correct overlay origins and angles
            Console.WriteLine("Correcting overlay origins and angles...");
            CorrectOverlayOriginsAndAngles(allEntities);
            Console.WriteLine("Finished correcting overlay origins and angles");


            // correct entity meshes' origins and angles
            foreach (var entity in allEntities)
            {
                var entityOrigin = entity.Variables.First(x => x.Key == "origin").Value;
                var entityAngles = entity.Variables.First(x => x.Key == "angles").Value;

                ////
                var entityMeshes = entity.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                    .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null)).ToList();

                // ignores entity meshes inside instances because they are already sorted out
                List<VBlock> entityMeshesInsideAnInstanceGroups = new();
                foreach (var instanceGroup in allInstanceGroups)
                {
                    var entityMeshesInsideInstanceGroup = instanceGroup.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                        .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                    if (entityMeshesInsideInstanceGroup == null || !entityMeshesInsideInstanceGroup.Any())
                        continue;

                    entityMeshesInsideAnInstanceGroups.AddRange(entityMeshesInsideInstanceGroup);
                }
                //

                foreach (var entityMesh in entityMeshes.Where(x => !entityMeshesInsideAnInstanceGroups.Any(y => Guid.Parse(y.Variables.First(z => z.Key == "id").Value) == Guid.Parse(x.Variables.First(z => z.Key == "id").Value))))
                {
                    var allVerticesInEntityMesh = entityMesh?
                            .InnerBlocks.Where(z => z.Id == "meshData" && z.InnerBlocks != null)?
                                .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "vertexData" && a.Arrays != null)?
                                    .SelectMany(a => a.Arrays.Where(b => b.Id == "streams" && b.InnerBlocks != null)?
                                        .SelectMany(b => b.InnerBlocks.Where(c => c.Id == "CDmePolygonMeshDataStream" && c.Arrays != null)?
                                            .Select(c => c.Arrays.FirstOrDefault(d => d.Id == "data"))))).FirstOrDefault();

                    if (allVerticesInEntityMesh == null)
                        continue;

                    var entityMeshOrigin = entityMesh.Variables.First(x => x.Key == "origin").Value;
                    var entityMeshAngles = entityMesh.Variables.First(x => x.Key == "angles").Value;
                    //var entityMeshScales = entityMesh.Variables.First(x => x.Key == "scales").Value; // TODO: should it be scaled? I think the vertices positions values are changed by Hammer instead when scaling

                    var allVerticesInEntityMeshList = allVerticesInEntityMesh.AllLinesInArrayByLineSplit.ToList();

                    if (!allVerticesInEntityMeshList.Any())
                        continue;


                    //MeshAndEntityAdjuster.RotateAllMeshFaces(entityMeshOrigin, entityMeshAngles, ref allVerticesInEntityMeshList);

                    //var entityAndEntityMeshAnglesCombined = (new Angle(entityAngles) + new Angle(entityMeshAngles)).GetStringFormat(); // entity meshes seem to need to combine angles but not origin
                    //MeshAndEntityAdjuster.RotateAllMeshFaces(entityMeshOrigin, new Angle(0,0,0).GetStringFormat(), ref allVerticesInEntityMeshList);


                    allVerticesInEntityMesh.SetAllLinesInArrayByLineSplit(allVerticesInEntityMeshList);
                }
                ////
            }


            // ignores meshes inside instances because they are already sorted out
            List<VBlock> meshesInsideAnInstanceGroups = new();
            foreach (var instanceGroup in allInstanceGroups)
            {
                var meshesInsideInstanceGroup = instanceGroup.Arrays.Where(x => x.Id == "children" && x.InnerBlocks != null)?
                    .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "CMapMesh" && y.InnerBlocks != null));

                if (meshesInsideInstanceGroup == null || !meshesInsideInstanceGroup.Any())
                    continue;

                meshesInsideAnInstanceGroups.AddRange(meshesInsideInstanceGroup);
            }
            //

            // correct mesh origins and angles
            foreach (var mesh in allWorldMeshes.Where(x => !meshesInsideAnInstanceGroups.Any(y => Guid.Parse(y.Variables.First(z => z.Key == "id").Value) == Guid.Parse(x.Variables.First(z => z.Key == "id").Value))))
            {
                ////
                var allVerticesInMesh = mesh.InnerBlocks.Where(x => x.Id == "meshData" && x.InnerBlocks != null)?
                    .SelectMany(x => x.InnerBlocks.Where(y => y.Id == "vertexData" && y.Arrays != null)?
                        .SelectMany(y => y.Arrays.Where(z => z.Id == "streams" && z.InnerBlocks != null)?
                            .SelectMany(z => z.InnerBlocks.Where(a => a.Id == "CDmePolygonMeshDataStream" && a.Arrays != null)?
                                .Select(a => a.Arrays.FirstOrDefault(b => b.Id == "data"))))).FirstOrDefault();

                if (allVerticesInMesh == null)
                    continue;

                var meshOrigin = mesh.Variables.First(x => x.Key == "origin").Value;
                var meshAngles = mesh.Variables.First(x => x.Key == "angles").Value;
                //var meshScales = mesh.Variables.First(x => x.Key == "scales").Value; // TODO: should it be scaled? I think the vertices positions values are changed by Hammer instead when scaling

                var allVerticesInMeshList = allVerticesInMesh.AllLinesInArrayByLineSplit.ToList();

                if (!allVerticesInMeshList.Any())
                    continue;

                MeshAndEntityAdjuster.RotateAllMeshFaces(meshOrigin, meshAngles, ref allVerticesInMeshList);

                allVerticesInMesh.SetAllLinesInArrayByLineSplit(allVerticesInMeshList);
                ////
            }
        }

        private static void SetSelectionSetIdsInMainVmap()
        {
            selectionSetsInMainVmap = GetSelectionSetsInVmap(ConfigurationSorter.vmap);
        }


        private static void SetSelectionSetIdsInPrefabsByPrefabEntityId()
        {
            if (ConfigurationSorter.prefabEntityIdsByVmap != null && ConfigurationSorter.prefabEntityIdsByVmap.Any())
            {
                foreach (var prefab in ConfigurationSorter.prefabEntityIdsByVmap)
                {
                    var selectionSets = GetSelectionSetsInVmap(prefab.Key);

                    if (selectionSetsInPrefabByPrefabEntityId.ContainsKey(prefab.Value))
                        continue;

                    selectionSetsInPrefabByPrefabEntityId.Add(prefab.Value, selectionSets);
                }
            }
        }


        private static SelectionSetsInVmap GetSelectionSetsInVmap(VMap vmap)
        {
            var selectionSetsInVmap = new SelectionSetsInVmap()
            {
                ExampleSelectionSet = GetSelectionSetInVmap(vmap, SelectionSetNames.ExampleSelectionSetName),
            };

            return selectionSetsInVmap;
        }


        private static VSelectionSet? GetSelectionSetInVmap(VMap vmap, List<string> selectionSetNamesList)
        {
            if (vmap.CMapRootElement == null)
                return null;

            var selectionSetRaw = (from x in vmap.CMapRootElement.InnerBlocks
                          where x.Id == "rootSelectionSet"
                          from y in x.Arrays
                          where y.Id == "children"
                          from z in y.InnerBlocks
                          where z.Id == "CMapSelectionSet"
                          from a in z.Variables
                          where a.Key == "selectionSetName"
                          where selectionSetNamesList.Any(x => x.ToLower() == a.Value.ToLower())
                          select z)
            .FirstOrDefault();

            if (selectionSetRaw == null)
                return null;

            return new VSelectionSet(selectionSetRaw);
        }
	}
}