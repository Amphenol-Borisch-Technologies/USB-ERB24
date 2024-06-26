﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ABT.TestSpace.TestExec.Switching.RelayForms;

namespace ABT.TestSpace.TestExec.Switching.USB_ERB24 {
    public enum UE { B0 = 0, B1 = 1 } // USB-ERB24 Boards.
    public enum R { C01 = 00, C02 = 01, C03 = 02, C04 = 03, C05 = 04, C06 = 05, C07 = 06, C08 = 07,
                    C09 = 08, C10 = 09, C11 = 10, C12 = 11, C13 = 12, C14 = 13, C15 = 14, C16 = 15,
                    C17 = 16, C18 = 17, C19 = 18, C20 = 19, C21 = 20, C22 = 21, C23 = 22, C24 = 23 }
    // R enum represents USB-ERB24 Relays, all Form C, explicitly correlated from relay # to corresponding bit number; Relay C01 = bit 0... relay C24 = bit 23. 

    // NOTE:  UE enum is a static definition of TestExecutive's MCC USB-ERB24(s).
    // Potential dynamic definition methods for USB_ERB24s:
    //  - Read them from MCC InstaCal's cb.cfg file.
    //  - Dynamically discover them programmatically: https://www.mccdaq.com/pdfs/manuals/Mcculw_WebHelp/ULStart.htm.
    //  - Specify MCC USB-ERB24s in TestExecutive.GlobalConfigurationFile.
    // NOTE:  MCC's InstaCal USB-ERB24 indexing begins at 0, or  zero-indexed/zero-based.
    // - Guessing because USB device indexing is likely also zero-based.  Or else because InstaCal is written in C/C++, which are zero-based.
    // - Anyway, UE.B0's numerical value is 0, which is used when constructing a new MccBoard UE.B0 Object:
    // - Instantiation 'new MccBoard((Int32)UE.B0)' is equivalent to 'new MccBoard(0)'.
    // NOTE:  enum named R instead of RELAYS for concision; consider below:
    //  - Set(UE.B0, new Dictionary<R, C.S>() {{R.C01,C.S.NC}, {R.C02,C.S.NO}, ... {R.C24,C.S.NC} });
    //  - Set(UE.B0, new Dictionary<RELAYS, C.S>() {{RELAYS.C01,C.S.NC}, {RELAYS.C02,C.S.NO}, ... {RELAYS.C24,C.S.NC} });
    // NOTE:  R's items named C## because USB-ERB24's relays are all Form C.
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

    public static class UE24 {
        public static readonly Dictionary<UE, MccBoard> USB_ERB24s = GetUSB_ERB24s();
        internal static readonly Dictionary<PORTS, BitVector32.Section> PortSections = GetPortSections();
        internal static readonly Int32[] _ue24bitVector32Masks = GetUE24BitVector32Masks();
        internal enum PORTS { A, B, CL, CH }
        private static Int32[] GetUE24BitVector32Masks() {
            Int32 ue24RelayCount = Enum.GetValues(typeof(R)).Length;
            Debug.Assert(ue24RelayCount == 24);
            Int32[] ue24BitVector32Masks = new Int32[ue24RelayCount];
            ue24BitVector32Masks[0] = BitVector32.CreateMask();
            for (Int32 i = 0; i < ue24RelayCount - 1; i++) ue24BitVector32Masks[i + 1] = BitVector32.CreateMask(ue24BitVector32Masks[i]);
            return ue24BitVector32Masks;
        }
        private static Dictionary<UE, MccBoard> GetUSB_ERB24s() {
            Dictionary<UE, MccBoard> USB_ERB24s = new Dictionary<UE, MccBoard>();
            foreach (UE ue in Enum.GetValues(typeof(UE))) USB_ERB24s.Add(ue, new MccBoard((Int32)ue));
            return USB_ERB24s;
        }
        private static Dictionary<PORTS, BitVector32.Section> GetPortSections() {
            Dictionary<PORTS, BitVector32.Section> PortSections = new Dictionary<PORTS, BitVector32.Section>();
            PortSections.Add(PORTS.A, BitVector32.CreateSection(0b_1111_1111));
            PortSections.Add(PORTS.B, BitVector32.CreateSection(0b_1111_1111, PortSections[PORTS.A]));
            PortSections.Add(PORTS.CL, BitVector32.CreateSection(0b_1111, PortSections[PORTS.B]));
            PortSections.Add(PORTS.CH, BitVector32.CreateSection(0b_1111, PortSections[PORTS.CL]));
            foreach (PORTS port in Enum.GetValues(typeof(PORTS))) Debug.WriteLine($"PortSections[PORTS.{Enum.GetName(typeof(PORTS), port)}]: '{PortSections[port]}'.");
            return PortSections;
        }

