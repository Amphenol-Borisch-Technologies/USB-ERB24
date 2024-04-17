using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MccDaq; // MCC DAQ Universal Library 6.73 from https://www.mccdaq.com/Software-Downloads.
using static ABT.TestSpace.TestExec.Switching.RelayForms;
using static ABT.TestSpace.TestExec.Switching.USB_ERB24.UE24;
using System.Diagnostics;

namespace ABT.TestSpace.TestExec.Switching.USB_ERB24 {
    [TestClass()]
    public class USB_ERB24_Tests {
        // TODO: Add tests for class' USB_ERB24 3 public methods that aren't tested yet.
        private static readonly UInt16[] ports0x00 = { 0x0000, 0x0000, 0x0000, 0x0000 };
        private static readonly UInt16[] ports0xFF = { 0x00FF, 0x00FF, 0x000F, 0x000F };
        private static readonly UInt16[] ports0xAA = { 0x00AA, 0x00AA, 0x000A, 0x000A };
        private static readonly UInt16[] ports0x55 = { 0x0055, 0x0055, 0x0005, 0x0005 };

        private static IEnumerable<Object[]> GetUEs { get { return new[] { new Object[] { GetUEHashSet() } }; } }

        private static IEnumerable<Object[]> GetUEεRs { get { return new[] { new Object[] { GetUEHashSet(), GetRHashSet() } }; } }
        
        private static IEnumerable<Object[]> GetUEεRεCs { get { return new[] { new Object[] { GetUEHashSet(), GetDictionaryRεC_NC(), GetDictionaryRεC_NO() } }; } }

        private static void ConfirmRs(UE ue, C.S c) {
            DialogResult dr = MessageBox.Show(
                $"Confirm All USB-ERB24 ue '{ue}' R are in state '{c}'.{Environment.NewLine}{Environment.NewLine}" +
                $"Click Yes if confirmed, No if any R aren't continuous from terminals 'C' to '{c}'.", "Verify all USB-ERB24 R", MessageBoxButtons.YesNoCancel);
            if (dr == DialogResult.No) Assert.Fail();
            if (dr == DialogResult.Cancel) Assert.Inconclusive();
        }

        private static Dictionary<R, C.S> GetDictionaryRεC_NC() { return GetRHashSet().ToDictionary(r => r, r => C.S.NC); }

        private static Dictionary<R, C.S> GetDictionaryRεC_NO() { return GetRHashSet().ToDictionary(r => r, r => C.S.NO); }

        private static Dictionary<R, C.S> GetDictionaryRεC_NC_NO() {
            return new Dictionary<R, C.S>() {
                {R.C01, C.S.NC}, {R.C02, C.S.NO}, {R.C03, C.S.NC}, {R.C04, C.S.NO}, {R.C05, C.S.NC}, {R.C06, C.S.NO}, {R.C07, C.S.NC}, {R.C08, C.S.NO},
                {R.C09, C.S.NC}, {R.C10, C.S.NO}, {R.C11, C.S.NC}, {R.C12, C.S.NO}, {R.C13, C.S.NC}, {R.C14, C.S.NO}, {R.C15, C.S.NC}, {R.C16, C.S.NO},
                {R.C17, C.S.NC}, {R.C18, C.S.NO}, {R.C19, C.S.NC}, {R.C20, C.S.NO}, {R.C21, C.S.NC}, {R.C22, C.S.NO}, {R.C23, C.S.NC}, {R.C24, C.S.NO},
            };
        }

        private static Dictionary<R, C.S> GetDictionaryRεC_NO_NC() {
            return new Dictionary<R, C.S>() {
                {R.C01, C.S.NO}, {R.C02, C.S.NC}, {R.C03, C.S.NO}, {R.C04, C.S.NC}, {R.C05, C.S.NO}, {R.C06, C.S.NC}, {R.C07, C.S.NO}, {R.C08, C.S.NC},
                {R.C09, C.S.NO}, {R.C10, C.S.NC}, {R.C11, C.S.NO}, {R.C12, C.S.NC}, {R.C13, C.S.NO}, {R.C14, C.S.NC}, {R.C15, C.S.NO}, {R.C16, C.S.NC},
                {R.C17, C.S.NO}, {R.C18, C.S.NC}, {R.C19, C.S.NO}, {R.C20, C.S.NC}, {R.C21, C.S.NO}, {R.C22, C.S.NC}, {R.C23, C.S.NO}, {R.C24, C.S.NC},
            };
        }

