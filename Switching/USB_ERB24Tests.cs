using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.Switching.USB_ERB24;
using static ABT.TestSpace.Switching.RelayForms;

namespace ABT.TestSpace.Switching {
    [TestClass()]
    public class USB_ERB24_Tests {
        // TODO: Add tests for class' USB_ERB24 3 public methods that aren't tested yet.
        private static readonly UInt16[] ports0x00 = { 0x0000, 0x0000, 0x0000, 0x0000 };
        private static readonly UInt16[] ports0xFF = { 0x00FF, 0x00FF, 0x000F, 0x000F };
        private static readonly UInt16[] ports0xAA = { 0x00AA, 0x00AA, 0x000A, 0x000A };
        private static readonly UInt16[] ports0x55 = { 0x0055, 0x0055, 0x0005, 0x0005 };

        private static IEnumerable<Object[]> GetRs { get { return new[] { new Object[] { GetRHashSet() } }; } }

        private static IEnumerable<Object[]> GetRεCs { get { return new[] { new Object[] { GetDictionaryRεC_NC(), GetDictionaryRεC_NO() } }; } }

        private static IEnumerable<Object[]> GetUE24s { get { return new[] { new Object[] { GetUE24HashSet() } }; } }

        private static IEnumerable<Object[]> GetUE24s_Rs { get { return new[] { new Object[] { GetUE24HashSet(), GetRHashSet() } }; } }

        private static void ConfirmRs(UE24 ue24, C c) {
            DialogResult dr = MessageBox.Show(
                $"Confirm All USB-ERB24 UE24 '{ue24}' R are in state '{c}'.{Environment.NewLine}{Environment.NewLine}" +
                $"Click Yes if confirmed, No if any R aren't continuous from terminals 'C' to '{c}'.", "Verify all USB-ERB24 R", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.No) Assert.Fail();
            if (dr == DialogResult.Cancel) Assert.Inconclusive();
        }

        private static Dictionary<R, C> GetDictionaryRεC_NC() { return GetRHashSet().ToDictionary(r => r, r => C.NC); }

        private static Dictionary<R, C> GetDictionaryRεC_NO() { return GetRHashSet().ToDictionary(r => r, r => C.NO); }