        public static void Initialize() { foreach (UE ue in USB_ERB24s.Keys) PortsWrite(USB_ERB24s[ue], USB_ERB24_Tests.ports0x00); }
        // NOTE:  Mustn't invoke TestExecutive.CT_EmergencyStop.ThrowIfCancellationRequested(); on Initialize().

        public static Boolean Initialized() {
            Boolean initialized = true;
            foreach (UE ue in USB_ERB24s.Keys) initialized &= USB_ERB24_Tests.ports0x00.SequenceEqual(PortsRead(USB_ERB24s[ue]));
            return initialized;
        }

        #region Is/Are
        public static Boolean Is(UE ue, R r, C.S s) { return Get(ue, r) == s; }

        public static Boolean Are(UE ue, HashSet<R> rs, C.S s) {
            Dictionary<R, C.S> rεs = rs.ToDictionary(r => r, r => s);
            Dictionary<R, C.S> are = Get(ue, rs);
            return rεs.Count == are.Count && !rεs.Except(are).Any();
        }

        public static Boolean Are(UE ue, Dictionary<R, C.S> RεS) {
            Dictionary<R, C.S> actual = Get(ue, new HashSet<R>(RεS.Keys));
            Boolean are = true;
            foreach (KeyValuePair<R, C.S> kvp in RεS) are &= kvp.Value == actual[kvp.Key];
            return are;
        }

        public static Boolean Are(UE ue, C.S s) {
            Boolean are = true;
            foreach (C.S S in Get(ue).Values) are &= S == s;
            return are;
        }
        #endregion Is/Are

        #region Get
        public static C.S Get(UE ue, R r) { return BitRead(USB_ERB24s[ue], (Int32)r) is DigitalLogicState.Low ? C.S.NC : C.S.NO; }

        public static Dictionary<R, C.S> Get(UE ue, HashSet<R> rs) { return rs.ToDictionary(r => r, r => Get(ue, r)); }

        public static Dictionary<R, C.S> Get(UE ue) {
            // Obviously, can utilize MccBoard.DBitIn to read individual bits, instead of MccBoard.DIn to read multiple bits:
            // - But, the USB-ERB24 reads it's relay's states by reading its internal 82C55's ports.
            // - Speculate the USB-ERB24's 822C55 operates in Mode 0, wherein PORTS.A & PORTS.B read all 8 bits collectively, not individually,
            //   while PORTS.CL & PORTS.CH can read their bits individually, though may also read them collectively.
            // - If correct, then utilizing MccBoard's DBitIn function could be very inefficient compared to
            //   the DIn function, since DBitIn would have to perform similar bit-shifting/bit-setting functions as this method does,
            //   once for each of the USB-ERB24's 24 relays, as opposed to 4 times for this method.
            // - Regardless, if preferred, below /*,*/commented code can replace the entirety of this method.
            /*
                public static Dictionary<R, C.S> Get(UE ue) { return new HashSet<R>(Enum.GetValues(typeof(R)).Cast<R>()).ToDictionary(r => r, r => Get(ue, r)); }
            */

            UInt16[] portBits = PortsRead(USB_ERB24s[ue]);
            UInt32[] biggerPortBits = Array.ConvertAll(portBits, delegate (UInt16 uint16) { return (UInt32)uint16; });
            UInt32 relayBits = 0x0000;
            relayBits |= biggerPortBits[(UInt32)PORTS.CH] << 00;
            relayBits |= biggerPortBits[(UInt32)PORTS.CL] << 04;
            relayBits |= biggerPortBits[(UInt32)PORTS.B] << 08;
            relayBits |= biggerPortBits[(UInt32)PORTS.A] << 16;
            BitVector32 bitVector32 = new BitVector32((Int32)relayBits);

            Dictionary<R, C.S> rεs = new Dictionary<R, C.S>();
            for (Int32 i = 0; i < _ue24bitVector32Masks.Length; i++) rεs.Add((R)i, bitVector32[_ue24bitVector32Masks[i]] ? C.S.NO : C.S.NC);
            return rεs;
        }

