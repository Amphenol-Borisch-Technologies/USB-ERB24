using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.TestExec.Switching.RelayForms;

namespace ABT.TestSpace.TestExec.Switching.USB_ERB24 {
        public enum UE { B0, B1 } // USB-ERB24 Boards.
        public enum R : Byte { C01, C02, C03, C04, C05, C06, C07, C08, C09, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22, C23, C24 } // USB-ERB24 Relays, all Form C.
        // NOTE: UE enum is a static definition of TestExecutive's MCC USB-ERB24(s).
        // Potential dynamic definition methods for USB_ERB24s:
        //  - Read them from MCC InstaCal's cb.cfg file.
        //  - Dynamically discover them programmatically: https://www.mccdaq.com/pdfs/manuals/Mcculw_WebHelp/ULStart.htm.
        //  - Specify MCC USB-ERB24s in TestExecutive.config.xml.
        // NOTE: MCC's InstaCal USB-ERB24 indexing begins at 0, guessing because USB device indexing is likely also zero based.
        // - So UE.B0's numerical value is 0, which is used when constructing a new MccBoard UE.B0 object:
        // - Instantiation 'new MccBoard((Int32)UE.B0)' is equivalent to 'new MccBoard(0)'.
        // NOTE: enum named R instead of RELAYS for concision; consider below:
        //  - Set(UE.B0, new Dictionary<R, C.S>() {{R.C01,C.S.NC}, {R.C02,C.S.NO}, ... {R.C24,C.S.NC} });
        //  - Set(UE.B0, new Dictionary<RELAYS, C.S>() {{RELAYS.C01,C.S.NC}, {RELAYS.C02,C.S.NO}, ... {RELAYS.C24,C.S.NC} });
        // NOTE: R's items named C## because USB-ERB24's relays are all Form C.

    public sealed class UE24 {
        // NOTE: Most of this class is compatible with MCC's USB-ERB08 Relay Board, essentially a USB-ERB24 but with only 8 Form C relays instead of the USB-ERB24's 24.
        // - Some portions are specific to the USB-ERB24 however; examples are enum R containing 24 relays & enum PORTS containing 24 bits.
        // NOTE: This class assumes all USB-ERB24 relays are configured for Non-Inverting Logic & Pull-Down/de-energized at power-up.
        // NOTE: USB-ERB24 relays are configurable for either Non-Inverting or Inverting logic, via hardware DIP switch S1.
        //  - Non-Inverting:  Logic low de-energizes the relays, logic high energizes them.
        //  - Inverting:      Logic low energizes the relays, logic high de-energizes them.  
        // NOTE: USB-ERB24 relays are configurable with default power-up states, via hardware DIP switch S2.
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
            this.USB_ERB24s = new Dictionary<UE, MccBoard>() {
                {UE.B0, new MccBoard((Int32)UE.B0)},
                {UE.B1, new MccBoard((Int32)UE.B1)}
            };
        }
        #region methods

        #region Is/Are
        public static Boolean Is(UE ue, R r, C.S s) { return Get(ue, r) == s; }

        public static Boolean Are(UE ue, HashSet<R> rs, C.S s) {
            Dictionary<R, C.S> RεS = rs.ToDictionary(r => r, r => s);
            Dictionary<R, C.S> Are = Get(ue, rs);
            return RεS.Count == Are.Count && !RεS.Except(Are).Any();
        }