        private static Dictionary<R, C> GetDictionaryRεC_NC_NO() {
            return new Dictionary<R, C>() {
                {R.C01, C.NC}, {R.C02, C.NO}, {R.C03, C.NC}, {R.C04, C.NO}, {R.C05, C.NC}, {R.C06, C.NO}, {R.C07, C.NC}, {R.C08, C.NO},
                {R.C09, C.NC}, {R.C10, C.NO}, {R.C11, C.NC}, {R.C12, C.NO}, {R.C13, C.NC}, {R.C14, C.NO}, {R.C15, C.NC}, {R.C16, C.NO},
                {R.C17, C.NC}, {R.C18, C.NO}, {R.C19, C.NC}, {R.C20, C.NO}, {R.C21, C.NC}, {R.C22, C.NO}, {R.C23, C.NC}, {R.C24, C.NO},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_NO_NC() {
            return new Dictionary<R, C>() {
                {R.C01, C.NO}, {R.C02, C.NC}, {R.C03, C.NO}, {R.C04, C.NC}, {R.C05, C.NO}, {R.C06, C.NC}, {R.C07, C.NO}, {R.C08, C.NC},
                {R.C09, C.NO}, {R.C10, C.NC}, {R.C11, C.NO}, {R.C12, C.NC}, {R.C13, C.NO}, {R.C14, C.NC}, {R.C15, C.NO}, {R.C16, C.NC},
                {R.C17, C.NO}, {R.C18, C.NC}, {R.C19, C.NO}, {R.C20, C.NC}, {R.C21, C.NO}, {R.C22, C.NC}, {R.C23, C.NO}, {R.C24, C.NC},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_X_NO() {
            return new Dictionary<R, C>() {
                {R.C02, C.NO}, {R.C04, C.NO}, {R.C06, C.NO}, {R.C08, C.NO},
                {R.C10, C.NO}, {R.C12, C.NO}, {R.C14, C.NO}, {R.C16, C.NO},
                {R.C18, C.NO}, {R.C20, C.NO}, {R.C22, C.NO}, {R.C24, C.NO},
            };
        }

        private static Dictionary<R, C> GetDictionaryRεC_NO_X() {
            return new Dictionary<R, C>() {
                {R.C01, C.NO}, {R.C03, C.NO}, {R.C05, C.NO}, {R.C07, C.NO},
                {R.C09, C.NO}, {R.C11, C.NO}, {R.C13, C.NO}, {R.C15, C.NO},
                {R.C17, C.NO}, {R.C19, C.NO}, {R.C21, C.NO}, {R.C23, C.NO},
            };
        }

        private static HashSet<R> GetRHashSet() {
            HashSet<R> rs = new HashSet<R>();
            foreach (R r in Enum.GetValues(typeof(R))) rs.Add(r);
            return rs;
        }

        private static HashSet<UE24> GetUE24HashSet() {
            HashSet<UE24> ue24s = new HashSet<UE24>();
            foreach (UE24 ue24 in Enum.GetValues(typeof(UE24))) ue24s.Add(ue24);
            return ue24s;
        }

        private static void ProcessError(ErrorInfo errorInfo) { if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) throw new InvalidOperationException(); }

        private static Boolean ReadWritPort(MccBoard mccBoard, ErrorInfo errorInfo, DigitalPortType digitalPortType, UInt16 writ) {
            PortWrite(mccBoard, digitalPortType, writ);
            errorInfo = mccBoard.DIn(digitalPortType, out UInt16 read);
            ProcessError(errorInfo);
            Console.WriteLine($"writ: {writ,-3}; read: {read,-3}");
            return writ == read;
        }

        private static Boolean ReadWritPorts(MccBoard mccBoard, UInt16[] writPorts) {
            ErrorInfo errorInfo = new ErrorInfo();
            Boolean allPassed = true;
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, writPorts[(Int32)PORTS.A]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, writPorts[(Int32)PORTS.B]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, writPorts[(Int32)PORTS.CL]);
            allPassed &= ReadWritPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, writPorts[(Int32)PORTS.CH]);
            UInt16[] readPorts = PortsRead(mccBoard);
            for (Int32 i = 0; i < readPorts.Length; i++) {
                allPassed &= writPorts[i] == readPorts[i];
                Console.WriteLine($"Read Ports[{i}] '{readPorts[i]}', Writ Ports[{i}] '{writPorts[i]}.'");
            }
            return allPassed;
        }

        private static Boolean WriteReadPort(MccBoard mccBoard, ErrorInfo errorInfo, DigitalPortType digitalPortType, UInt16 writ) {
            errorInfo = mccBoard.DOut(digitalPortType, writ);
            ProcessError(errorInfo);
            UInt32 read = PortRead(mccBoard, digitalPortType);
            Console.WriteLine($"writ: {writ,-3}; read: {read,-3}");
            return writ == read;
        }

        private static Boolean WriteReadPorts(MccBoard mccBoard, UInt16[] writPorts) {
            ErrorInfo errorInfo = new ErrorInfo();
            Boolean allPassed = true;
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortA, writPorts[(Int32)PORTS.A]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortB, writPorts[(Int32)PORTS.B]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCL, writPorts[(Int32)PORTS.CL]);
            allPassed &= WriteReadPort(mccBoard, errorInfo, DigitalPortType.FirstPortCH, writPorts[(Int32)PORTS.CH]);
            UInt16[] readPorts = PortsRead(mccBoard);
            for (Int32 i = 0; i < readPorts.Length; i++) {
                allPassed &= writPorts[i] == readPorts[i];
                Console.WriteLine($"Read Ports[{i}] '{readPorts[i]}', Writ Ports[{i}] '{writPorts[i]}.'");
            }
            return allPassed;
        }
        #region Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void UE24s_Test(HashSet<UE24> ue24s) { for (Int32 i = 0; i < ue24s.Count; i++) Assert.IsTrue(ue24s.Contains((UE24)i));  }

        [TestMethod()]
        public void PORTS_Test() {
            Assert.AreEqual((Int32)PORTS.A, 0);
            Assert.AreEqual((Int32)PORTS.B, 1);
            Assert.AreEqual((Int32)PORTS.CL, 2);
            Assert.AreEqual((Int32)PORTS.CH, 3);
            Assert.AreEqual(Enum.GetNames(typeof(PORTS)).Length, 4);
        }

