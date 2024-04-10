using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static ABT.TestSpace.TestExec.Switching.RelayForms;

namespace ABT.TestSpace.TestExec.Switching {
    [TestClass()]
    public class RelayForms_Tests {
        #region Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
        [TestMethod()]
        public void RelayFormsA_Test() {
            Assert.AreEqual((Int32)A.S.NO, 0);
            Assert.AreEqual((Int32)A.S.C, 1);
            Assert.AreEqual(Enum.GetNames(typeof(A.S)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsB_Test() {
            Assert.AreEqual((Int32)B.S.NC, 0);
            Assert.AreEqual((Int32)B.S.O, 1);
            Assert.AreEqual(Enum.GetNames(typeof(B.S)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsC_Test() {
            Assert.AreEqual((Int32)C.S.NC, 0);
            Assert.AreEqual((Int32)C.S.NO, 1);
            Assert.AreEqual(Enum.GetNames(typeof(C.S)).Length, 2);
        }
        #endregion Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
    }
}