        public static HashSet<R> Get(UE ue, C.S s) {
            HashSet<R> rs = new HashSet<R>();
            foreach (R r in Enum.GetValues(typeof(R))) if (Get(ue, r) == s) rs.Add(r);
            return rs;
        }
        #endregion Get

        #region Set
        public static void Set(UE ue, R r, C.S s) {
            BitWrite(USB_ERB24s[ue], (Int32)r, s is C.S.NC ? DigitalLogicState.Low : DigitalLogicState.High);
            Debug.Assert(Is(ue, r, s));
        }

        public static void Set(UE ue, HashSet<R> rs, C.S s) {
            Set(ue, rs.ToDictionary(r => r, r => s));
            Debug.Assert(Are(ue, rs.ToDictionary(r => r, r => s)));
        }

        public static void Set(UE ue, Dictionary<R, C.S> RεS) {
            // This method only sets relay states for relays explicitly declared in RεS.
            //  - That is, if RεS = {{R.C01, C.S.NO}, {R.C02, C.S.NC}}, then only relays R.C01 & R.C02 will have their states actively set, respectively to NO & NC.
            //  - Relay states R.C03, R.C04...R.C24 remain as they were:
            //      - Relays that were NC remain NC.
            //      - Relays that were NO remain NO.
            //
            // Obviously, can utilize MccBoard.DBitOut to write individual bits, instead of MccBoard.DOut to write multiple bits:
            // - But, the USB-ERB24 energizes/de-energizes it's relays by writing its internal 82C55's ports.
            // - Speculate the USB-ERB24's 822C55 operates in Mode 0, wherein PORTS.A & PORTS.B write all 8 bits collectively, not individually,
            //   while PORTS.CL & PORTS.CH can write their bits individually, though may also write them collectively.
            // - If correct, then utilizing MccBoard's DBitOut function could be very inefficient compared to
            //   the DOut function, since it'd have to perform similar And/Or functions as this method does,
            //   once for every call to DBitOut.
            //  - Thought is that DOut will write the bits as simultaneously as possible, at least more so than DBitOut.
            // - Regardless, if preferred, below /*,*/commented code can replace the entirety of this method.
            /*
                public static void Set(UE ue, Dictionary<R, C.S> RεS) { foreach (KeyValuePair<R, C.S> kvp in RεS) Set(ue, kvp.Key, kvp.Value); }
            */

            UInt32 relayBit;
            UInt32 bits_NC = 0x00FF_FFFF; // bits_NC utilize Boolean And logic.  Note using 24 bits due to USB-ERB24.
            UInt32 bits_NO = 0x0000_0000; // bits_NO utilize Boolean Or logic.

            foreach (KeyValuePair<R, C.S> kvp in RεS) {
                relayBit = (UInt32)1 << (Int32)kvp.Key;
                if (kvp.Value == C.S.NC) bits_NC ^= relayBit;  // Sets a 0 in bits_NC for each explicitly assigned NC state in RεS.
                else bits_NO |= relayBit;                      // Sets a 1 in bits_NO for each explicitly assigned NO state in RεS.
            }
            Debug.WriteLine($"bits_NC: 0x{bits_NC:X8}");
            Debug.WriteLine($"bits_NO: 0x{bits_NO:X8}");

            BitVector32 bv32_NC = new BitVector32((Int32)bits_NC);
            BitVector32 bv32_NO = new BitVector32((Int32)bits_NO);
            Debug.WriteLine($"bv32_NC: {bv32_NC}");
            Debug.WriteLine($"bv32_NO: {bv32_NO}");

            UInt16[] portStates = PortsRead(USB_ERB24s[ue]);
            foreach (PORTS port in Enum.GetValues(typeof(PORTS))) {
                Debug.WriteLine($"portStates[{port}]: 0x{portStates[(Int32)port]:X4}");
                Debug.WriteLine($"bv32_NC[PortSections[{port}]: '{bv32_NC[PortSections[port]]}'.");
            }

            portStates[(Int32)PORTS.A] &= (UInt16)bv32_NC[PortSections[PORTS.A]]; // &= sets portStates bits low for each explicitly assigned NC state in RεS.
            portStates[(Int32)PORTS.B] &= (UInt16)bv32_NC[PortSections[PORTS.B]];
            portStates[(Int32)PORTS.CL] &= (UInt16)bv32_NC[PortSections[PORTS.CL]];
            portStates[(Int32)PORTS.CH] &= (UInt16)bv32_NC[PortSections[PORTS.CH]];

            portStates[(Int32)PORTS.A] |= (UInt16)bv32_NO[PortSections[PORTS.A]]; // |= sets portStates bits high for each explicitly assigned NO state in RεS.
            portStates[(Int32)PORTS.B] |= (UInt16)bv32_NO[PortSections[PORTS.B]];
            portStates[(Int32)PORTS.CL] |= (UInt16)bv32_NO[PortSections[PORTS.CL]];
            portStates[(Int32)PORTS.CH] |= (UInt16)bv32_NO[PortSections[PORTS.CH]];

            PortsWrite(USB_ERB24s[ue], portStates);
            Debug.Assert(PortsRead(USB_ERB24s[ue]).SequenceEqual(portStates));
        }

