using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.Switching.USB_ERB24;
using static ABT.TestSpace.Switching.RelayForms;
using System.Linq;

namespace ABT.TestSpace.Switching {
    [TestClass()]
    public class USB_ERB24_Tests {
        // TODO: Add tests for class' USB_ERB24 3 public methods that aren't tested yet.
        private static readonly ushort[] ports0x00 = { 0x0000, 0x0000, 0x0000, 0x0000 };
        private static readonly ushort[] ports0xFF = { 0x00FF, 0x00FF, 0x000F, 0x000F };
        private static readonly ushort[] ports0xAA = { 0x00AA, 0x00AA, 0x000A, 0x000A };
        private static readonly ushort[] ports0x55 = { 0x0055, 0x0055, 0x0005, 0x0005 };

        private static IEnumerable<object[]> GetRs { get { return new[] { new object[] { GetRHashSet() } }; } }

        private static IEnumerable<object[]> GetRεCs { get { return new[] { new object[] { GetDictionaryRεC_NC(), GetDictionaryRεC_NO() } }; } }

        private static IEnumerable<object[]> GetUE24s { get { return new[] { new object[] { GetUE24HashSet() } }; } }

        private static IEnumerable<object[]> GetUE24s_Rs { get { return new[] { new object[] { GetUE24HashSet(), GetRHashSet() } }; } }

        private static void ConfirmRs(UE24 UE24, C C) {
            DialogResult dr = MessageBox.Show(
                $"Confirm All USB-ERB24 UE24 '{UE24}' R are in state '{C}'.{Environment.NewLine}{Environment.NewLine}" +
                $"Click Yes if confirmed, No if any R aren't continuous from terminals 'C' to '{C}'.", "Verify all USB-ERB24 R", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.No) Assert.Fail();
            if (dr == DialogResult.Cancel) Assert.Inconclusive();
        }

        private static Dictionary<R, C> GetDictionaryRεC_NC() { return GetRHashSet().ToDictionary(r => r, r => C.NC); }

        private static Dictionary<R, C> GetDictionaryRεC_NO() { return GetRHashSet().ToDictionary(r => r, r => C.NO); }

