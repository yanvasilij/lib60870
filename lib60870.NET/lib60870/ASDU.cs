using System;
using System.Collections.Generic;

namespace lib60870
{

	public class ASDUParsingException : Exception
	{
	

		public ASDUParsingException (string message) : base(message)
		{
		
		}
	}

	public class ASDU
	{
		private ConnectionParameters parameters;

		private TypeID typeId;

		private byte vsq; /* variable structure qualifier */

		private CauseOfTransmission cot; /* cause */
		private byte oa; /* originator address */
		private bool isTest; /* is message a test message */
		private bool isNegative; /* is message a negative confirmation */

		private int ca; /* Common address */

		private byte[] payload = null;
		private List<InformationObject> informationObjects = null;

		public ASDU(TypeID typeId, CauseOfTransmission cot, bool isTest, bool isNegative, byte oa, int ca, bool isSequence) {
			this.typeId = typeId;
			this.cot = cot;
			this.isTest = isTest;
			this.isNegative = isNegative;
			this.oa = oa;
			this.ca = ca;

			if (isSequence)
				this.vsq = 0x80;
			else
				this.vsq = 0;
		}

		public void AddInformationObject(InformationObject io) {
			if (informationObjects == null)
				informationObjects = new List<InformationObject> ();

			informationObjects.Add (io);

			vsq = (byte) ((vsq & 0x80) | informationObjects.Count);
		}

		public ASDU (ConnectionParameters parameters, byte[] msg, int msgLength)
		{
			this.parameters = parameters;

			int bufPos = 6;

			typeId = (TypeID) msg [bufPos++];
			vsq = msg [bufPos++];

			byte cotByte = msg [bufPos++];

			if ((cotByte & 0x80) != 0)
				isTest = true;
			else
				isTest = false;

			if ((cotByte & 0x40) != 0)
				isNegative = true;
			else
				isNegative = false;

			cot = (CauseOfTransmission) (cotByte & 0x3f);

			if (parameters.SizeOfCOT == 2)
				oa = msg [bufPos++];

			ca = msg [bufPos++];

			if (parameters.SizeOfCA > 1) 
				ca += (msg [bufPos++] * 0x100);

			int payloadSize = msgLength - bufPos;

			payload = new byte[payloadSize];

			/* save payload */
			Buffer.BlockCopy (msg, bufPos, payload, 0, payloadSize);
		}

		public void Encode(Frame frame, ConnectionParameters parameters) {
			frame.SetNextByte ((byte)typeId);
			frame.SetNextByte (vsq);

			byte cotByte = (byte) cot;

			if (isTest)
				cotByte = (byte) (cotByte | 0x80);

			if (isNegative)
				cotByte = (byte) (cotByte | 0x40);

			frame.SetNextByte (cotByte);

			if (parameters.SizeOfCOT == 2)
				frame.SetNextByte ((byte)oa);

			frame.SetNextByte ((byte)(ca % 256));

			if (parameters.SizeOfCA > 1) 
				frame.SetNextByte((byte)(ca / 256));

			if (payload != null)
				frame.AppendBytes (payload);
			else {

				foreach (InformationObject io in informationObjects) {
					io.Encode (frame, parameters);
				}
			}

		}

		public TypeID TypeId {
			get {
				return this.typeId;
			}
		}

		public CauseOfTransmission Cot {
			get {
				return this.cot;
			}
			set {
				this.cot = value;
			}
		}

		public byte Oa {
			get {
				return this.oa;
			}
		}

		public bool IsTest {
			get {
				return this.isTest;
			}
		}

		public bool IsNegative {
			get {
				return this.isNegative;
			}
			set {
				isNegative = value;
			}
		}



		public int Ca {
			get {
				return this.ca;
			}
		}

		public bool IsSquence {
			get {
				if ((vsq & 0x80) != 0)
					return true;
				else
					return false;
			}
		}

		public int NumberOfElements {
			get {
				return (vsq & 0x7f);
			}
		}

		public InformationObject GetElement(int index)
		{
			InformationObject retVal = null;

			int elementSize;

			switch (typeId) {

			case TypeID.M_ME_NB_1:

				elementSize = parameters.SizeOfIOA + 3;

				retVal = new MeasuredValueScaled (parameters, payload, index * elementSize);

				break;

			case TypeID.M_ME_NC_1:

				elementSize = parameters.SizeOfIOA + 5;

				retVal = new MeasuredValueShortFloat (parameters, payload, index * elementSize);

				break;

			case TypeID.M_SP_NA_1:

				elementSize = parameters.SizeOfIOA + 1;

				retVal = new SinglePointInformation(parameters, payload, index * elementSize);

				break;

			case TypeID.M_SP_TB_1:

				elementSize = parameters.SizeOfIOA + 8;

				retVal = new SinglePointWithCP56Time2a (parameters, payload, index * elementSize);

				break;

			case TypeID.M_ME_TE_1:

				elementSize = parameters.SizeOfIOA + 10;

				//TODO check if index is valid
				//TODO check if msg size is large enough!

				retVal = new MeasuredValueScaledWithCP56Time2a (parameters, payload, index * elementSize);

				break;

			case TypeID.M_ME_TF_1:

				elementSize = parameters.SizeOfIOA + 12;

				retVal = new MeasuredValueShortFloatWithCP56Time2a (parameters, payload, index * elementSize);

				break;

			default:
				throw new ASDUParsingException ("Unknown ASDU type id:" + typeId);
			}

			return retVal;
		}


		public override string ToString()
		{
			string ret;

			ret = "TypeID: " + typeId.ToString() + " COT: " + cot.ToString ();

			if (parameters.SizeOfCOT == 2)
				ret += " OA: " + oa;

			if (isTest)
				ret += " [TEST]";

			if (isNegative)
				ret += " [NEG]";

			if (IsSquence)
				ret += " [SEQ]";

			ret += " elements: " + NumberOfElements;

			ret += " CA: " + ca;

			return ret;
		}
	}
}