        public static void Set(UE ue, C.S s) {
            Dictionary<R, C.S> rεs = new HashSet<R>(Enum.GetValues(typeof(R)).Cast<R>()).ToDictionary(r => r, r => s);
            Set(ue, rεs);
            Debug.Assert(Are(ue, rεs));
        }
        #endregion Set

        #region internal methods
        /// <summary>
        /// BitRead() wraps MccBoard's DBitIn(DigitalPortType portType, int bitNum, DigitalLogicState bitValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        internal static DigitalLogicState BitRead(MccBoard mccBoard, Int32 bitNum) {
            ErrorInfo errorInfo = mccBoard.DBitIn(DigitalPortType.FirstPortA, bitNum, out DigitalLogicState bitValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
            return bitValue;
        }

        /// <summary>
        /// BitWrite() wraps MccBoard's DBitOut(DigitalPortType portType, int bitNum, DigitalLogicState bitValue) function.
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
        /// </summary>
        internal static void BitWrite(MccBoard mccBoard, Int32 bitNum, DigitalLogicState bitValue) {
            ErrorInfo errorInfo = mccBoard.DBitOut(DigitalPortType.FirstPortA, bitNum, bitValue);
            if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(mccBoard, errorInfo);
        }

        /// <summary>
        /// PortRead() wraps MccBoard's DIn(DigitalPortType portType, out UInt16 dataValue) function.
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
        /// PortWrite() wraps MccBoard's DOut(DigitalPortType portType, UInt16 dataValue) function,
        /// one of the four available MccBoard functions for the USB-ERB8 & USB-ERB24.
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
            StringBuilder sb = new StringBuilder(Environment.NewLine);
            sb.AppendLine($"MccBoard BoardNum   : {mccBoard.BoardNum}");
            sb.AppendLine($"MccBoard BoardName  : {mccBoard.BoardName}");
            sb.AppendLine($"MccBoard Descriptor : {mccBoard.Descriptor}");
            sb.AppendLine($"ErrorInfo Value     : {errorInfo.Value}");
            sb.AppendLine($"ErrorInfo Message   : {errorInfo.Message}");
            throw new InvalidOperationException(sb.ToString());
        }
        #endregion internal methods
    }
}