        public static Boolean Are(UE ue, Dictionary<R, C.S> RεS) {
            Dictionary<R, C.S> Actual = Get(ue, new HashSet<R>(RεS.Keys));
            Boolean are = true;
            foreach(KeyValuePair<R, C.S> kvp in RεS) are &= (kvp.Value == Actual[kvp.Key]);
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
        /// Get(UE ue, R r) wraps MccBoard's DBitIn(DigitalPortType portType, int bitNum, out DigitalLogicState bitValue) function.
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
        /// Set(UE ue, R r, C.S s) wraps MccBoard's DBitOut(DigitalPortType portType, int bitNum, DigitalLogicState bitValue) function.
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

        public static void Set(UE ue, Dictionary<R, C.S> RεS) { foreach (KeyValuePair<R, C.S> kvp in RεS) Set(ue, kvp.Key, kvp.Value); }

        public static void Set(UE ue, C.S s) {
            Dictionary<R, C.S> RεS = new Dictionary<R, C.S>();
            foreach (R r in Enum.GetValues(typeof(R))) RεS.Add(r, s);
            Set(ue, RεS);
            Debug.Assert(Are(ue, RεS));
        }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(HashSet<UE> ues, C.S s) { foreach (UE ue in ues) { Set(ue, s); } }

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
        public static void Set(Dictionary<UE, Dictionary<R, C.S>> UEεRεS) { foreach (KeyValuePair<UE, Dictionary<R, C.S>> kvp in UEεRεS) Set(kvp.Key, kvp.Value); }

        /// <summary>
        /// Mainly useful for parallelism, when testing multiple UUTs concurrently, with each B wired identically to test 1 UUT.
        /// </summary>
        public static void Set(C.S s) {
            foreach (UE ue in Enum.GetValues(typeof(UE))) Set(ue, s);
            Debug.Assert(Are(s));
        }
        #endregion Set

        #region private methods
        private enum PORTS { A, B, CL, CH }
        /// <summary>
        /// Private methods PortRead() & PortsRead() wrap MccBoard's DIn(DigitalPortType portType, out ushort dataValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        private static UInt16 PortRead(MccBoard mccBoard, DigitalPortType digitalPortType) {
            ErrorInfo errorInfo = mccBoard.DIn(digitalPortType, out UInt16 dataValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
            return dataValue;
        }

        private static UInt16[] PortsRead(MccBoard mccBoard) {
            return new UInt16[] {
                PortRead(mccBoard, DigitalPortType.FirstPortA),
                PortRead(mccBoard, DigitalPortType.FirstPortB),
                PortRead(mccBoard, DigitalPortType.FirstPortCL),
                PortRead(mccBoard, DigitalPortType.FirstPortCH)
            };
        }

        /// <summary>
        /// Private methods PortWrite() & PortsWrite() wrap MccBoard's DOut(DigitalPortType portType, ushort dataValue) function,
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// As yet they've no client methods.
        /// </summary>
        private static void PortWrite(MccBoard mccBoard, DigitalPortType digitalPortType, UInt16 dataValue) {
            ErrorInfo errorInfo = mccBoard.DOut(digitalPortType, dataValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
        }

        private static void PortsWrite(MccBoard mccBoard, UInt16[] ports) {
            PortWrite(mccBoard, DigitalPortType.FirstPortA, ports[(Int32)PORTS.A]);
            PortWrite(mccBoard, DigitalPortType.FirstPortB, ports[(Int32)PORTS.B]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCL, ports[(Int32)PORTS.CL]);
            PortWrite(mccBoard, DigitalPortType.FirstPortCH, ports[(Int32)PORTS.CH]);
        }

        private static DigitalPortType GetPort(R r) {
            switch (r) {
                case R relay when R.C01 <= relay && relay <= R.C08: return DigitalPortType.FirstPortA;
                case R relay when R.C09 <= relay && relay <= R.C16: return DigitalPortType.FirstPortB;
                case R relay when R.C17 <= relay && relay <= R.C20: return DigitalPortType.FirstPortCL;
                case R relay when R.C21 <= relay && relay <= R.C24: return DigitalPortType.FirstPortCH;
                default: throw new NotImplementedException(TestExecutive.NotImplementedMessageEnum(typeof(R)));
            }
        }

        private static void ProcessErrorInfo(MccBoard mccBoard, ErrorInfo errorInfo) {
            throw new InvalidOperationException(
                $"{Environment.NewLine}" +
                $"MccBoard BoardNum   : {mccBoard.BoardNum}.{Environment.NewLine}" +
                $"MccBoard BoardName  : {mccBoard.BoardName}.{Environment.NewLine}" +
                $"MccBoard Descriptor : {mccBoard.Descriptor}.{Environment.NewLine}" +
                $"ErrorInfo Value     : {errorInfo.Value}.{Environment.NewLine}" +
                $"ErrorInfo Message   : {errorInfo.Message}.{Environment.NewLine}");
        }
        #endregion private methods
        #endregion methods
    }
}