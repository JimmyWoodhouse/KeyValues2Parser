using KeyValues2Parser.Constants;
using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser.Models
{
	public static class MeshAndEntityAdjuster
	{
        // TODO - Is this needed? Do prefabs still need their start position rotating, so that everything lines up correctly?
        /*public static string MoveAndRotateStartPositionInPrefab(Prefab prefab, string contents)
		{
			contents = MergeVerticesToString(prefab.origin, contents.Replace("[", string.Empty).Replace("]", string.Empty)); // removes the offset that being in an prefabs causes
			contents = GetRotatedVerticesNewPositionAsString(new Vertices(contents), prefab.origin, prefab.angles.yaw, 'y', false); // removes the rotation that being in an prefabs causes
			contents = "[" + contents + "]";

            return contents;
        }*/


        public static string MoveAndRotateVerticesInPrefab(Prefab prefab, string contents)
        {
            contents = MergeVerticesToString(prefab.origin, contents); // removes the offset that being in an prefabs causes
			contents = GetRotatedVerticesNewPositionAsString(new Vertices(contents), prefab.origin, prefab.angles.yaw, 'y', false); // removes the rotation that being in an prefabs causes

            return contents;
		}


        public static string MoveAndRotateVerticesInInstance(Vertices instanceDifferenceOrigin, Angle instanceDifferenceAngles, string contents)
        {
            contents = MergeVerticesToString(instanceDifferenceOrigin, contents); // removes the offset that being in an instances causes
			contents = GetRotatedVerticesNewPositionAsString(new Vertices(contents), instanceDifferenceOrigin, instanceDifferenceAngles.yaw, 'y', false); // removes the rotation that being in an instances causes

            return contents;
		}


        public static void RotateOverlayVerticesInPrefabAndSetFakeVertices(Prefab prefab, VBlock entityProperties, List<string> allVerticesOffsetsInOverlay, string originalOriginValue)
        {
            for (int i = 0; i < allVerticesOffsetsInOverlay.Count(); i++) // allVerticesOffsetsInOverlay.Count() should be 4
            {
                var overlayVerticesOffset = allVerticesOffsetsInOverlay[i];

                var verticesString = MergeTwoVerticesAsString(MergeVerticesToString(prefab.origin, overlayVerticesOffset), originalOriginValue); // removes the offset that being in an prefabs causes
				verticesString = GetRotatedVerticesNewPositionAsString(new Vertices(verticesString), prefab.origin, prefab.angles.yaw, 'y', false); // removes the rotation that being in an prefabs causes

				AddSingleFakeVertices(entityProperties, i, verticesString);
            }
        }


        public static void AddSingleFakeVertices(VBlock entityProperties, int verticesIndex, string verticesString)
        {
            entityProperties.Variables.Add($"fake_vertices{verticesIndex}", verticesString);
        }


        public static string MergeVerticesToString(Vertices vertices, string verticesString)
        {
            var verticesNew = GetVerticesFromString(verticesString);

            var xNew = vertices.x + verticesNew.x;
            var yNew = vertices.y + verticesNew.y;
            var zNew = vertices.z + verticesNew.z;

            return $"{ConfigurationSorter.GetFloatInEnglishFormatString(xNew)} {ConfigurationSorter.GetFloatInEnglishFormatString(yNew)} {ConfigurationSorter.GetFloatInEnglishFormatString(zNew)}";
        }


        public static string MergeTwoVerticesAsString(string verticesString1, string verticesString2)
        {
            var vertices1New = GetVerticesFromString(verticesString1);
            var vertices2New = GetVerticesFromString(verticesString2);

            var xNew = vertices1New.x + vertices2New.x;
            var yNew = vertices1New.y + vertices2New.y;
            var zNew = vertices1New.z + vertices2New.z;

            return string.Concat(xNew, " ", yNew, " ", zNew);
        }


        public static string MergeAnglesToString(Angle angle, string anglesString)
        {
            var anglesNew = GetAnglesFromString(anglesString);

            var pitchNew = angle.pitch + anglesNew.pitch;
            var yawNew = angle.yaw + anglesNew.yaw;
            var rollNew = angle.roll + anglesNew.roll;

            return string.Concat(pitchNew, " ", yawNew, " ", rollNew);
        }


        public static Vertices GetVerticesFromString(string verticesString)
        {
            var verticesStringSplit = verticesString.Split(" ");

            if (verticesStringSplit.Count() != 3)
                return null;

            float.TryParse(verticesStringSplit[0], Globalization.Style, Globalization.Culture, out var xCasted);
            float.TryParse(verticesStringSplit[1], Globalization.Style, Globalization.Culture, out var yCasted);
            float.TryParse(verticesStringSplit[2], Globalization.Style, Globalization.Culture, out var zCasted);

            return new Vertices(xCasted, yCasted, zCasted);
        }


        public static Angle GetAnglesFromString(string anglesString)
        {
            var anglesStringSplit = anglesString.Split(" ");

            if (anglesStringSplit.Count() != 3)
                return null;

            float.TryParse(anglesStringSplit[0], Globalization.Style, Globalization.Culture, out var pitchCasted);
            float.TryParse(anglesStringSplit[1], Globalization.Style, Globalization.Culture, out var yawCasted);
            float.TryParse(anglesStringSplit[2], Globalization.Style, Globalization.Culture, out var rollCasted);

            return new Angle(pitchCasted, yawCasted, rollCasted);
        }


        public static string GetRotatedVerticesNewPositionAsString(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees, char rotationAxis, bool rotateOnlyTheSelectedAxis)
        {
			rotationAxis = char.Parse(rotationAxis.ToString().ToLower());
            if (rotationAxis != 'x' && rotationAxis != 'y' && rotationAxis != 'z')
            {
                Console.WriteLine("GetRotatedVerticesNewPositionAsString() can't rotate vertices because 'rotationAxis' was not 'x', 'y' or 'z'.");
                return string.Empty;
            }

            Vertices newVertices = null;
			switch (rotationAxis)
            {
                case 'x':
					newVertices = GetRotatedVerticesNewPositionAsVerticesX(verticesToRotate, centerVertices, angleInDegrees, rotateOnlyTheSelectedAxis);
					break;
                case 'y':
					newVertices = GetRotatedVerticesNewPositionAsVerticesY(verticesToRotate, centerVertices, angleInDegrees, rotateOnlyTheSelectedAxis);
					break;
                case 'z':
					newVertices = GetRotatedVerticesNewPositionAsVerticesZ(verticesToRotate, centerVertices, angleInDegrees, rotateOnlyTheSelectedAxis);
					break;
            }

            return newVertices.GetStringFormat();
        }


        public static Vertices GetRotatedVerticesNewPositionAsVerticesX(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees, bool rotateOnlyTheSelectedAxis = false)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
			double sinTheta = Math.Sin(angleInRadians);

			var newVertices = new Vertices(
				(float)(sinTheta * (verticesToRotate.z - centerVertices.z) + cosTheta * (verticesToRotate.x - centerVertices.x) + centerVertices.x),
                verticesToRotate.y,
				rotateOnlyTheSelectedAxis ? (float)verticesToRotate.z : (float)(cosTheta * (verticesToRotate.z - centerVertices.z) - sinTheta * (verticesToRotate.x - centerVertices.x) + centerVertices.z)

			);

            return newVertices;
        }


        public static Vertices GetRotatedVerticesNewPositionAsVerticesY(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees, bool rotateOnlyTheSelectedAxis = false)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            var newVertices = new Vertices(
				rotateOnlyTheSelectedAxis ? verticesToRotate.x : (float)(cosTheta * (verticesToRotate.x - centerVertices.x) - sinTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.x),
                (float)(sinTheta * (verticesToRotate.x - centerVertices.x) + cosTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.y),
                (float)verticesToRotate.z
            );

            return newVertices;
        }


        public static Vertices GetRotatedVerticesNewPositionAsVerticesZ(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees, bool rotateOnlyTheSelectedAxis = false)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
			double cosTheta = Math.Cos(angleInRadians);
			double sinTheta = Math.Sin(angleInRadians);

			var newVertices = new Vertices(
				verticesToRotate.x,
				rotateOnlyTheSelectedAxis ? verticesToRotate.y : (float)(cosTheta * (verticesToRotate.y - centerVertices.y) - sinTheta * (verticesToRotate.z - centerVertices.z) + centerVertices.y),
				(float)(sinTheta * (verticesToRotate.y - centerVertices.y) + cosTheta * (verticesToRotate.z - centerVertices.z) + centerVertices.z)

			);

            return newVertices;
        }


        public static string GetScaledVerticesNewPositionAsString(Vertices verticesToScale, Vertices centerVertices, Vertices scale)
        {
            var newVertices = GetScaledVerticesNewPositionAsVertices(verticesToScale, centerVertices, scale);

            return newVertices.GetStringFormat();
        }


        public static Vertices GetScaledVerticesNewPositionAsVertices(Vertices verticesToScale, Vertices centerVertices, Vertices scale)
        {
            if (scale.Equals(new Vertices(1, 1, 1)))
                return verticesToScale;

            var diffX = verticesToScale.x - centerVertices.x;
            var diffY = verticesToScale.y - centerVertices.y;
            var diffZ = (float)(verticesToScale.z - centerVertices.z);

            var diffMultipliedX = diffX * scale.x;
            var diffMultipliedY = diffY * scale.y;
            var diffMultipliedZ = diffZ * scale.z;

            var newVertices = new Vertices(
                verticesToScale.x + (diffMultipliedX - diffX),
                verticesToScale.y + (diffMultipliedY - diffY),
				(float)(verticesToScale.z + (diffMultipliedZ - diffZ))
			);

			return newVertices;
        }



        // TODO - THIS TAKES VERTICES, NOT MESH FACES - NEEDS FIXING TO ROTATE THE VERTICES
        // Meshes need to be rotated as their vertices are stored as the non-rotated coordinates
        // Doesn't add the mesh origin offset permanently because this is done later on in Mesh()
        public static void RotateAllMeshFaces(string meshOrEntityOrigin, string meshOrEntityAngles, ref List<string> verticesList)
        {
            for (int i = 0; i < verticesList.Count; i++)
            {
                // mesh id is not changed

                // temp add the origin offset
                var verticesWithOriginOffset = GetVerticesFromString(verticesList[i]) + GetVerticesFromString(meshOrEntityOrigin);
                verticesList[i] = verticesWithOriginOffset.GetStringFormat(); // vertices are the distance from the origin by default

                // rotate vertices
				verticesList[i] = GetRotatedVerticesNewPositionAsString(new Vertices(verticesList[i]), GetVerticesFromString(meshOrEntityOrigin), GetAnglesFromString(meshOrEntityAngles).yaw, 'y', false).Replace(",", ".");

                // remove the temp added origin offset
                var verticesWithOriginOffset2 = GetVerticesFromString(verticesList[i]) - GetVerticesFromString(meshOrEntityOrigin);
                verticesList[i] = verticesWithOriginOffset2.GetStringFormat(); // vertices are the distance from the origin by default
            }
        }


        // TODO - THIS TAKES VERTICES, NOT MESH FACES - NEEDS FIXING TO ROTATE THE VERTICES
        // Ignores scaling of prefabs (not sure if it is even possible to scale a prefab ??)
        // Doesn't add the mesh origin offset permanently because this is done later on in Mesh()
        public static void MoveAndRotateAllMeshFacesInPrefab(Prefab prefab, string meshOrEntityOrigin, string meshOrEntityAngles, ref List<string> verticesList)
        {
            RotateAllMeshFaces(meshOrEntityOrigin, meshOrEntityAngles, ref verticesList);

            for (int i = 0; i < verticesList.Count; i++)
            {

                // temp add the origin offset
                var verticesWithOriginOffset = GetVerticesFromString(verticesList[i]) + GetVerticesFromString(meshOrEntityOrigin);
                verticesList[i] = verticesWithOriginOffset.GetStringFormat(); // vertices are the distance from the origin by default

                // rotate the vertices around the prefab origin
                verticesList[i] = MoveAndRotateVerticesInPrefab(prefab, verticesList[i]);

                // remove the temp added origin offset
                var verticesWithOriginOffset2 = GetVerticesFromString(verticesList[i]) - GetVerticesFromString(meshOrEntityOrigin);
                verticesList[i] = verticesWithOriginOffset2.GetStringFormat(); // vertices are the distance from the origin by default
            }
        }


        // TODO - THIS TAKES VERTICES, NOT MESH FACES - NEEDS FIXING TO ROTATE THE VERTICES
        // Ignores scaling of instances (not sure if it is even possible to scale a instance ??)
        // Doesn't add the mesh origin offset permanently because this is done later on in Mesh()
        public static void MoveAndRotateAllMeshFacesInInstance(Vertices instanceOrigin, Angle instanceAngles, Vertices instanceGroupOrigin, Angle instanceGroupAngles, Vertices instanceDifferenceOrigin, Angle instanceDifferenceAngles, Vertices meshOrEntityOriginInInstance, Angle meshOrEntityAnglesInInstance, ref List<string> verticesList)
        {
            RotateAllMeshFaces(meshOrEntityOriginInInstance.GetStringFormat(), meshOrEntityAnglesInInstance.GetStringFormat(), ref verticesList);

            // TODO: everything in here (except calling MoveAndRotateVerticesInInstance()) might be useless, as 'meshOrEntityOriginInInstance' & 'meshOrEntityAnglesInInstance' always come through as new(0,0,0)
            /*for (int i = 0; i < verticesList.Count; i++)
            {

                // temp add the origin offset
                var verticesWithOriginOffset = GetVerticesFromString(verticesList[i]) + meshOrEntityOriginInInstance; // + instanceGroupOrigin;
                verticesList[i] = verticesWithOriginOffset.GetStringFormat(); // vertices are the distance from the origin by default

                // rotate the vertices around the instance origin
                verticesList[i] = MoveAndRotateVerticesInInstance(instanceOrigin, instanceAngles, verticesList[i]);

                // remove the temp added origin offset
                var verticesWithOriginOffset2 = GetVerticesFromString(verticesList[i]) - meshOrEntityOriginInInstance; // - instanceGroupOrigin;
                verticesList[i] = verticesWithOriginOffset2.GetStringFormat(); // vertices are the distance from the origin by default
            }*/

            RotateAllMeshFaces(instanceGroupOrigin.GetStringFormat(), instanceGroupAngles.GetStringFormat(), ref verticesList);
        }
	}
}
