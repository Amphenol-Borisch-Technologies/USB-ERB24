using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.Switching.RelayForms;

namespace ABT.TestSpace.Switching {
    public sealed class USB_ERB24 {
        private static readonly USB_ERB24 _only = new USB_ERB24();
        public static readonly MccBoard _mccBoard = new MccBoard((Int32)UE24.E01);
        private static ErrorInfo _errorInfo = new ErrorInfo();
        // Singleton pattern requires explicit static constructor to tell C# compiler not to mark type as beforefieldinit.
        // https://csharpindepth.com/articles/singleton
        private USB_ERB24() { }
        public static USB_ERB24 Only { get { return _only; } }
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
        public enum R : Byte { C01, C02, C03, C04, C05, C06, C07, C08, C09, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22, C23, C24 }
        // NOTE: enum named R instead of RELAYS for concision; consider below:
        //  - Set(UE24.E01, new Dictionary<R, C>() {{R.C01,C.NC}, {R.C02,C.NO}, ... {R.C24,C.NC} });
        //  - Set(UE24.E01, new Dictionary<R, C>() {{RELAYS.C01,C.NC}, {RELAYS.C02,C.NO}, ... {RELAYS.C24,C.NC} });
        // NOTE: R's elements named C## because USB-ERB24's relays are all Form C.
        //  - Also because can't name them R.01, R.02...R.24; identifiers cannot begin with numbers.
        // NOTE: Enumerate Form A relays as R.A01, R.A02...
        // NOTE: Enumerate Form B relays as R.B01, R.B02...
        internal enum PORTS { A, B, CL, CH }

        internal static Int32[] _ue24bitVector32Masks = GetUE24BitVector32Masks();
        #region Is/Are
        public static Boolean Is(UE24 UE24, R R, C C) { return Get(UE24, R) == C; }

        public static Boolean Are(UE24 UE24, HashSet<R> Rs, C C) {
            Dictionary<R, C> RεC = Rs.ToDictionary(r => r, r => C);
            Dictionary<R, C> Are = Get(UE24, Rs);
            return RεC.Count == Are.Count && !RεC.Except(Are).Any();
        }

        public static Boolean Are(UE24 UE24, Dictionary<R, C> RεC) {
            Dictionary<R, C> Are = Get(UE24, new HashSet<R>(RεC.Keys));
            return RεC.Count == Are.Count && !RεC.Except(Are).Any();
        }

        public static Boolean Are(UE24 UE24, C C) {
            Dictionary<R, C> Are = Get(UE24);
            Boolean areEqual = true;
            foreach (KeyValuePair<R, C> kvp in Are) areEqual &= kvp.Value == C;
            return areEqual;
        }

        // Below 3 methods mainly useful for parallelism, when testing multiple UUTs concurrently, with each UE24 wired identically to test 1 UUT.
        public static Boolean Are(HashSet<UE24> UE24s, C C) {
            Boolean areEqual = true;
            foreach (UE24 UE24 in UE24s) areEqual &= Are(UE24, C);
            return areEqual;
        }

        public static Boolean Are(HashSet<UE24> UE24s, HashSet<R> Rs, C C) {
            Boolean areEqual = true;
            foreach (UE24 UE24 in UE24s) areEqual &= Are(UE24, Rs, C);
            return areEqual;
        }

        public static Boolean Are(Dictionary<UE24, Dictionary<R, C>> UE24εRεC) {
            Boolean areEqual = true;
            foreach (KeyValuePair<UE24, Dictionary<R, C>> kvp in UE24εRεC) areEqual &= Are(kvp.Key, kvp.Value);
            return areEqual;
        }

        public static Boolean Are(C C) {
            Boolean areEqual = true;
            foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) areEqual &= Are(UE24, C);
            return areEqual;
        }
        #endregion Is/Are

        #region Get
        public static C Get(UE24 UE24, R R) {
            _errorInfo =_mccBoard.DBitIn(DigitalPortType.FirstPortA, (Int32)R, out DigitalLogicState digitalLogicState);
            ProcessErrorInfo();
            return digitalLogicState == DigitalLogicState.Low ? C.NC : C.NO;
        }

        public static Dictionary<R, C> Get(UE24 UE24, HashSet<R> Rs) {
            Dictionary<R, C> RεC = Get(UE24);
            foreach (R R in Rs) if (!RεC.ContainsKey(R)) RεC.Remove(R);
            return RεC;
        }