        [TestMethod()]
        public void Rs_Test() {
            Assert.AreEqual((Int32)R.C01, 00);
            Assert.AreEqual((Int32)R.C02, 01);
            Assert.AreEqual((Int32)R.C03, 02);
            Assert.AreEqual((Int32)R.C04, 03);
            Assert.AreEqual((Int32)R.C05, 04);
            Assert.AreEqual((Int32)R.C06, 05);
            Assert.AreEqual((Int32)R.C07, 06);
            Assert.AreEqual((Int32)R.C08, 07);
            Assert.AreEqual((Int32)R.C09, 08);
            Assert.AreEqual((Int32)R.C10, 09);
            Assert.AreEqual((Int32)R.C11, 10);
            Assert.AreEqual((Int32)R.C12, 11);
            Assert.AreEqual((Int32)R.C13, 12);
            Assert.AreEqual((Int32)R.C14, 13);
            Assert.AreEqual((Int32)R.C15, 14);
            Assert.AreEqual((Int32)R.C16, 15);
            Assert.AreEqual((Int32)R.C17, 16);
            Assert.AreEqual((Int32)R.C18, 17);
            Assert.AreEqual((Int32)R.C19, 18);
            Assert.AreEqual((Int32)R.C20, 19);
            Assert.AreEqual((Int32)R.C21, 20);
            Assert.AreEqual((Int32)R.C22, 21);
            Assert.AreEqual((Int32)R.C23, 22);
            Assert.AreEqual((Int32)R.C24, 23);
            Assert.AreEqual(Enum.GetNames(typeof(R)).Length, 24);
        }
        #endregion Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.