        private static Dictionary<R, C> GetDictionaryRεC_AA() {
            return new Dictionary<R, C>() {
                {R.C01, C.NC}, {R.C02, C.NO}, {R.C03, C.NC}, {R.C04, C.NO}, {R.C05, C.NC}, {R.C06, C.NO}, {R.C07, C.NC}, {R.C08, C.NO},
                {R.C09, C.NC}, {R.C10, C.NO}, {R.C11, C.NC}, {R.C12, C.NO}, {R.C13, C.NC}, {R.C14, C.NO}, {R.C15, C.NC}, {R.C16, C.NO},
                {R.C17, C.NC}, {R.C18, C.NO}, {R.C19, C.NC}, {R.C20, C.NO}, {R.C21, C.NC}, {R.C22, C.NO}, {R.C23, C.NC}, {R.C24, C.NO},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_55() {
            return new Dictionary<R, C>() {
                {R.C01, C.NO}, {R.C02, C.NC}, {R.C03, C.NO}, {R.C04, C.NC}, {R.C05, C.NO}, {R.C06, C.NC}, {R.C07, C.NO}, {R.C08, C.NC},
                {R.C09, C.NO}, {R.C10, C.NC}, {R.C11, C.NO}, {R.C12, C.NC}, {R.C13, C.NO}, {R.C14, C.NC}, {R.C15, C.NO}, {R.C16, C.NC},
                {R.C17, C.NO}, {R.C18, C.NC}, {R.C19, C.NO}, {R.C20, C.NC}, {R.C21, C.NO}, {R.C22, C.NC}, {R.C23, C.NO}, {R.C24, C.NC},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_Ax() {
            return new Dictionary<R, C>() {
                {R.C02, C.NO}, {R.C04, C.NO}, {R.C06, C.NO}, {R.C08, C.NO},
                {R.C10, C.NO}, {R.C12, C.NO}, {R.C14, C.NO}, {R.C16, C.NO},
                {R.C18, C.NO}, {R.C20, C.NO}, {R.C22, C.NO}, {R.C24, C.NO},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_5x() {
            return new Dictionary<R, C>() {
                {R.C01, C.NO}, {R.C03, C.NO}, {R.C05, C.NO}, {R.C07, C.NO},
                {R.C09, C.NO}, {R.C11, C.NO}, {R.C13, C.NO}, {R.C15, C.NO},
                {R.C17, C.NO}, {R.C19, C.NO}, {R.C21, C.NO}, {R.C23, C.NO},
            };
        }

        private static HashSet<R> GetRHashSet() {
            HashSet<R> Rs = new HashSet<R>();
            foreach (R R in Enum.GetValues(typeof(R))) Rs.Add(R);
            return Rs;
        }

        private static MccBoard GetMccBoard(UE24 UE24) { return new MccBoard((int)UE24); }

        private static HashSet<UE24> GetUE24HashSet() {
            HashSet<UE24> UE24s = new HashSet<UE24>();
            foreach (UE24 UE24 in Enum.GetValues(typeof(UE24))) UE24s.Add(UE24);
            return UE24s;
        }

        private static void ProcessError(ErrorInfo errorInfo) { if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) throw new InvalidOperationException(); }

        private static bool ReadWritPort(MccBoard mccBoard, ErrorInfo errorInfo, DigitalPortType digitalPortType, ushort writ) {
            PortWrite(mccBoard, digitalPortType, writ);
            errorInfo = mccBoard.DIn(digitalPortType, out ushort read);
            ProcessError(errorInfo);
            Console.WriteLine($"writ: {writ,-3}; read: {read,-3}");
            return writ == read;
        }

        private static bool ReadWritPorts(MccBoard mccBoard, ushort[] writPorts) {
            ErrorInfo errorInfo = new ErrorInfo();
            bool allPassed = true;
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, writPorts[(int)PORTS.A]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, writPorts[(int)PORTS.B]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, writPorts[(int)PORTS.CL]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, writPorts[(int)PORTS.CH]);
            ushort[] readPorts = PortsRead(mccBoard);
            for (int i = 0; i < readPorts.Length; i++) {
                allPassed &= writPorts[i] == readPorts[i];
                Console.WriteLine($"Read Ports[{i}] '{readPorts[i]}', Writ Ports[{i}] '{writPorts[i]}.'");
            }
            return allPassed;
        }

        private static bool WriteReadPort(MccBoard mccBoard, ErrorInfo errorInfo, DigitalPortType digitalPortType, ushort writ) {
            errorInfo = mccBoard.DOut(digitalPortType, writ);
            ProcessError(errorInfo);
            uint read = PortRead(mccBoard, digitalPortType);
            Console.WriteLine($"writ: {writ,-3}; read: {read,-3}");
            return writ == read;
        }

        private static bool WriteReadPorts(MccBoard mccBoard, ushort[] writPorts) {
            ErrorInfo errorInfo = new ErrorInfo();
            bool allPassed = true;
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, writPorts[(int)PORTS.A]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, writPorts[(int)PORTS.B]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, writPorts[(int)PORTS.CL]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, writPorts[(int)PORTS.CH]);
            ushort[] readPorts = PortsRead(mccBoard);
            for (int i = 0; i < readPorts.Length; i++) {
                allPassed &= writPorts[i] == readPorts[i];
                Console.WriteLine($"Read Ports[{i}] '{readPorts[i]}', Writ Ports[{i}] '{writPorts[i]}.'");
            }
            return allPassed;
        }
        #region Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void UE24s_Test(HashSet<UE24> UE24s) { for (int i = 0; i < UE24s.Count; i++) Assert.IsTrue(UE24s.Contains((UE24)i)); }

        [TestMethod()]
        public void PORTS_Test() {
            Assert.AreEqual((int)PORTS.A, 0);
            Assert.AreEqual((int)PORTS.B, 1);
            Assert.AreEqual((int)PORTS.CL, 2);
            Assert.AreEqual((int)PORTS.CH, 3);
            Assert.AreEqual(Enum.GetNames(typeof(PORTS)).Length, 4);
        }

        [TestMethod()]
        public void Rs_Test() {
            Assert.AreEqual((int)R.C01, 00);
            Assert.AreEqual((int)R.C02, 01);
            Assert.AreEqual((int)R.C03, 02);
            Assert.AreEqual((int)R.C04, 03);
            Assert.AreEqual((int)R.C05, 04);
            Assert.AreEqual((int)R.C06, 05);
            Assert.AreEqual((int)R.C07, 06);
            Assert.AreEqual((int)R.C08, 07);
            Assert.AreEqual((int)R.C09, 08);
            Assert.AreEqual((int)R.C10, 09);
            Assert.AreEqual((int)R.C11, 10);
            Assert.AreEqual((int)R.C12, 11);
            Assert.AreEqual((int)R.C13, 12);
            Assert.AreEqual((int)R.C14, 13);
            Assert.AreEqual((int)R.C15, 14);
            Assert.AreEqual((int)R.C16, 15);
            Assert.AreEqual((int)R.C17, 16);
            Assert.AreEqual((int)R.C18, 17);
            Assert.AreEqual((int)R.C19, 18);
            Assert.AreEqual((int)R.C20, 19);
            Assert.AreEqual((int)R.C21, 20);
            Assert.AreEqual((int)R.C22, 21);
            Assert.AreEqual((int)R.C23, 22);
            Assert.AreEqual((int)R.C24, 23);
            Assert.AreEqual(Enum.GetNames(typeof(R)).Length, 24);
        }
        #endregion Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.

        #region public method tests
        [TestMethod()]
        public void Is_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            foreach (R R in Enum.GetValues(typeof(R))) Assert.IsTrue(Is(UE24.E01, R, C.NC));
            PortsWrite(mccBoard, ports0xFF);
            foreach (R R in Enum.GetValues(typeof(R))) Assert.IsTrue(Is(UE24.E01, R, C.NO));
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void AreRs_Test(HashSet<R> Rs) {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Assert.IsTrue(Are(UE24.E01, Rs, C.NC));
            PortsWrite(mccBoard, ports0xFF);
            Assert.IsTrue(Are(UE24.E01, Rs, C.NO));
        }

        [TestMethod()]
        [DynamicData(nameof(GetRεCs))]
        public void AreRεC_Test(Dictionary<R, C> RεC_NC, Dictionary<R, C> RεC_NO) {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Assert.IsTrue(Are(UE24.E01, RεC_NC));
            PortsWrite(mccBoard, ports0xFF);
            Assert.IsTrue(Are(UE24.E01, RεC_NO));
        }

        [TestMethod()]
        public void AreUE24_C_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Assert.IsTrue(Are(UE24.E01, C.NC));
            ConfirmRs(UE24.E01, C.NC);
            PortsWrite(mccBoard, ports0xFF);
            Assert.IsTrue(Are(UE24.E01, C.NO));
            ConfirmRs(UE24.E01, C.NO);
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreUE24s_C_Test(HashSet<UE24> UE24s) {
            foreach (UE24 UE24 in UE24s) {
                PortsWrite(GetMccBoard(UE24), ports0x00);
                Assert.IsTrue(Are(UE24s, C.NC));
                PortsWrite(GetMccBoard(UE24), ports0xFF);
                Assert.IsTrue(Are(UE24s, C.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s_Rs))]
        public void AreUE24s_Rs_C_Test(HashSet<UE24> UE24s, HashSet<R> Rs) {
            foreach (UE24 UE24 in UE24s) {
                PortsWrite(GetMccBoard(UE24), ports0x00);
                Assert.IsTrue(Are(UE24s, Rs, C.NC));
                PortsWrite(GetMccBoard(UE24), ports0xFF);
                Assert.IsTrue(Are(UE24s, Rs, C.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreUE24s_RεC_Test(HashSet<UE24> UE24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = UE24s.ToDictionary(ue24 => ue24, ue24 => GetDictionaryRεC_NC());

            foreach (UE24 UE24 in UE24s) {
                PortsWrite(GetMccBoard(UE24), ports0x00);
                Assert.IsTrue(Are(UE24εRεC));
                PortsWrite(GetMccBoard(UE24), ports0xFF);
                UE24εRεC = UE24s.ToDictionary(ue24 => ue24, ue24 => GetDictionaryRεC_NO());
                Assert.IsTrue(Are(UE24εRεC));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void Are_Test(HashSet<UE24> UE24s) {
            foreach (UE24 UE24 in UE24s) {
                PortsWrite(GetMccBoard(UE24), ports0x00);
                bool arelow = true;
                arelow &= Are(UE24, C.NC);
                Assert.IsTrue(arelow);
            }
            foreach (UE24 UE24 in UE24s) {
                PortsWrite(GetMccBoard(UE24), ports0xFF);
                bool areHIGH = true;
                areHIGH &= Are(UE24, C.NO);
                Assert.IsTrue(areHIGH);
            }
        }

        [TestMethod()]
        public void GetR_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            HashSet<R> Rs = GetRHashSet();
            foreach (R R in Rs) Assert.AreEqual(Get(UE24.E01, R), C.NC);
            PortsWrite(mccBoard, ports0xFF);
            foreach (R R in Rs) Assert.AreEqual(Get(UE24.E01, R), C.NO);
        }

        [TestMethod()]
        public void Get_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Dictionary<R, C> RεC = Get(UE24.E01);
            foreach (KeyValuePair<R, C> kvp in RεC) Assert.AreEqual(kvp.Value, C.NC);
            PortsWrite(mccBoard, ports0xFF);
            RεC = Get(UE24.E01);
            foreach (KeyValuePair<R, C> kvp in RεC) Assert.AreEqual(kvp.Value, C.NO);
        }

        [TestMethod()]
        public void GetRεC_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC_Test = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 UE24 in GetUE24HashSet()) UE24εRεC_Test.Add(UE24, GetDictionaryRεC_NC());
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = Get();
            Assert.AreEqual(UE24εRεC_Test.Count, UE24εRεC.Count);
            foreach (UE24 UE24 in GetUE24HashSet()) {
                Assert.AreEqual(UE24εRεC_Test[UE24].Count, UE24εRεC[UE24].Count);
                Assert.IsTrue(!UE24εRεC_Test[UE24].Except(UE24εRεC[UE24]).Any());
            }
        }

        [TestMethod()]
        public void GetRs_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            PortsWrite(mccBoard, ports0x00);
            Dictionary<R, C> RεC_Test = GetDictionaryRεC_NC();
            Dictionary<R, C> RεC = Get(UE24.E01, GetRHashSet());
            Assert.AreEqual(RεC_Test.Count, RεC.Count);
            Assert.IsTrue(!RεC.Except(RεC_Test).Any());

            PortsWrite(mccBoard, ports0xFF);
            RεC_Test = GetDictionaryRεC_NO();
            RεC = Get(UE24.E01, GetRHashSet());
            Assert.AreEqual(RεC_Test.Count, RεC.Count);
            Assert.IsTrue(!RεC.Except(RεC_Test).Any());
        }

        [TestMethod()]
        public void SetUE24_C_Test() {
            for (int i = 0; i < 4; i++) {
                Set(UE24.E01, C.NO);
                Set(UE24.E01, C.NC);
            }
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, C.NO);
            Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
        }

        [TestMethod()]
        public void SetUE24_RεC_Test() {
            Set(UE24.E01, GetDictionaryRεC_NC());
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, GetDictionaryRεC_Ax());
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, GetDictionaryRεC_NO());
            Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, GetDictionaryRεC_AA());
            Assert.IsTrue(ports0xAA.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, GetDictionaryRεC_55());
            Assert.IsTrue(ports0x55.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24_R_C_Test(HashSet<R> Rs) {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            ErrorInfo errorInfo;
            DigitalLogicState digitalLogicState;
            foreach (R R in Rs) {
                Set(UE24.E01, R, C.NC);
                errorInfo = mccBoard.DBitIn(DigitalPortType.FirstPortA, (int)R, out digitalLogicState);
                ProcessErrorInfo(mccBoard, errorInfo);
                Assert.AreEqual(digitalLogicState, DigitalLogicState.Low);

                Set(UE24.E01, R, C.NO);
                errorInfo = mccBoard.DBitIn(DigitalPortType.FirstPortA, (int)R, out digitalLogicState);
                ProcessErrorInfo(mccBoard, errorInfo);
                Assert.AreEqual(digitalLogicState, DigitalLogicState.High);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24_Rs_C_Test(HashSet<R> Rs) {
            Set(UE24.E01, Rs, C.NC);
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
            Set(UE24.E01, Rs, C.NO);
            Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24s_C_Test(HashSet<UE24> UE24s) {
            Set(UE24s, C.NC);
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
            Set(UE24s, C.NO);
            Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24s_Rs_C_Test(HashSet<R> Rs) {
            HashSet<UE24> UE24s = GetUE24HashSet();
            Set(UE24s, Rs, C.NC);
            foreach (UE24 UE24 in UE24s) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24))));
            Set(UE24s, Rs, C.NO);
            foreach (UE24 UE24 in UE24s) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24))));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24εRεC_Test(HashSet<UE24> UE24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 UE24 in UE24s) UE24εRεC.Add(UE24, GetDictionaryRεC_NC());
            Set(UE24εRεC);
            foreach (UE24 UE24 in UE24s) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24))));
            UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 UE24 in UE24s) UE24εRεC.Add(UE24, GetDictionaryRεC_NO());
            Set(UE24εRεC);
            foreach (UE24 UE24 in UE24s) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24))));
        }

        [TestMethod()]
        public void SetC_Test() {
            Set(UE24.E01, C.NC);
            Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));

            Set(UE24.E01, C.NO);
            Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(GetMccBoard(UE24.E01))));
        }
        #endregion public method tests

        #region private method tests
        [TestMethod()]
        public void PortRead_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            ErrorInfo errorInfo = new ErrorInfo();
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, ports0x00[(int)PORTS.A]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, ports0x00[(int)PORTS.B]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, ports0x00[(int)PORTS.CL]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, ports0x00[(int)PORTS.CH]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, ports0xFF[(int)PORTS.A]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, ports0xFF[(int)PORTS.B]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(int)PORTS.CL]));
            Assert.IsTrue(WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(int)PORTS.CH]));
        }

        [TestMethod()]
        public void PortsRead_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            Assert.IsTrue(WriteReadPorts(mccBoard, ports0x00));
            Assert.IsTrue(WriteReadPorts(mccBoard, ports0xFF));
        }

        [TestMethod()]
        public void PortWrite_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            ErrorInfo errorInfo = new ErrorInfo();
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, ports0x00[(int)PORTS.A]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, ports0x00[(int)PORTS.B]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, ports0x00[(int)PORTS.CL]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, ports0x00[(int)PORTS.CH]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, ports0xFF[(int)PORTS.A]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, ports0xFF[(int)PORTS.B]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(int)PORTS.CL]));
            Assert.IsTrue(ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(int)PORTS.CH]));
        }

        [TestMethod()]
        public void PortsWrite_Test() {
            MccBoard mccBoard = GetMccBoard(UE24.E01);
            Assert.IsTrue(ReadWritPorts(mccBoard, ports0x00));
            Assert.IsTrue(ReadWritPorts(mccBoard, ports0xFF));
        }

        [TestMethod()]
        public void GetPort_Test() {
            DigitalPortType dtp;
            for (int bitNum = 0; bitNum < Enum.GetNames(typeof(R)).Length; bitNum++) {
                dtp = GetPort((R)bitNum);
                switch (bitNum) {
                    case int b when 0 <= b && b <= 7:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortA);
                        break;
                    case int b when 8 <= b && b <= 15:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortB);
                        break;
                    case int b when 16 <= b && b <= 19:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortCL);
                        break;
                    case int b when 20 <= b && b <= 23:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortCH);
                        break;
                }
            }
        }
        #endregion private method tests
    }
}