using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ABT.Switching.Tests {
    [TestClass()]
    public class RelayFormsTests {
        #region Declarations, tested in case they change & impact other TestLibrary.Switching methods and/or other MSTest tests.
        [TestMethod()]
        public void RelayFormsATest() {
            Assert.AreEqual((Int32)RelayForms.A.NO, 0);
            Assert.AreEqual((Int32)RelayForms.A.C, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.A)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsBTest() {
            Assert.AreEqual((Int32)RelayForms.B.NC, 0);
            Assert.AreEqual((Int32)RelayForms.B.O, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.B)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsCTest() {
            Assert.AreEqual((Int32)RelayForms.C.NC, 0);
            Assert.AreEqual((Int32)RelayForms.C.NO, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.C)).Length, 2);
        }
        #endregion Declarations, tested in case they change & impact other TestLibrary.Switching methods and/or other MSTest tests.
    }
}