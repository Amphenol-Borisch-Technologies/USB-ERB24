using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.Switching.RelayForms;

namespace ABT.TestSpace.Switching {
    public static class USB_ERB24 {
        // TODO: Convert the UE24 class to a Singleton, like the USB_TO_GPIO class?
        // TODO: Convert internal methods to private ones, after fully debugged/tested.
        //  - If there are more than one USB-ERB24s in the test system, make the UE24 Singleton class a Dictionary of USB-ERB24s, rather than just one USB-ERB24.
        //  - Each USB-ERB24 in the Singleton's Dictionary can be accessed by it's UE24 enum; UE24.S01, UE24.S02...UE24.Snn, for UE24 Singletons 01, 02...nn.
        // NOTE: This class assumes all USB-ERB24 relays are configured for Non-Inverting Logic & Pull-Down/de-energized at power-up.
        // NOTE: USB-ERB24 relays are configurable for either Non-Inverting or Inverting logic, via hardware DIP switch S1.
        //  - Non-Inverting:  Logic low de-energizes the relays, logic high energizes them.
        //  - Inverting:      Logic low energizes the relays, logic high de-energizes them.  
        // NOTE: USB-ERB24 relays are configurable with default power-up states, via hardware DIP switch S2.
        //  - Pull-Up:        Relays are energized at power-up.
        //  - Pull-Down:      Relays are de-energized at power-up.
        //  - https://www.mccdaq.com/PDFs/Manuals/usb-erb24.pdf.
        // NOTE: UE24 enum is a static definition of TestExecutive's MCC USB-ERB24(s).
        // Potential dynamic definition methods for UE24:
        //  - Read them from MCC InstaCal's cb.cfg file.
        //  - Dynamically discover them programmatically: https://www.mccdaq.com/pdfs/manuals/Mcculw_WebHelp/ULStart.htm.
        //  - Specify MCC USB-ERB24s in App.config, then confirm existence during TestExecutive's initialization.
        // NOTE: MCC's InstaCal USB-ERB24's UE24 number indexing begins at 0, guessing because USB device indexing is likely also zero based.
        // - So UE24.E01 numerical value is 0, which is used when constructing a new MccBoard UE24.E01 object:
        // - Instantiation 'new MccBoard((Int32)UE24.E01)' is equivalent to 'new MccBoard(0)'.
        public enum UE24 { E01 }
        public enum R : byte { C01, C02, C03, C04, C05, C06, C07, C08, C09, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22, C23, C24 }
        // NOTE: enum named R instead of RELAYS for concision; consider below:
        //  - Set(UE24.E01, new Dictionary<R, C>() {{R.C01,C.NC}, {R.C02,C.NO}, ... {R.C24,C.NC} });
        //  - Set(UE24.E01, new Dictionary<R, C>() {{RELAYS.C01,C.NC}, {RELAYS.C02,C.NO}, ... {RELAYS.C24,C.NC} });
        // NOTE: R's elements named C## because USB-ERB24's relays are all Form C.
        //  - Also because can't name them R.01, R.02...R.24; identifiers cannot begin with numbers.
        // NOTE: Enumerate Form A relays as R.A01, R.A02...
        // NOTE: Enumerate Form B relays as R.B01, R.B02...
        internal enum PORTS { A, B, CL, CH }

        internal static int[] _ue24bitVector32Masks = GetUE24BitVector32Masks();
        #region Is/Are
        public static bool Is(UE24 UE24, R R, C C) { return Get(UE24, R) == C; }

        public static bool Are(UE24 UE24, HashSet<R> Rs, C C) {
            Dictionary<R, C> RεC = Rs.ToDictionary(r => r, r => C);
            Dictionary<R, C> Are = Get(UE24, Rs);
            return RεC.Count == Are.Count && !RεC.Except(Are).Any();
        }

        public static bool Are(UE24 UE24, Dictionary<R, C> RεC) {
            Dictionary<R, C> Are = Get(UE24, new HashSet<R>(RεC.Keys));
            return RεC.Count == Are.Count && !RεC.Except(Are).Any();
        }

        public static bool Are(UE24 UE24, C C) {
            Dictionary<R, C> Are = Get(UE24);
            bool areEqual = true;
            foreach (KeyValuePair<R, C> kvp in Are) areEqual &= kvp.Value == C;
            return areEqual;
        }

        public static bool Are(HashSet<UE24> UE24s, C C) {
            bool areEqual = true;
            foreach (UE24 UE24 in UE24s) areEqual &= Are(UE24, C);
            return areEqual;
        }

        public static bool Are(HashSet<UE24> UE24s, HashSet<R> Rs, C C) {
            bool areEqual = true;
            foreach (UE24 UE24 in UE24s) areEqual &= Are(UE24, Rs, C);
            return areEqual;
        }

        public static bool Are(Dictionary<UE24, Dictionary<R, C>> UE24εRεC) {
            bool areEqual = true;
            foreach (KeyValuePair<UE24, Dictionary<R, C>> kvp in UE24εRεC) areEqual &= Are(kvp.Key, kvp.Value);
            return areEqual;
        }

