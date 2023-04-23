namespace ABT.TestSpace.Switching {
    public static class RelayForms {
        // https://forum.digikey.com/t/understanding-form-a-form-b-form-c-contact-configuration/811

        public enum A { NO, C }
        // A.NO; Form A relay is deenergized and in normally opened state.
        // A.C;  Form A relay is energized and in abnormally closed state.

        public enum B { NC, O }
        // B.NC; Form B relay is deenergized and in normally closed state.
        // B.O;  Form B relay is energized and in abnormally opened state.

        public enum C { NC, NO }
        // C.NC; Form C relay is deenergized and in normally closed/normally open state, with common C terminal connected to NC terminal & disconnected from NO terminal.
        // C.NO; Form C relay is energized and in abnormally opened/abnormally closed state, with common C terminal disconnected from NC terminal & connected to NO terminal.
    }
}