        public static Dictionary<R, C> Get(UE24 UE24) {
            // Obviously, can utilize MccBoard.DBitIn to read individual bits, instead of MccBoard.DIn to read multiple bits:
            // - But, the USB-ERB24's reads it's relay states by reading its internal 82C55's ports.
            // - These ports appear to operate similarly to MccBoard's DIn function, that is, they read the 82C55's 
            //   port bits simultaneously.
            // - If correct, then utilizing MccBoard's DBitIn function could be very inefficient compared to
            //   the DIn function, since DBitIn would have to perform similar bit-shifting/bit-setting functions as this method does,
            //   once for each of the USB-ERB24's 24 relays, as opposed to 4 times for this method.
            // - Regardless, if preferred, below /*,*/commented code can replace the entirety of this method.
            /*
            DigitalLogicState digitalLogicState;
            R R;  C C;  Dictionary<R, C> RεC = new Dictionary<R, C>();
            for (Int32 i = 0; i < Enum.GetValues(typeof(R)).Length; i++) {
                _errorInfo = _mccBoard.DBitIn(DigitalPortType.FirstPortA, i, out digitalLogicState);
                ProcessErrorInfo ();
                R = (R)Enum.ToObject(typeof(R), i);
                C = digitalLogicState == DigitalLogicState.Low ? C.NC : C.NO;
                RεC.Add(R, C);
            }
            return RεC;
            */

            UInt16[] portBits = PortsRead();
            UInt32[] biggerPortBits = Array.ConvertAll(portBits, delegate (UInt16 uInt16) { return (UInt32)uInt16; });
            UInt32 relayBits = 0x0000;
            relayBits |= biggerPortBits[(UInt32)PORTS.CH] << 00;
            relayBits |= biggerPortBits[(UInt32)PORTS.CL] << 04;
            relayBits |= biggerPortBits[(UInt32)PORTS.B]  << 08;
            relayBits |= biggerPortBits[(UInt32)PORTS.A]  << 16;
            BitVector32 bitVector32 = new BitVector32((Int32)relayBits);

            R R; C C; Dictionary<R, C> RεC = new Dictionary<R, C>();
            for (Int32 i = 0; i < _ue24bitVector32Masks.Length; i++) {
                R = (R)Enum.ToObject(typeof(R), i);
                C = bitVector32[_ue24bitVector32Masks[i]] ? C.NO : C.NC;
                RεC.Add(R, C);
            }
            return RεC;
        }