        #region public method tests
        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void Is_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                foreach (R r in GetRHashSet()) Assert.IsTrue(Is(ue24, r, C.NC));
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                foreach (R r in GetRHashSet()) Assert.IsTrue(Is(ue24, r, C.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreRs_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                Assert.IsTrue(Are(ue24, GetRHashSet(), C.NC));
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                Assert.IsTrue(Are(ue24, GetRHashSet(), C.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetRεCs))]
        public void AreRεC_Test(Dictionary<R, C> RεC_NC, Dictionary<R, C> RεC_NO) {
            foreach (UE24 ue24 in GetUE24HashSet()) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                Assert.IsTrue(Are(ue24, RεC_NC));
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                Assert.IsTrue(Are(ue24, RεC_NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreUE24_C_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                Assert.IsTrue(Are(ue24, C.NC));
                ConfirmRs(ue24, C.NC);
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                Assert.IsTrue(Are(ue24, C.NO));
                ConfirmRs(ue24, C.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreUE24s_C_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0x00);
            Assert.IsTrue(Are(ue24s, C.NC));
            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0xFF);
            Assert.IsTrue(Are(ue24s, C.NO));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s_Rs))]
        public void AreUE24s_Rs_C_Test(HashSet<UE24> ue24s, HashSet<R> rs) {
            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0x00);
            Assert.IsTrue(Are(ue24s, rs, C.NC));
            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0xFF);
            Assert.IsTrue(Are(ue24s, rs, C.NO));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void AreUE24s_RεC_Test(HashSet<UE24> ue24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = ue24s.ToDictionary(ue24 => ue24, ue24 => GetDictionaryRεC_NC());

            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0x00);
            Assert.IsTrue(Are(UE24εRεC));
            foreach (UE24 ue24 in ue24s) PortsWrite(Only.UE24s[ue24], ports0xFF);
            UE24εRεC = ue24s.ToDictionary(ue => ue, ue => GetDictionaryRεC_NO());
            Assert.IsTrue(Are(UE24εRεC));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void Are_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                Boolean arelow = true;
                arelow &= Are(ue24, C.NC);
                Assert.IsTrue(arelow);
            }
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                Boolean areHIGH = true;
                areHIGH &= Are(ue24, C.NO);
                Assert.IsTrue(areHIGH);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void GetR_Test(HashSet<UE24> ue24s) {
            HashSet<R> rs = GetRHashSet();
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                foreach (R r in rs) Assert.AreEqual(Get(ue24, r), C.NC);
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                foreach (R r in rs) Assert.AreEqual(Get(ue24, r), C.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void Get_Test(HashSet<UE24> ue24s) {
            Dictionary<R, C> RεC = new Dictionary<R, C>();
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                RεC = Get(ue24);
                foreach (KeyValuePair<R, C> kvp in RεC) Assert.AreEqual(kvp.Value, C.NC);
                PortsWrite(Only.UE24s[ue24], ports0xFF);
                RεC = Get(ue24);
                foreach (KeyValuePair<R, C> kvp in RεC) Assert.AreEqual(kvp.Value, C.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void GetRεC_Test(HashSet<UE24> ue24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC_Test = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 ue in ue24s) PortsWrite(Only.UE24s[ue], ports0x00);
            foreach (UE24 ue in ue24s) UE24εRεC_Test.Add(ue, GetDictionaryRεC_NC());
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = Get();
            Assert.AreEqual(UE24εRεC_Test.Count, UE24εRεC.Count);
            foreach (UE24 ue in GetUE24HashSet()) {
                Assert.AreEqual(UE24εRεC_Test[ue].Count, UE24εRεC[ue].Count);
                Assert.IsTrue(!UE24εRεC_Test[ue].Except(UE24εRεC[ue]).Any());
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void GetRs_Test(HashSet<UE24> ue24s) {
            Dictionary<R, C> RεC_Test;
            Dictionary<R, C> RεC;
            foreach (UE24 ue24 in ue24s) {
                PortsWrite(Only.UE24s[ue24], ports0x00);
                RεC_Test = GetDictionaryRεC_NC();
                RεC = Get(ue24, GetRHashSet());
                Assert.AreEqual(RεC_Test.Count, RεC.Count);
                Assert.IsTrue(!RεC.Except(RεC_Test).Any());

                PortsWrite(Only.UE24s[ue24], ports0xFF);
                RεC_Test = GetDictionaryRεC_NO();
                RεC = Get(ue24, GetRHashSet());
                Assert.AreEqual(RεC_Test.Count, RεC.Count);
                Assert.IsTrue(!RεC.Except(RεC_Test).Any());
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24_C_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                for (Int32 i = 0; i < 4; i++) {
                    Set(ue24, C.NO);
                    Set(ue24, C.NC);
                }
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));
                Set(ue24, C.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24_RεC_Test(HashSet<UE24> ue24s) {
            UInt16[] ports;
            foreach (UE24 ue24 in ue24s) {
                Set(ue24, GetDictionaryRεC_NC());
                ports = PortsRead(Only.UE24s[ue24]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));

                Set(ue24, GetDictionaryRεC_X_NO());
                ports = PortsRead(Only.UE24s[ue24]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xAA.SequenceEqual(PortsRead(Only.UE24s[ue24])));

                Set(ue24, GetDictionaryRεC_NO_X());
                ports = PortsRead(Only.UE24s[ue24]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));

                Set(ue24, new Dictionary<R, C>() { { R.C01, C.NC }, { R.C08, C.NC }, { R.C09, C.NC }, { R.C16, C.NC }, { R.C17, C.NC }, { R.C20, C.NC }, { R.C21, C.NC }, { R.C24, C.NC } });
                ports = PortsRead(Only.UE24s[ue24]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.AreEqual(ports[(Int32)PORTS.A], 0x7E);
                Assert.AreEqual(ports[(Int32)PORTS.B], 0x7E);
                Assert.AreEqual(ports[(Int32)PORTS.CL], 0x6);
                Assert.AreEqual(ports[(Int32)PORTS.CH], 0x6);

                Set(ue24, new Dictionary<R, C>() { { R.C01, C.NO }, { R.C08, C.NO }, { R.C09, C.NO }, { R.C16, C.NO }, { R.C17, C.NO }, { R.C20, C.NO }, { R.C21, C.NO }, { R.C24, C.NO } });
                ports = PortsRead(Only.UE24s[ue24]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24_R_C_Test(HashSet<R> rs) {
            ErrorInfo errorInfo;
            DigitalLogicState digitalLogicState;
            foreach (UE24 ue24 in GetUE24HashSet()) {
                foreach (R r in rs) {
                    Set(ue24, r, C.NC);
                    errorInfo = Only.UE24s[ue24].DBitIn(DigitalPortType.FirstPortA, (Int32)r, out digitalLogicState);
                    ProcessErrorInfo(Only.UE24s[ue24], errorInfo);
                    Assert.AreEqual(digitalLogicState, DigitalLogicState.Low);

                    Set(ue24, r, C.NO);
                    errorInfo = Only.UE24s[ue24].DBitIn(DigitalPortType.FirstPortA, (Int32)r, out digitalLogicState);
                    ProcessErrorInfo(Only.UE24s[ue24], errorInfo);
                    Assert.AreEqual(digitalLogicState, DigitalLogicState.High);
                }
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24_Rs_C_Test(HashSet<R> rs) {
            foreach (UE24 ue24 in GetUE24HashSet()) {
                Set(ue24, rs, C.NC);
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));
                Set(ue24, rs, C.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24s_C_Test(HashSet<UE24> ue24s) {
            Set(ue24s, C.NC);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            Set(ue24s, C.NO);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetRs))]
        public void SetUE24s_Rs_C_Test(HashSet<R> rs) {
            HashSet<UE24> ue24s = GetUE24HashSet();
            Set(ue24s, rs, C.NC);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            Set(ue24s, rs, C.NO);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetUE24εRεC_Test(HashSet<UE24> ue24s) {
            Dictionary<UE24, Dictionary<R, C>> UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 ue24 in ue24s) UE24εRεC.Add(ue24, GetDictionaryRεC_NC());
            Set(UE24εRεC);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            UE24εRεC = new Dictionary<UE24, Dictionary<R, C>>();
            foreach (UE24 ue24 in ue24s) UE24εRεC.Add(ue24, GetDictionaryRεC_NO());
            Set(UE24εRεC);
            foreach (UE24 ue24 in ue24s) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void SetC_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                Set(ue24, C.NC);
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.UE24s[ue24])));

                Set(ue24, C.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.UE24s[ue24])));
            }
        }
        #endregion public method tests

        #region private method tests
        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void PortRead_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                ErrorInfo errorInfo = new ErrorInfo();
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortA, ports0x00[(Int32)PORTS.A]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortB, ports0x00[(Int32)PORTS.B]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCL, ports0x00[(Int32)PORTS.CL]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCH, ports0x00[(Int32)PORTS.CH]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortA, ports0xFF[(Int32)PORTS.A]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortB, ports0xFF[(Int32)PORTS.B]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(Int32)PORTS.CL]));
                Assert.IsTrue(WriteReadPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(Int32)PORTS.CH]));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void PortsRead_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                Assert.IsTrue(WriteReadPorts(Only.UE24s[ue24], ports0x00));
                Assert.IsTrue(WriteReadPorts(Only.UE24s[ue24], ports0xFF));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void PortWrite_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                ErrorInfo errorInfo = new ErrorInfo();
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortA, ports0x00[(Int32)PORTS.A]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortB, ports0x00[(Int32)PORTS.B]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCL, ports0x00[(Int32)PORTS.CL]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCH, ports0x00[(Int32)PORTS.CH]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortA, ports0xFF[(Int32)PORTS.A]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortB, ports0xFF[(Int32)PORTS.B]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(Int32)PORTS.CL]));
                Assert.IsTrue(ReadWritPort(Only.UE24s[ue24], errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(Int32)PORTS.CH]));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUE24s))]
        public void PortsWrite_Test(HashSet<UE24> ue24s) {
            foreach (UE24 ue24 in ue24s) {
                Assert.IsTrue(ReadWritPorts(Only.UE24s[ue24], ports0x00));
                Assert.IsTrue(ReadWritPorts(Only.UE24s[ue24], ports0xFF));
            }
        }

        [TestMethod()]
        public void GetPort_Test() {
            DigitalPortType dtp;
            for (Int32 bitNum = 0; bitNum < Enum.GetNames(typeof(R)).Length; bitNum++) {
                dtp = GetPort((R)bitNum);
                switch (bitNum) {
                    case Int32 b when 0 <= b && b <= 7:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortA);
                        break;
                    case Int32 b when 8 <= b && b <= 15:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortB);
                        break;
                    case Int32 b when 16 <= b && b <= 19:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortCL);
                        break;
                    case Int32 b when 20 <= b && b <= 23:
                        Assert.AreEqual(dtp, DigitalPortType.FirstPortCH);
                        break;
                }
            }
        }
        #endregion private method tests
    }
}