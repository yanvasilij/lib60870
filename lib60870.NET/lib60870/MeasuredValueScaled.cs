using System;

namespace lib60870
{
	public class MeasuredValueScaled : InformationObject
	{
		private int scaledValue;

		public int ScaledValue {
			get {
				return this.scaledValue;
			}
		}

		private QualityDescriptor quality;

		public QualityDescriptor Quality {
			get {
				return this.quality;
			}
		}

		public MeasuredValueScaled (int objectAddress, int value, QualityDescriptor quality)
			: base(objectAddress)
		{
			this.scaledValue = value;
			this.quality = quality;
		}

		public MeasuredValueScaled (ConnectionParameters parameters, byte[] msg, int startIndex) :
			base(parameters, msg, startIndex)
		{
			startIndex += parameters.SizeOfIOA; /* skip IOA */

			scaledValue = msg [startIndex++];
			scaledValue += (msg [startIndex++] * 0x100);

			if (scaledValue > 32767)
				scaledValue = scaledValue - 65536;

			/* parse QDS (quality) */
			quality = new QualityDescriptor (msg [startIndex++]);
		}

		public override void Encode(Frame frame, ConnectionParameters parameters) {
			base.Encode(frame, parameters);

			int valueToEncode;

			if (scaledValue < 0)
				valueToEncode = scaledValue + 65536;
			else
				valueToEncode = scaledValue;

			frame.SetNextByte ((byte)(valueToEncode % 256));
			frame.SetNextByte ((byte)(valueToEncode / 256));

			frame.SetNextByte (quality.EncodedValue);
		}

	}
}