        // Below 3 methods mainly useful for parallelism, when testing multiple UUTs concurrently, with each UE24 wired identically to test 1 UUT.
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
            _errorInfo = _mccBoard.DBitOut(DigitalPortType.FirstPortA, (Int32)R, C is C.NC ? DigitalLogicState.Low : DigitalLogicState.High);
            ProcessErrorInfo();
        }

        public static void Set(UE24 UE24, HashSet<R> Rs, C C) { Set(UE24, Rs.ToDictionary(r => r, r => C)); }

        public static void Set(UE24 UE24, Dictionary<R, C> RεC) {
            // This method only sets relay states for relays explicitly declared in RεC.
            //  - That is, if RεC = {{R.C01, C.NO}, {R.C02, C.NC}}, then only relays R.C01 & R.C02 will have their states actively set, respectively to NO & NC.
            //  - Relay states R.C03, R.C04...R.C24 remain as they were:
            //      - Relays that were NC remain NC.
            //      - Relays that were NO remain NO.
            //
            // Obviously, can utilize MccBoard.DBitOut to write individual bits, instead of MccBoard.DOut to write multiple bits:
            // - But, the USB-ERB24's energizes/de-energizes it's relay by writing its internal 82C55's ports.
            // - These ports appear to operate similarly to MccBoard's DOut function, that is, they write the 
            //   entire port's bits simultaneously.
            // - If correct, then utilizing MccBoard's DBitOut function could be very inefficient compared to
            //   the DOut function, since it'd have to perform similar And/Or functions as this method does,
            //   once for every call to DBitOut.
            //  - Thought is that DOut will write the bits as simultaneously as possible, at least more so than DBitOut.
            // - Regardless, if preferred, below /*,*/commented code can replace the entirety of this method.
            /*
            foreach (KeyValuePair<R, C> kvp in RεC) {
                _errorInfo = _mccBoard.DBitOut(DigitalPortType.FirstPortA, (Int32)kvp.Key, kvp.Value == C.NC ? DigitalLogicState.Low: DigitalLogicState.High);
                ProcessErrorInfo();
            }
            */

            UInt32 relayBit;
            UInt32 bits_NC = 0xFFFF_FFFF; // bits_NC utilize Boolean And logic.
            UInt32 bits_NO = 0x0000_0000; // bits_NO utilize Boolean Or logic.

            foreach (KeyValuePair<R, C> kvp in RεC) {
                relayBit = (UInt32)1 << (Byte)kvp.Key;
                if (kvp.Value == C.NC) bits_NC ^= relayBit; // Sets a 0 in bits_NC for each explicitly assigned NC state in RεC.
                else bits_NO |= relayBit;                   // Sets a 1 in bits_NO for each explicitly assigned NO state in RεC.
            }

            BitVector32 bv32_NC = new BitVector32((Int32)bits_NC);
            BitVector32 bv32_NO = new BitVector32((Int32)bits_NO);
            BitVector32.Section sectionPortA  = BitVector32.CreateSection(0b1111_1111);
            BitVector32.Section sectionPortB  = BitVector32.CreateSection(0b1111_1111, sectionPortA);
            BitVector32.Section sectionPortCL = BitVector32.CreateSection(0b1111, sectionPortB);
            BitVector32.Section sectionPortCH = BitVector32.CreateSection(0b1111, sectionPortCL);

            UInt16[] portStates = PortsRead();

            portStates[(Int32)PORTS.A]  &= (UInt16)bv32_NC[sectionPortA]; // &= sets portStates bits low for each explicitly assigned NC state in RεC.
            portStates[(Int32)PORTS.B]  &= (UInt16)bv32_NC[sectionPortB];
            portStates[(Int32)PORTS.CL] &= (UInt16)bv32_NC[sectionPortCL];
            portStates[(Int32)PORTS.CH] &= (UInt16)bv32_NC[sectionPortCH];

            portStates[(Int32)PORTS.A]  |= (UInt16)bv32_NO[sectionPortA]; // |= sets portStates bits high for each explicitly assigned NO state in RεC.
            portStates[(Int32)PORTS.B]  |= (UInt16)bv32_NO[sectionPortB];
            portStates[(Int32)PORTS.CL] |= (UInt16)bv32_NO[sectionPortCL];
            portStates[(Int32)PORTS.CH] |= (UInt16)bv32_NO[sectionPortCH];

            PortsWrite(portStates);
        }

        public static void Set(UE24 UE24, C C) {
            Dictionary<R, C> RεC = new Dictionary<R, C>();
            foreach (R R in Enum.GetValues(typeof(R))) RεC.Add(R, C);
            Set(UE24, RεC);
        }

        // Below 3 methods mainly useful for parallelism, when testing multiple UUTs concurrently, with each UE24 wired identically to test 1 UUT.
        public static void Set(HashSet<UE24> UE24s, C C) { foreach (UE24 UE24 in UE24s) { Set(UE24, C); } }

        public static void Set(HashSet<UE24> UE24s, HashSet<R> Rs, C C) { foreach (UE24 UE24 in UE24s) Set(UE24, Rs, C); }

        public static void Set(Dictionary<UE24, Dictionary<R, C>> UE24εRεC) { foreach (KeyValuePair<UE24, Dictionary<R, C>> kvp in UE24εRεC) Set(kvp.Key, kvp.Value); }

        public static void Set(C C) { foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) Set(UE24, C); }
        #endregion Set

        #region private methods
        internal static UInt16 PortRead(DigitalPortType digitalPortType) {
            _errorInfo = _mccBoard.DIn(digitalPortType, out UInt16 dataValue);
            ProcessErrorInfo();
            return dataValue;
        }

        internal static UInt16[] PortsRead() {
            return new UInt16[] {
                PortRead(DigitalPortType.FirstPortA),
                PortRead(DigitalPortType.FirstPortB),
                PortRead(DigitalPortType.FirstPortCL),
                PortRead(DigitalPortType.FirstPortCH)
            };
        }

        internal static void PortWrite(DigitalPortType digitalPortType, UInt16 dataValue) {
            _errorInfo = _mccBoard.DOut(digitalPortType, dataValue);
            ProcessErrorInfo();
        }

        internal static void PortsWrite(UInt16[] ports) {
            PortWrite(DigitalPortType.FirstPortA, ports[(Int32)PORTS.A]);
            PortWrite(DigitalPortType.FirstPortB, ports[(Int32)PORTS.B]);
            PortWrite(DigitalPortType.FirstPortCL, ports[(Int32)PORTS.CL]);
            PortWrite(DigitalPortType.FirstPortCH, ports[(Int32)PORTS.CH]);
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

        internal static void ProcessErrorInfo() {
            // Transform C style error-checking to .Net style exceptioning.
            if (_errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) {
                throw new InvalidOperationException(
                $"{Environment.NewLine}" +
                $"MccBoard BoardNum   : {_mccBoard.BoardNum}.{Environment.NewLine}" +
                $"MccBoard BoardName  : {_mccBoard.BoardName}.{Environment.NewLine}" +
                $"MccBoard Descriptor : {_mccBoard.Descriptor}.{Environment.NewLine}" +
                $"ErrorInfo Value     : {_errorInfo.Value}.{Environment.NewLine}" +
                $"ErrorInfo Message   : {_errorInfo.Message}.{Environment.NewLine}");
            }
        }

        internal static Int32[] GetUE24BitVector32Masks() {
            Int32 ue24RelayCount = Enum.GetValues(typeof(R)).Length;
            Debug.Assert(ue24RelayCount == 24);
            Int32[] ue24BitVector32Masks = new Int32[ue24RelayCount];
            ue24BitVector32Masks[0] = BitVector32.CreateMask();
            for (Int32 i = 0; i < ue24RelayCount - 1; i++) ue24BitVector32Masks[i + 1] = BitVector32.CreateMask(ue24BitVector32Masks[i]);
            return ue24BitVector32Masks;
        }
        #endregion private methods
    }
}
