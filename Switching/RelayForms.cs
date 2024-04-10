namespace ABT.TestSpace.TestExec.Switching {
    public static class RelayForms {
        /// <summary>
        /// Relay Forms A, B & C.
        /// </summary>
        public static class A {
            public enum S { NO, C }
            /// <summary>
            /// Form A States:
            /// S.NO; Form A relay is de-energized and in normally opened state.
            /// S.C;  Form A relay is energized and in abnormally closed state.
            /// </summary>

            public enum T { C, NO }
            /// <summary>
            /// Form A Terminals.
            /// T.C; Form A relay Common terminal.
            /// T.NO; Form A relay Normally Open terminal.
            /// </summary>
        }

        public static class B {
            public enum S { NC, O }
            /// <summary>
            /// Form B States.
            /// B.NC; Form B relay is de-energized and in normally closed state.
            /// B.O;  Form B relay is energized and in abnormally opened state.
            /// </summary>

            public enum T { C, NC }
            /// <summary>
            /// Form B Terminals.
            /// T.C; Form B relay Common terminal.
            /// T.NC; Form B relay Normally Closed terminal.
            /// </summary>
        }

        public static class C {
            public enum S { NC, NO }
            /// <summary>
            /// Form C States.
            /// S.NC; Form C relay is de-energized and in normally closed/normally open state, with common C terminal connected to NC terminal & disconnected from NO terminal.
            /// S.NO; Form C relay is energized and in abnormally opened/abnormally closed state, with common C terminal disconnected from NC terminal & connected to NO terminal.
            /// </summary>

            public enum T { C, NC, NO }
            /// <summary>
            /// Form C Terminals.
            /// T.C; Form C relay Common terminal.
            /// T.NO; Form C relay Normally Closed terminal.
            /// T.NO; Form C relay Normally Open terminal.
            /// </summary>
        }
    }
}