        public static bool Are(C C) {
            bool areEqual = true;
            foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) areEqual &= Are(UE24, C);
            return areEqual;
        }
        #endregion Is/Are

        #region Get
        public static C Get(UE24 UE24, R R) {
            MccBoard mccBoard = new MccBoard((int)UE24);
            ErrorInfo errorInfo = mccBoard.DBitIn(DigitalPortType.FirstPortA, (int)R, out DigitalLogicState digitalLogicState);
            ProcessErrorInfo(mccBoard, errorInfo);
            return digitalLogicState == DigitalLogicState.Low ? C.NC : C.NO;
        }

        public static Dictionary<R, C> Get(UE24 UE24, HashSet<R> Rs) {
            Dictionary<R, C> RεC = Get(UE24);
            foreach (R R in Rs) if (!RεC.ContainsKey(R)) RεC.Remove(R);
            return RεC;
        }

        public static Dictionary<R, C> Get(UE24 UE24) {
            MccBoard mccBoard = new MccBoard((int)UE24);
            ushort[] bits = PortsRead(mccBoard);
            uint[] biggerBits = Array.ConvertAll(bits, delegate (ushort uInt16) { return (uint)uInt16; });
            uint relayBits = 0x0000;
            relayBits |= biggerBits[(uint)PORTS.CH] << 00;
            relayBits |= biggerBits[(uint)PORTS.CL] << 04;
            relayBits |= biggerBits[(uint)PORTS.B] << 08;
            relayBits |= biggerBits[(uint)PORTS.A] << 16;
            BitVector32 bitVector32 = new BitVector32((int)relayBits);

            R R; C C; Dictionary<R, C> RεC = new Dictionary<R, C>();
            for (int i = 0; i < _ue24bitVector32Masks.Length; i++) {
                R = (R)Enum.ToObject(typeof(R), i);
                C = bitVector32[_ue24bitVector32Masks[i]] ? C.NO : C.NC;
                RεC.Add(R, C);
            }
            return RεC;
        }

        public static Dictionary<UE24, Dictionary<R, C>> Get(HashSet<UE24> UE24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = Get();
            foreach (UE24 UE24 in UE24s) if (!UE24εRεC.ContainsKey(UE24)) UE24εRεC.Remove(UE24);
            return UE24εRεC;
        }

