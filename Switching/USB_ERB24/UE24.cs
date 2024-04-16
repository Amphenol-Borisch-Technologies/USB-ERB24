using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.TestExec.Switching.RelayForms;

namespace ABT.TestSpace.TestExec.Switching.USB_ERB24 {
    public enum UE { B0, B1 } // USB-ERB24 Boards.
    public enum R : Byte { C01, C02, C03, C04, C05, C06, C07, C08, C09, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22, C23, C24 } // USB-ERB24 Relays, all Form C.
                                                                                                                                                    // NOTE:  UE enum is a static definition of TestExecutive's MCC USB-ERB24(s).
                                                                                                                                                    // Potential dynamic definition methods for USB_ERB24s:
                                                                                                                                                    //  - Read them from MCC InstaCal's cb.cfg file.
                                                                                                                                                    //  - Dynamically discover them programmatically: https://www.mccdaq.com/pdfs/manuals/Mcculw_WebHelp/ULStart.htm.
                                                                                                                                                    //  - Specify MCC USB-ERB24s in TestExecutive.GlobalConfigurationFile.
                                                                                                                                                    // NOTE:  MCC's InstaCal USB-ERB24 indexing begins at 0, guessing because USB device indexing is likely also zero based.
                                                                                                                                                    // - So UE.B0's numerical value is 0, which is used when constructing a new MccBoard UE.B0 Object:
                                                                                                                                                    // - Instantiation 'new MccBoard((Int32)UE.B0)' is equivalent to 'new MccBoard(0)'.
                                                                                                                                                    // NOTE:  enum named R instead of RELAYS for concision; consider below:
                                                                                                                                                    //  - Set(UE.B0, new Dictionary<R, C.S>() {{R.C01,C.S.NC}, {R.C02,C.S.NO}, ... {R.C24,C.S.NC} });
                                                                                                                                                    //  - Set(UE.B0, new Dictionary<RELAYS, C.S>() {{RELAYS.C01,C.S.NC}, {RELAYS.C02,C.S.NO}, ... {RELAYS.C24,C.S.NC} });
                                                                                                                                                    // NOTE:  R's items named C## because USB-ERB24's relays are all Form C.

    // TODO:  Eventually; change UE24 from a singleton to a static class; consistent with ABT.TestSpace.TestExec.SCPI_VISA_Instruments namespace classes.
    public sealed class UE24 {
        // NOTE:  Most of this class is compatible with MCC's USB-ERB08 Relay Board, essentially a USB-ERB24 but with only 8 Form C relays instead of the USB-ERB24's 24.
        // - Some portions are specific to the USB-ERB24 however; examples are enum R containing 24 relays & enum PORT containing 24 bits.
        // NOTE:  This class assumes all USB-ERB24 relays are configured for Non-Inverting Logic & Pull-Down/de-energized at power-up.
        // NOTE:  USB-ERB24 relays are configurable for either Non-Inverting or Inverting logic, via hardware DIP switch S1.
        //  - Non-Inverting:  Logic low de-energizes the relays, logic high energizes them.
        //  - Inverting:      Logic low energizes the relays, logic high de-energizes them.  
        // NOTE:  USB-ERB24 relays are configurable with default power-up states, via hardware DIP switch S2.
        //  - Pull-Up:        Relays are energized at power-up.
        //  - Pull-Down:      Relays are de-energized at power-up.
        //  - https://www.mccdaq.com/PDFs/Manuals/usb-erb24.pdf.

        public Dictionary<UE, MccBoard> USB_ERB24s;
        private readonly static UE24 _only = new UE24();

