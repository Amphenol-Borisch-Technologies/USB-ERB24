using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ABT.TestSpace.Switching
{
    [TestClass()]
    public class RelayForms_Tests
    {
        #region Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
        [TestMethod()]
        public void RelayFormsA_Test()
        {
            Assert.AreEqual((int)RelayForms.A.NO, 0);
            Assert.AreEqual((int)RelayForms.A.C, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.A)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsB_Test()
        {
            Assert.AreEqual((int)RelayForms.B.NC, 0);
            Assert.AreEqual((int)RelayForms.B.O, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.B)).Length, 2);
        }

        [TestMethod()]
        public void RelayFormsC_Test()
        {
            Assert.AreEqual((int)RelayForms.C.NC, 0);
            Assert.AreEqual((int)RelayForms.C.NO, 1);
            Assert.AreEqual(Enum.GetNames(typeof(RelayForms.C)).Length, 2);
        }
        #endregion Declarations, tested in case they change & impact other TestExecutive.Switching methods and/or other MSTest tests.
    }
}