        private static Dictionary<R, C.S> GetDictionaryRεC_X_NO() {
            return new Dictionary<R, C.S>() {
                {R.C02, C.S.NO}, {R.C04, C.S.NO}, {R.C06, C.S.NO}, {R.C08, C.S.NO},
                {R.C10, C.S.NO}, {R.C12, C.S.NO}, {R.C14, C.S.NO}, {R.C16, C.S.NO},
                {R.C18, C.S.NO}, {R.C20, C.S.NO}, {R.C22, C.S.NO}, {R.C24, C.S.NO},
            };
        }

        private static Dictionary<R, C.S> GetDictionaryRεC_NO_X() {
            return new Dictionary<R, C.S>() {
                {R.C01, C.S.NO}, {R.C03, C.S.NO}, {R.C05, C.S.NO}, {R.C07, C.S.NO},
                {R.C09, C.S.NO}, {R.C11, C.S.NO}, {R.C13, C.S.NO}, {R.C15, C.S.NO},
                {R.C17, C.S.NO}, {R.C19, C.S.NO}, {R.C21, C.S.NO}, {R.C23, C.S.NO},
            };
        }

        private static HashSet<R> GetRHashSet() { return new HashSet<R>(Enum.GetValues(typeof(R)).Cast<R>()); }

        private static HashSet<UE> GetUEHashSet() { return new HashSet<UE>(Enum.GetValues(typeof(UE)).Cast<UE>()); }

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
        [DynamicData(nameof(GetUEs))]
        public void UEs_Test(HashSet<UE> ues) { for (Int32 i = 0; i < ues.Count; i++) Assert.IsTrue(ues.Contains((UE)i)); }

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
        [DynamicData(nameof(GetUEεRs))]
        public void Is_Test(HashSet<UE> ues, HashSet<R> rs) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                foreach (R r in rs) Assert.IsTrue(Is(ue, r, C.S.NC));
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                foreach (R r in rs) Assert.IsTrue(Is(ue, r, C.S.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void AreRs_Test(HashSet<UE> ues, HashSet<R> rs) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                Assert.IsTrue(Are(ue, rs, C.S.NC));
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                Assert.IsTrue(Are(ue, rs, C.S.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRεCs))]
        public void AreRεC_Test(HashSet<UE> ues, Dictionary<R, C.S> RεC_NC, Dictionary<R, C.S> RεC_NO) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                Assert.IsTrue(Are(ue, RεC_NC));
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                Assert.IsTrue(Are(ue, RεC_NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void AreUE_C_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                Assert.IsTrue(Are(ue, C.S.NC));
                ConfirmRs(ue, C.S.NC);
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                Assert.IsTrue(Are(ue, C.S.NO));
                ConfirmRs(ue, C.S.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void AreUEs_C_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                Assert.IsTrue(Are(ue, C.S.NC));
            }
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                Assert.IsTrue(Are(ue, C.S.NO));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void AreUEs_Rs_C_Test(HashSet<UE> ues, HashSet<R> rs) {
            foreach (UE ue in ues) PortsWrite(Only.USB_ERB24s[ue], ports0x00);
            Assert.IsTrue(Are(ues, rs, C.S.NC));
            foreach (UE ue in ues) PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
            Assert.IsTrue(Are(ues, rs, C.S.NO));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void AreUEs_RεC_Test(HashSet<UE> ues) {
            Dictionary<UE, Dictionary<R, C.S>> ueεRεC = ues.ToDictionary(ue => ue, ue => GetDictionaryRεC_NC());

            foreach (UE ue in ues) PortsWrite(Only.USB_ERB24s[ue], ports0x00);
            Assert.IsTrue(Are(ueεRεC));
            foreach (UE ue in ues) PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
            ueεRεC = ues.ToDictionary(ue => ue, ue => GetDictionaryRεC_NO());
            Assert.IsTrue(Are(ueεRεC));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void Are_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                Boolean arelow = true;
                arelow &= Are(ue, C.S.NC);
                Assert.IsTrue(arelow);
            }
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                Boolean areHIGH = true;
                areHIGH &= Are(ue, C.S.NO);
                Assert.IsTrue(areHIGH);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void GetR_Test(HashSet<UE> ues, HashSet<R> rs) {
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                foreach (R r in rs) Assert.AreEqual(Get(ue, r), C.S.NC);
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                foreach (R r in rs) Assert.AreEqual(Get(ue, r), C.S.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void Get_Test(HashSet<UE> ues) {
            Dictionary<R, C.S> RεC;
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                RεC = Get(ue);
                foreach (KeyValuePair<R, C.S> kvp in RεC) Assert.AreEqual(kvp.Value, C.S.NC);
                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                RεC = Get(ue);
                foreach (KeyValuePair<R, C.S> kvp in RεC) Assert.AreEqual(kvp.Value, C.S.NO);
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void GetRεC_Test(HashSet<UE> ues) {
            Dictionary<UE, Dictionary<R, C.S>> ueεRεC_Test = new Dictionary<UE, Dictionary<R, C.S>>();
            foreach (UE ue in ues) PortsWrite(Only.USB_ERB24s[ue], ports0x00);
            foreach (UE ue in ues) ueεRεC_Test.Add(ue, GetDictionaryRεC_NC());
            Dictionary<UE, Dictionary<R, C.S>> ueεRεC = Get();
            Assert.AreEqual(ueεRεC_Test.Count, ueεRεC.Count);
            foreach (UE ue in ues) {
                Assert.AreEqual(ueεRεC_Test[ue].Count, ueεRεC[ue].Count);
                Assert.IsTrue(!ueεRεC_Test[ue].Except(ueεRεC[ue]).Any());
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void GetRs_Test(HashSet<UE> ues) {
            Dictionary<R, C.S> RεC_Test;
            Dictionary<R, C.S> RεC;
            foreach (UE ue in ues) {
                PortsWrite(Only.USB_ERB24s[ue], ports0x00);
                RεC_Test = GetDictionaryRεC_NC();
                RεC = Get(ue, GetRHashSet());
                Assert.AreEqual(RεC_Test.Count, RεC.Count);
                Assert.IsTrue(!RεC.Except(RεC_Test).Any());

                PortsWrite(Only.USB_ERB24s[ue], ports0xFF);
                RεC_Test = GetDictionaryRεC_NO();
                RεC = Get(ue, GetRHashSet());
                Assert.AreEqual(RεC_Test.Count, RεC.Count);
                Assert.IsTrue(!RεC.Except(RεC_Test).Any());
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void SetUE_C_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                for (Int32 i = 0; i < 4; i++) {
                    Set(ue, C.S.NO);
                    Set(ue, C.S.NC);
                }
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
                Set(ue, C.S.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void SetUE_RεC_Test(HashSet<UE> ues) {
            UInt16[] ports;
            foreach (UE ue in ues) {
                Set(ue, GetDictionaryRεC_NC());
                ports = PortsRead(Only.USB_ERB24s[ue]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));

                Set(ue, GetDictionaryRεC_X_NO());
                ports = PortsRead(Only.USB_ERB24s[ue]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xAA.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));

                Set(ue, GetDictionaryRεC_NO_X());
                ports = PortsRead(Only.USB_ERB24s[ue]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));

                Set(ue, new Dictionary<R, C.S>() { { R.C01, C.S.NC }, { R.C08, C.S.NC }, { R.C09, C.S.NC }, { R.C16, C.S.NC }, { R.C17, C.S.NC }, { R.C20, C.S.NC }, { R.C21, C.S.NC }, { R.C24, C.S.NC } });
                ports = PortsRead(Only.USB_ERB24s[ue]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.AreEqual(ports[(Int32)PORTS.A], 0x7E);
                Assert.AreEqual(ports[(Int32)PORTS.B], 0x7E);
                Assert.AreEqual(ports[(Int32)PORTS.CL], 0x6);
                Assert.AreEqual(ports[(Int32)PORTS.CH], 0x6);

                Set(ue, new Dictionary<R, C.S>() { { R.C01, C.S.NO }, { R.C08, C.S.NO }, { R.C09, C.S.NO }, { R.C16, C.S.NO }, { R.C17, C.S.NO }, { R.C20, C.S.NO }, { R.C21, C.S.NO }, { R.C24, C.S.NO } });
                ports = PortsRead(Only.USB_ERB24s[ue]);
                for (Int32 i = 0; i < ports.Length; i++) Console.WriteLine($"Port[{i}={ports[i]:X}");
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void SetUE_R_C_Test(HashSet<UE> ues, HashSet<R> rs) {
            ErrorInfo errorInfo;
            DigitalLogicState digitalLogicState;
            foreach (UE ue in ues) {
                foreach (R r in rs) {
                    Set(ue, r, C.S.NC);
                    errorInfo = Only.USB_ERB24s[ue].DBitIn(DigitalPortType.FirstPortA, (Int32)r, out digitalLogicState);
                    if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(Only.USB_ERB24s[ue], errorInfo);
                    Assert.AreEqual(digitalLogicState, DigitalLogicState.Low);

                    Set(ue, r, C.S.NO);
                    errorInfo = Only.USB_ERB24s[ue].DBitIn(DigitalPortType.FirstPortA, (Int32)r, out digitalLogicState);
                    if (errorInfo.Value != ErrorInfo.ErrorCode.NoErrors) ProcessErrorInfo(Only.USB_ERB24s[ue], errorInfo);
                    Assert.AreEqual(digitalLogicState, DigitalLogicState.High);
                }
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void SetUE_Rs_C_Test(HashSet<UE> ues, HashSet<R> rs) {
            foreach (UE ue in ues) {
                Set(ue, rs, C.S.NC);
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
                Set(ue, rs, C.S.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void SetUEs_C_Test(HashSet<UE> ues) {
            Set(ues, C.S.NC);
            foreach (UE ue in ues) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            Set(ues, C.S.NO);
            foreach (UE ue in ues) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEεRs))]
        public void SetUEs_Rs_C_Test(HashSet<UE> ues, HashSet<R> rs) {
            Set(ues, rs, C.S.NC);
            foreach (UE ue in ues) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            Set(ues, rs, C.S.NO);
            foreach (UE ue in ues) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void SetUEεRεC_Test(HashSet<UE> ues) {
            Dictionary<UE, Dictionary<R, C.S>> ueεRεC = new Dictionary<UE, Dictionary<R, C.S>>();
            foreach (UE ue in ues) ueεRεC.Add(ue, GetDictionaryRεC_NO());
            Set(ueεRεC);
            foreach (UE ue in ues) Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            ueεRεC = new Dictionary<UE, Dictionary<R, C.S>>();
            foreach (UE ue in ues) ueεRεC.Add(ue, GetDictionaryRεC_NC());
            Set(ueεRεC);
            foreach (UE ue in ues) Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void SetC_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                Set(ue, C.S.NC);
                Assert.IsTrue(ports0x00.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));

                Set(ue, C.S.NO);
                Assert.IsTrue(ports0xFF.SequenceEqual(PortsRead(Only.USB_ERB24s[ue])));
            }
        }
        #endregion public method tests

        #region private method tests
        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void PortRead_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                ErrorInfo errorInfo = new ErrorInfo();
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortA, ports0x00[(Int32)PORTS.A]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortB, ports0x00[(Int32)PORTS.B]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCL, ports0x00[(Int32)PORTS.CL]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCH, ports0x00[(Int32)PORTS.CH]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortA, ports0xFF[(Int32)PORTS.A]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortB, ports0xFF[(Int32)PORTS.B]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(Int32)PORTS.CL]));
                Assert.IsTrue(WriteReadPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(Int32)PORTS.CH]));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void PortsRead_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                Assert.IsTrue(WriteReadPorts(Only.USB_ERB24s[ue], ports0x00));
                Assert.IsTrue(WriteReadPorts(Only.USB_ERB24s[ue], ports0xFF));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void PortWrite_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                ErrorInfo errorInfo = new ErrorInfo();
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortA, ports0x00[(Int32)PORTS.A]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortB, ports0x00[(Int32)PORTS.B]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCL, ports0x00[(Int32)PORTS.CL]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCH, ports0x00[(Int32)PORTS.CH]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortA, ports0xFF[(Int32)PORTS.A]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortB, ports0xFF[(Int32)PORTS.B]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCL, ports0xFF[(Int32)PORTS.CL]));
                Assert.IsTrue(ReadWritPort(Only.USB_ERB24s[ue], errorInfo, DigitalPortType.FirstPortCH, ports0xFF[(Int32)PORTS.CH]));
            }
        }

        [TestMethod()]
        [DynamicData(nameof(GetUEs))]
        public void PortsWrite_Test(HashSet<UE> ues) {
            foreach (UE ue in ues) {
                Assert.IsTrue(ReadWritPorts(Only.USB_ERB24s[ue], ports0x00));
                Assert.IsTrue(ReadWritPorts(Only.USB_ERB24s[ue], ports0xFF));
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