        public static UE24 Only { get { return _only; } }
        static UE24() { }
        // Singleton pattern requires explicit static constructor to tell C# compiler not to mark type as beforefieldinit.
        // https://csharpindepth.com/articles/singleton
        private UE24() {
            USB_ERB24s = new Dictionary<UE, MccBoard>();
            foreach (UE ue in Enum.GetValues(typeof(UE))) USB_ERB24s.Add(ue, new MccBoard((Int32)ue));
            Dictionary<PORTS, BitVector32.Section> PortSections = new Dictionary<PORTS, BitVector32.Section>();
            PortSections.Add(PORTS.A, BitVector32.CreateSection(0b1111_1111));
            PortSections.Add(PORTS.B, BitVector32.CreateSection(0b1111_1111, PortSections[PORTS.A]));
            PortSections.Add(PORTS.CL, BitVector32.CreateSection(0b1111_1111, PortSections[PORTS.B]));
            PortSections.Add(PORTS.CH, BitVector32.CreateSection(0b1111_1111, PortSections[PORTS.CL]));
            _ue24bitVector32Masks = GetUE24BitVector32Masks();
        }
        internal enum PORTS { A, B, CL, CH }
        private static readonly Dictionary<PORTS, BitVector32.Section> PortSections;
        internal static Int32[] _ue24bitVector32Masks;

        #region methods

        public static void Initialize() {
            // NOTE:  Mustn't invoke TestExecutive.CT_EmergencyStop.ThrowIfCancellationRequested(); on Initialize() or it's invoked methods Reset() & Clear().
            foreach (UE ue in _only.USB_ERB24s.Keys) {
                foreach (R r in Enum.GetValues(typeof(R))) {
                    ErrorInfo errorInfo = _only.USB_ERB24s[ue].DBitOut(DigitalPortType.FirstPortA, (Int32)r, DigitalLogicState.Low);
                    if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(_only.USB_ERB24s[ue], errorInfo);
                }
            }
        }

        public static Boolean Initialized() {
            return Are(C.S.NC);
        }

        #region Is/Are
        public static Boolean Is(UE ue, R r, C.S s) {
            return Get(ue, r) == s;
        }

        public static Boolean Are(UE ue, HashSet<R> rs, C.S s) {
            Dictionary<R, C.S> RεS = rs.ToDictionary(r => r, r => s);
            Dictionary<R, C.S> Are = Get(ue, rs);
            return RεS.Count == Are.Count && !RεS.Except(Are).Any();
        }

        public static Boolean Are(UE ue, Dictionary<R, C.S> RεS) {
            Dictionary<R, C.S> Actual = Get(ue, new HashSet<R>(RεS.Keys));
            Boolean are = true;
            foreach (KeyValuePair<R, C.S> kvp in RεS) are &= kvp.Value == Actual[kvp.Key];
            return are;
        }

