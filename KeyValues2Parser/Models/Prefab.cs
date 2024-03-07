using KeyValues2Parser.ParsingKV2;

namespace KeyValues2Parser.Models
{
	public class Prefab
    {
        public string filepath;

        public Guid originalId; // the original ID
        public Guid id; // the original ID or; if set; a fake instance id, to avoid duplicate ids for templates

        public Vertices origin;
        public Angle angles;
        public Vertices scales;

        public string targetMapPath;
        public int fixup_style;

        public Prefab(VBlock prefab)
        {
            originalId = Guid.Parse(prefab.Variables["id"]);
            id = prefab.Variables.ContainsKey("fake_instance_id") ? Guid.Parse(prefab.Variables["fake_instance_id"]) : Guid.Parse(prefab.Variables["id"]);

            origin = prefab.Variables.ContainsKey("origin") ? new Vertices(prefab.Variables["origin"]) : null;
            angles = prefab.Variables.ContainsKey("angles") ? new Angle(prefab.Variables["angles"]) : null;
            scales = prefab.Variables.ContainsKey("scales") ? new Vertices(prefab.Variables["scales"]) : null;

            if (prefab.Variables.ContainsKey("fake_instance_origin_difference"))
            {
                var fullMovingDistanceVertices = new Vertices(prefab.Variables["fake_instance_origin_difference"]);

                var xOriginMoving90AnglePercentage = (fullMovingDistanceVertices.x / 90);
                var yOriginMoving90AnglePercentage = (fullMovingDistanceVertices.y / 90);

                var xOriginMovingDistanceMultiplier = (xOriginMoving90AnglePercentage / 180) % 90;
                var yOriginMovingDistanceMultiplier = (yOriginMoving90AnglePercentage / 180) % 90;


                //if (prefab.Variables.ContainsKey("fake_instance_origin_difference"))
                    origin += new Vertices(fullMovingDistanceVertices.x * xOriginMovingDistanceMultiplier, fullMovingDistanceVertices.y * yOriginMovingDistanceMultiplier, (float)fullMovingDistanceVertices.z);
                /*if (prefab.Variables.ContainsKey("fake_instance_angles_difference"))
                    angles += new Angle(prefab.Variables["fake_instance_angles_difference"]);*/
            }


            targetMapPath = prefab.Variables.ContainsKey("targetMapPath") ? prefab.Variables["targetMapPath"] : null;
            if (targetMapPath.ToLower().StartsWith("/"))
                targetMapPath = new string(targetMapPath.Skip(1).ToArray());
            if (targetMapPath.ToLower().StartsWith(@"\\"))
                targetMapPath = new string(targetMapPath.Skip(2).ToArray());
            if (targetMapPath.ToLower().StartsWith("maps/"))
                targetMapPath = new string(targetMapPath.Skip(5).ToArray());
            if (targetMapPath.ToLower().StartsWith(@"maps\\"))
                targetMapPath = new string(targetMapPath.Skip(6).ToArray());
            if (!targetMapPath.ToLower().EndsWith(".vmap"))
                targetMapPath += ".vmap";
        }
    }
}
