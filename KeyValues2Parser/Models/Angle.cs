using KeyValues2Parser.Constants;

namespace KeyValues2Parser.Models
{
	public class Angle : IEquatable<Angle>
    {
        public float pitch;
        public float yaw;
        public float roll;

        public Angle(string angle)
        {
            if (string.IsNullOrWhiteSpace(angle))
                return;

            var angleSplit = angle.Split(" ");

            if (angleSplit.Count() != 3)
                return;

            float.TryParse(angleSplit[0], Globalization.Style, Globalization.Culture, out pitch);
            float.TryParse(angleSplit[1], Globalization.Style, Globalization.Culture, out yaw);
            float.TryParse(angleSplit[2], Globalization.Style, Globalization.Culture, out roll);

            pitch = ValidateValue(pitch);
            yaw = ValidateValue(yaw);
            roll = ValidateValue(roll);
        }

        public Angle(float pitch, float yaw, float roll)
        {
            this.pitch = ValidateValue(pitch);
            this.yaw = ValidateValue(yaw);
            this.roll = ValidateValue(roll);
        }


		public override bool Equals(object obj)
		{
			return Equals(obj as Angle);
		}

        public bool Equals(Angle other)
        {
            if (pitch == other.pitch && yaw == other.yaw && roll == other.roll)
                return true;

            return false;
        }


        public static Angle operator +(Angle a, Angle b) => new Angle(ValidateValue(a.pitch + b.pitch), ValidateValue(a.yaw + b.yaw), ValidateValue(a.roll + b.roll));

        public static Angle operator -(Angle a, Angle b) => new Angle(ValidateValue(a.pitch - b.pitch), ValidateValue(a.yaw - b.yaw), ValidateValue(a.roll - b.roll));

        public static Angle operator *(Angle a, Angle b) => new Angle(ValidateValue(a.pitch * b.pitch), ValidateValue(a.yaw * b.yaw), ValidateValue(a.roll * b.roll));

        public static Angle operator /(Angle a, Angle b) => new Angle(ValidateValue(a.pitch / b.pitch), ValidateValue(a.yaw / b.yaw), ValidateValue(a.roll / b.roll));

        public static Angle operator *(Angle a, int b) => new Angle(ValidateValue(a.pitch * b), ValidateValue(a.yaw * b), ValidateValue(a.roll * b));

        public static Angle operator /(Angle a, int b) => new Angle(ValidateValue(a.pitch / b), ValidateValue(a.yaw / b), ValidateValue(a.roll / b));

        public static Angle operator *(Angle a, float b) => new Angle(ValidateValue(a.pitch * b), ValidateValue(a.yaw * b), ValidateValue(a.roll * b));

        public static Angle operator /(Angle a, float b) => new Angle(ValidateValue(a.pitch / b), ValidateValue(a.yaw / b), ValidateValue(a.roll / b));


        public override int GetHashCode()
        {
            int hashPitch = pitch == 0 ? 0 : pitch.GetHashCode();
            int hashYaw = yaw == 0 ? 0 : yaw.GetHashCode();
            int hashRoll = roll == 0 ? 0 : roll.GetHashCode();

            return hashPitch ^ hashYaw ^ hashRoll;
        }


        public string GetStringFormat()
        {
            return $"{ConfigurationSorter.GetFloatInEnglishFormatString(pitch)} {ConfigurationSorter.GetFloatInEnglishFormatString(yaw)} {ConfigurationSorter.GetFloatInEnglishFormatString(roll)}";
        }


        private static float ValidateValue(float value)
        {
            while (value >= 360)
                value -= 360;

            while (value <= -360)
                value += 360;

            return value;
        }
	}
}