        public static Boolean Are(UE ue, C.S s) {
            Dictionary<R, C.S> Are = Get(ue);
            Boolean areEqual = true;
            foreach (KeyValuePair<R, C.S> kvp in Are) areEqual &= kvp.Value == s;
            return areEqual;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Boolean Are(HashSet<UE> ues, C.S s) {
            Boolean areEqual = true;
            foreach (UE ue in ues) areEqual &= Are(ue, s);
            return areEqual;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Boolean Are(HashSet<UE> ues, HashSet<R> rs, C.S s) {
            Boolean areEqual = true;
            foreach (UE ue in ues) areEqual &= Are(ue, rs, s);
            return areEqual;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Boolean Are(Dictionary<UE, Dictionary<R, C.S>> UEεRεS) {
            Boolean areEqual = true;
            foreach (KeyValuePair<UE, Dictionary<R, C.S>> kvp in UEεRεS) areEqual &= Are(kvp.Key, kvp.Value);
            return areEqual;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Boolean Are(C.S s) {
            Boolean areEqual = true;
            foreach (UE ue in Enum.GetValues(typeof(UE))) areEqual &= Are(ue, s);
            return areEqual;
        }
        #endregion Is/Are

        #region Get
        /// <summary>
        /// Get(UE ue, R r) wraps MccBoard's DBitIn(DigitalPortType portType, Int32 bitNum, out DigitalLogicState bitValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        public static C.S Get(UE ue, R r) {
            ErrorInfo errorInfo = _only.USB_ERB24s[ue].DBitIn(DigitalPortType.FirstPortA, (Int32)r, out DigitalLogicState digitalLogicState);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(_only.USB_ERB24s[ue], errorInfo);
            return digitalLogicState == DigitalLogicState.Low ? C.S.NC : C.S.NO;
        }

        public static Dictionary<R, C.S> Get(UE ue, HashSet<R> rs) {
            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            foreach (R r in rs) RεS.Add(r, Get(ue, r));
            return RεS;
        }

        // TODO:  Soon; make this method work, replacing Get(UE ue) method.
        public static Dictionary<R, C.S> Get_InProgress(UE ue) {
            // Obviously, can utilize MccBoard.DBitIn to read individual bits, instead of MccBoard.DIn to read multiple bits:
            // - But, the USB-ERB24's reads it's relay states by reading its internal 82C55's ports.
            // - These ports appear to operate similarly to MccBoard's DIn function, that is, they read the 82C55's 
            //   port bits simultaneously.
            // - If correct, then utilizing MccBoard's DBitIn function could be very inefficient compared to
            //   the DIn function, since DBitIn would have to perform similar bit-shifting/bit-setting functions as this method does,
            //   once for each of the USB-ERB24's 24 relays, as opposed to 4 times for this method.
            // - Regardless, if preferred, below /*,*/commented code can replace the entirety of this method.
            /*
                public static Dictionary<R, C.S> Get(UE ue) {
                    Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
                    foreach (R r in Enum.GetValues(typeof(R))) RεS.Add(r, Get(ue, r));
                    return RεS;
                }
            */

            UInt16[] portBits = PortsRead(Only.USB_ERB24s[ue]);
            UInt32[] biggerPortBits = Array.ConvertAll(portBits, delegate (UInt16 uint16) { return (UInt32)uint16; });
            UInt32 relayBits = 0x0000;
            relayBits |= biggerPortBits[(UInt32)PORTS.CH] << 00;
            relayBits |= biggerPortBits[(UInt32)PORTS.CL] << 04;
            relayBits |= biggerPortBits[(UInt32)PORTS.B] << 08;
            relayBits |= biggerPortBits[(UInt32)PORTS.A] << 16;
            BitVector32 bitVector32 = new BitVector32((Int32)relayBits);

            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            for (Int32 i = 0; i < _ue24bitVector32Masks.Length; i++) RεS.Add((R)i, bitVector32[_ue24bitVector32Masks[i]] ? C.S.NO : C.S.NC);
            return RεS;
        }

        public static Dictionary<R, C.S> Get(UE ue) {
            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            foreach (R r in Enum.GetValues(typeof(R))) RεS.Add(r, Get(ue, r));
            return RεS;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Dictionary<UE, Dictionary<R, C.S>> Get(HashSet<UE> ues) {
            Dictionary<UE, Dictionary<R, C.S>> UEεRεS = Get();
            foreach (UE ue in ues) if (!UEεRεS.ContainsKey(ue)) UEεRεS.Remove(ue);
            return UEεRεS;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Dictionary<UE, Dictionary<R, C.S>> Get(HashSet<UE> ues, HashSet<R> rs) {
            Dictionary<UE, Dictionary<R, C.S>> UEεRεS = new Dictionary<UE, Dictionary<R, C.S>>();
            foreach (UE ue in ues) UEεRεS.Add(ue, Get(ue, rs));
            return UEεRεS;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Dictionary<UE, Dictionary<R, C.S>> Get(Dictionary<UE, R> UEεR) {
            Dictionary<UE, Dictionary<R, C.S>> UEεRεS = new Dictionary<UE, Dictionary<R, C.S>>();
            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            foreach (KeyValuePair<UE, R> kvp in UEεR) {
                RεS.Add(kvp.Value, Get(kvp.Key, kvp.Value));
                UEεRεS.Add(kvp.Key, RεS);
            }
            return UEεRεS;
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static Dictionary<UE, Dictionary<R, C.S>> Get() {
            Dictionary<UE, Dictionary<R, C.S>> UEεRεS = new Dictionary<UE, Dictionary<R, C.S>>();
            foreach (UE ue in Enum.GetValues(typeof(UE))) UEεRεS.Add(ue, Get(ue));
            return UEεRεS;
        }
        #endregion Get

        #region Set
        /// <summary>
        /// Set(UE ue, R r, C.S s) wraps MccBoard's DBitOut(DigitalPortType portType, Int32 bitNum, DigitalLogicState bitValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        public static void Set(UE ue, R r, C.S s) {
            ErrorInfo errorInfo = _only.USB_ERB24s[ue].DBitOut(DigitalPortType.FirstPortA, (Int32)r, s is C.S.NC ? DigitalLogicState.Low : DigitalLogicState.High);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(_only.USB_ERB24s[ue], errorInfo);
            Debug.Assert(Is(ue, r, s));
        }

        public static void Set(UE ue, HashSet<R> rs, C.S s) {
            Set(ue, rs.ToDictionary(r => r, r => s));
            Debug.Assert(Are(ue, rs.ToDictionary(r => r, r => s)));
        }

        // TODO:  Soon; make this method work, replacing Set(UE ue, Dictionary<R, C.S> RεS) method.
        public static void Set_InProgress(UE ue, Dictionary<R, C.S> RεS) {
            // This method only sets relay states for relays explicitly declared in RεS.
            //  - That is, if RεS = {{R.C01, C.S.NO}, {R.C02, C.S.NC}}, then only relays R.C01 & R.C02 will have their states actively set, respectively to NO & NC.
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
                public static void Set(UE ue, Dictionary<R, C.S> RεS) {
                    foreach (KeyValuePair<R, C.S> kvp in RεS) Set(ue, kvp.Key, kvp.Value);
                }
            */

            UInt32 relayBit;
            UInt32 bits_NC = 0xFFFF_FFFF; // bits_NC utilize Boolean And logic.
            UInt32 bits_NO = 0x0000_0000; // bits_NO utilize Boolean Or logic.

            foreach (KeyValuePair<R, C.S> kvp in RεS) {
                relayBit = (UInt32)1 << (Byte)kvp.Key;
                if (kvp.Value == C.S.NC) bits_NC ^= relayBit;  // Sets a 0 in bits_NC for each explicitly assigned NC state in RεS.
                else bits_NO |= relayBit;                      // Sets a 1 in bits_NO for each explicitly assigned NO state in RεS.
            }

            BitVector32 bv32_NC = new BitVector32((Int32)bits_NC);
            BitVector32 bv32_NO = new BitVector32((Int32)bits_NO);

            UInt16[] portStates = PortsRead(Only.USB_ERB24s[ue]);

            portStates[(Int32)PORTS.A] &= (UInt16)bv32_NC[PortSections[PORTS.A]]; // &= sets portStates bits low for each explicitly assigned NC state in RεS.
            portStates[(Int32)PORTS.B] &= (UInt16)bv32_NC[PortSections[PORTS.B]];
            portStates[(Int32)PORTS.CL] &= (UInt16)bv32_NC[PortSections[PORTS.CL]];
            portStates[(Int32)PORTS.CH] &= (UInt16)bv32_NC[PortSections[PORTS.CH]];

            portStates[(Int32)PORTS.A] |= (UInt16)bv32_NO[PortSections[PORTS.A]]; // |= sets portStates bits high for each explicitly assigned NO state in RεS.
            portStates[(Int32)PORTS.B] |= (UInt16)bv32_NO[PortSections[PORTS.B]];
            portStates[(Int32)PORTS.CL] |= (UInt16)bv32_NO[PortSections[PORTS.CL]];
            portStates[(Int32)PORTS.CH] |= (UInt16)bv32_NO[PortSections[PORTS.CH]];

            PortsWrite(Only.USB_ERB24s[ue], portStates);
            Debug.Assert(PortsRead(Only.USB_ERB24s[ue]).SequenceEqual(portStates));
        }

        public static void Set(UE ue, Dictionary<R, C.S> RεS) {
            foreach (KeyValuePair<R, C.S> kvp in RεS) Set(ue, kvp.Key, kvp.Value);
        }

        public static void Set(UE ue, C.S s) {
            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            foreach (R r in Enum.GetValues(typeof(R))) RεS.Add(r, s);
            Set(ue, RεS);
            Debug.Assert(Are(ue, RεS));
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(HashSet<UE> ues, C.S s) {
            foreach (UE ue in ues) { Set(ue, s); }
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(HashSet<UE> ues, HashSet<R> rs, C.S s) {
            foreach (UE ue in ues) {
                Set(ue, rs, s);
                Debug.Assert(Are(ue, rs, s));
            }
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(Dictionary<UE, Dictionary<R, C.S>> UEεRεS) {
            foreach (KeyValuePair<UE, Dictionary<R, C.S>> kvp in UEεRεS) Set(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(C.S s) {
            foreach (UE ue in Enum.GetValues(typeof(UE))) Set(ue, s);
            Debug.Assert(Are(s));
        }
        #endregion Set

        #region internal methods
        /// <summary>
        /// Private methods PortRead() & PortsRead() wrap MccBoard's DIn(DigitalPortType portType, out UInt16 dataValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        internal static UInt16 PortRead(MccBoard mccBoard, DigitalPortType digitalPortType) {
            ErrorInfo errorInfo = mccBoard.DIn(digitalPortType, out UInt16 dataValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
            return dataValue;
        }

        internal static UInt16[] PortsRead(MccBoard mccBoard) {
            return new UInt16[] {
                PortRead(mccBoard, DigitalPortType.FirstPortA),
                PortRead(mccBoard, DigitalPortType.FirstPortB),
                PortRead(mccBoard, DigitalPortType.FirstPortCL),
                PortRead(mccBoard, DigitalPortType.FirstPortCH)
            };
        }

        /// <summary>
        /// Private methods PortWrite() & PortsWrite() wrap MccBoard's DOut(DigitalPortType portType, UInt16 dataValue) function,
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// As yet they've no client methods.
        /// </summary>
        internal static void PortWrite(MccBoard mccBoard, DigitalPortType digitalPortType, UInt16 dataValue) {
            ErrorInfo errorInfo = mccBoard.DOut(digitalPortType, dataValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
        }

        internal static void PortsWrite(MccBoard mccBoard, UInt16[] ports) {
            PortWrite(mccBoard, DigitalPortType.FirstPortA, ports[(Int32)PORTS.A]);
            PortWrite(mccBoard, DigitalPortType.FirstPortB, ports[(Int32)PORTS.B]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCL, ports[(Int32)PORTS.CL]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCH, ports[(Int32)PORTS.CH]);
        }

        internal static DigitalPortType GetPort(R r) {
            switch (r) {
                case R relay when R.C01 <= relay && relay <= R.C08: return DigitalPortType.FirstPortA;
                case R relay when R.C09 <= relay && relay <= R.C16: return DigitalPortType.FirstPortB;
                case R relay when R.C17 <= relay && relay <= R.C20: return DigitalPortType.FirstPortCL;
                case R relay when R.C21 <= relay && relay <= R.C24: return DigitalPortType.FirstPortCH;
                default: throw new NotImplementedException($"Unimplemented Enum item; switch/case must support all items in enum '{String.Join(",", Enum.GetNames(typeof(R)))}'.");
            }
        }

        internal static void ProcessErrorInfo(MccBoard mccBoard, ErrorInfo errorInfo) {
            throw new InvalidOperationException(
                $"{Environment.NewLine}" +
                $"MccBoard BoardNum   : {mccBoard.BoardNum}.{Environment.NewLine}" +
                $"MccBoard BoardName  : {mccBoard.BoardName}.{Environment.NewLine}" +
                $"MccBoard Descriptor : {mccBoard.Descriptor}.{Environment.NewLine}" +
                $"ErrorInfo Value     : {errorInfo.Value}.{Environment.NewLine}" +
                $"ErrorInfo Message   : {errorInfo.Message}.{Environment.NewLine}");
        }

        private static Int32[] GetUE24BitVector32Masks() {
            Int32 ue24RelayCount = Enum.GetValues(typeof(R)).Length;
            Debug.Assert(ue24RelayCount == 24);
            Int32[] ue24BitVector32Masks = new Int32[ue24RelayCount];
            ue24BitVector32Masks[0] = BitVector32.CreateMask();
            for (Int32 i = 0; i < ue24RelayCount - 1; i++) ue24BitVector32Masks[i + 1] = BitVector32.CreateMask(ue24BitVector32Masks[i]);
            return ue24BitVector32Masks;
        }
        #endregion internal methods
        #endregion methods
    }
}