        public static Dictionary<UE24, Dictionary<R, C>> Get(HashSet<UE24> UE24s, HashSet<R> Rs) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 UE24 in UE24s) UE24εRεC.Add(UE24, Get(UE24, Rs));
            return UE24εRεC;
        }

        public static Dictionary<UE24, Dictionary<R, C>> Get(Dictionary<UE24, R> UE24εR) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            Dictionary<R, C> RεC = new Dictionary<R, C>();
            foreach (KeyValuePair<UE24, R> kvp in UE24εR) {
                RεC.Add(kvp.Value, Get(kvp.Key, kvp.Value));
                UE24εRεC.Add(kvp.Key, RεC);
            }
            return UE24εRεC;
        }

        public static Dictionary<UE24, Dictionary<R, C>> Get() {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) UE24εRεC.Add(UE24, Get(UE24));
            return UE24εRεC;
        }
        #endregion Get

        #region Set
        public static void Set(UE24 UE24, R R, C C) {
            MccBoard mccBoard = new MccBoard((int)UE24);
            ErrorInfo errorInfo = mccBoard.DBitOut(DigitalPortType.FirstPortA, (int)R, C is C.NC ? DigitalLogicState.Low : DigitalLogicState.High);
            ProcessErrorInfo(mccBoard, errorInfo);
        }

        public static void Set(UE24 UE24, HashSet<R> Rs, C C) { Set(UE24, Rs.ToDictionary(r => r, r => C)); }

        public static void Set(UE24 UE24, Dictionary<R, C> RεC) {
            uint bit;
            uint bits0 = 0x0000_0000;
            uint bits1 = 0x0000_0000;

            foreach (KeyValuePair<R, C> kvp in RεC) {
                bit = (uint)1 << (byte)kvp.Key;
                if (kvp.Value == C.NC) bits0 |= bit; // Sets a 1 in each bit0 bit corresponding to NC state in RεC.
                else bits1 |= bit;                   // Sets a 1 in each bit1 bit corresponding to NO state in RεC.
            }
            BitVector32 bit0Vector32 = new BitVector32((int)bits0);
            BitVector32 bit1Vector32 = new BitVector32((int)bits1);
            BitVector32.Section portA = BitVector32.CreateSection(0b1111_1111);
            BitVector32.Section portB = BitVector32.CreateSection(0b1111_1111, portA);
            BitVector32.Section portCL = BitVector32.CreateSection(0b1111, portB);
            BitVector32.Section portCH = BitVector32.CreateSection(0b1111, portCL);

            MccBoard mccBoard = new MccBoard((int)UE24);
            ushort[] ports = PortsRead(mccBoard);

            ports[(int)PORTS.A] |= (ushort)bit1Vector32[portA];
            ports[(int)PORTS.B] |= (ushort)bit1Vector32[portB];
            ports[(int)PORTS.CL] |= (ushort)bit1Vector32[portCL];
            ports[(int)PORTS.CH] |= (ushort)bit1Vector32[portCH];

            ports[(int)PORTS.A] &= (ushort)~bit0Vector32[portA];
            ports[(int)PORTS.B] &= (ushort)~bit0Vector32[portB];
            ports[(int)PORTS.CL] &= (ushort)~bit0Vector32[portCL];
            ports[(int)PORTS.CH] &= (ushort)~bit0Vector32[portCH];

            PortsWrite(mccBoard, ports);
        }

        public static void Set(UE24 UE24, C C) {
            Dictionary<R, C> RεC = new Dictionary<R, C>();
            foreach (R R in Enum.GetValues(typeof(R))) RεC.Add(R, C);
            Set(UE24, RεC);
        }

        public static void Set(HashSet<UE24> UE24s, C C) { foreach (UE24 UE24 in UE24s) { Set(UE24, C); } }

        public static void Set(HashSet<UE24> UE24s, HashSet<R> Rs, C C) { foreach (UE24 UE24 in UE24s) Set(UE24, Rs, C); }

        public static void Set(Dictionary<UE24, Dictionary<R, C>> UE24εRεC) { foreach (KeyValuePair<UE24, Dictionary<R, C>> kvp in UE24εRεC) Set(kvp.Key, kvp.Value); }

        public static void Set(C C) { foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) Set(UE24, C); }
        #endregion Set

        #region private methods
        internal static ushort PortRead(MccBoard mccBoard, DigitalPortType digitalPortType) {
            ErrorInfo errorInfo = mccBoard.DIn(digitalPortType, out ushort dataValue);
            ProcessErrorInfo(mccBoard, errorInfo);
            return dataValue;
        }

        internal static ushort[] PortsRead(MccBoard mccBoard) {
            return new ushort[] {
                PortRead(mccBoard, DigitalPortType.FirstPortA),
                PortRead(mccBoard, DigitalPortType.FirstPortB),
                PortRead(mccBoard, DigitalPortType.FirstPortCL),
                PortRead(mccBoard, DigitalPortType.FirstPortCH)
            };
        }

        internal static void PortWrite(MccBoard mccBoard, DigitalPortType digitalPortType, ushort dataValue) {
            ErrorInfo errorInfo = mccBoard.DOut(digitalPortType, dataValue);
            ProcessErrorInfo(mccBoard, errorInfo);
        }

        internal static void PortsWrite(MccBoard mccBoard, ushort[] ports) {
            PortWrite(mccBoard, DigitalPortType.FirstPortA, ports[(int)PORTS.A]);
            PortWrite(mccBoard, DigitalPortType.FirstPortB, ports[(int)PORTS.B]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCL, ports[(int)PORTS.CL]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCH, ports[(int)PORTS.CH]);
        }

        internal static DigitalPortType GetPort(R R) {
            switch (R) {
                case R r when R.C01 <= r && r <= R.C08: return DigitalPortType.FirstPortA;
                case R r when R.C09 <= r && r <= R.C16: return DigitalPortType.FirstPortB;
                case R r when R.C17 <= r && r <= R.C20: return DigitalPortType.FirstPortCL;
                case R r when R.C21 <= r && r <= R.C24: return DigitalPortType.FirstPortCH;
                default: throw new ArgumentException("Invalid USB-ERB24 DigitalPortType, must be in set '{ FirstPortA, FirstPortB, FirstPortCL, FirstPortCH }'.");
            }
        }

        internal static void ProcessErrorInfo(MccBoard mccBoard, ErrorInfo errorInfo) {
            // Transform C style error-checking to .Net style exceptioning.
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) {
                throw new InvalidOperationException(
                $"{Environment.NewLine}" +
                $"MccBoard BoardNum   : {mccBoard.BoardNum}.{Environment.NewLine}" +
                $"MccBoard BoardName  : {mccBoard.BoardName}.{Environment.NewLine}" +
                $"MccBoard Descriptor : {mccBoard.Descriptor}.{Environment.NewLine}" +
                $"ErrorInfo Value     : {errorInfo.Value}.{Environment.NewLine}" +
                $"ErrorInfo Message   : {errorInfo.Message}.{Environment.NewLine}");
            }
        }

        internal static int[] GetUE24BitVector32Masks() {
            int ue24RelayCount = Enum.GetValues(typeof(R)).Length;
            Debug.Assert(ue24RelayCount == 24);
            int[] ue24BitVector32Masks = new int[ue24RelayCount];
            ue24BitVector32Masks[0] = BitVector32.CreateMask();
            for (int i = 0; i < ue24RelayCount - 1; i++) ue24BitVector32Masks[i + 1] = BitVector32.CreateMask(ue24BitVector32Masks[i]);
            return ue24BitVector32Masks;
        }
        #endregion private methods
    }
}
