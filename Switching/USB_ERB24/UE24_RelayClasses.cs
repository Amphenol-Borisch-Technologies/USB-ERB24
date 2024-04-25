using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ABT.TestSpace.TestExec.Switching.USB_ERB24.UE24;
using static ABT.TestSpace.TestExec.Switching.RelayForms;

namespace ABT.TestSpace.TestExec.Switching.USB_ERB24 {
    public sealed class SwitchedNet {
        public readonly String ID;
        public readonly String Alias;

        public SwitchedNet(String ID, String Alias) { this.ID = ID; this.Alias = Alias; }

        public override Boolean Equals(Object obj) {
            SwitchedNet sn = obj as SwitchedNet;
            if (ReferenceEquals(this, sn)) return true;
            return sn != null && sn.ID == ID && sn.Alias == Alias;
        }

        public override Int32 GetHashCode() { return 3 * ID.GetHashCode() + Alias.GetHashCode(); }

        public override String ToString() { return ID; }
    }

    public sealed class Relay {
        public readonly UE UE;
        public readonly R R;
        public readonly SwitchedNet C;
        public readonly SwitchedNet NC;
        public readonly SwitchedNet NO;

        public Relay(UE UE, R R, SwitchedNet C, SwitchedNet NC, SwitchedNet NO) {
            this.UE = UE; this.R = R; this.C = C; this.NC = NC; this.NO = NO;
            Validate();
        }

        private void Validate() {
            if (C.Alias == String.Empty) throw new ArgumentException($"Relay terminal Common '{C.Alias}' cannot be String.Empty.");
            if (C == NO) throw new ArgumentException($"Relay terminals Common '{C}' & Normally Open '{NO}' cannot be identical.");
            if (C == NC) throw new ArgumentException($"Relay terminals Common '{C}' & Normally Closed '{NC}' cannot be identical.");
            if (NC == NO) throw new ArgumentException($"Relay terminals Normally Closed '{NC}' & Normally Open '{NO}' cannot be identical.");
        }

        public static String GetUE(UE ue) { return Enum.GetName(typeof(UE), ue); }
        public static String GetR(R r) { return Enum.GetName(typeof(R), r); }

        public C.S Get() { return UE24.Get(UE, R); }

        public void Set(C.S state) { UE24.Set(UE, R, state); }

        public Boolean Is(C.S state) { return UE24.Is(UE, R, state); }

        public override Boolean Equals(Object obj) {
            Relay r = obj as Relay;
            if (ReferenceEquals(this, r)) return true;
            return r != null && r.UE == UE && r.R == R && C == r.C && NC == r.NC && NO == r.NO;
        }

        public override Int32 GetHashCode() { return 3 * UE.GetHashCode() + R.GetHashCode(); }
    }

    public sealed class Terminal {
        public readonly UE UE;
        public readonly R R;
        public readonly C.T T;

        public Terminal(UE UE, R R, C.T T) { this.UE = UE; this.R = R; this.T = T; }

        public override Boolean Equals(Object obj) {
            Terminal t = obj as Terminal;
            if (ReferenceEquals(this, t)) return true;
            return t != null && t.UE == UE && t.R == R && t.T == T;
        }

        public override Int32 GetHashCode() { return 3 * UE.GetHashCode() + R.GetHashCode() + T.GetHashCode(); }
    }

    public sealed class State {
        public readonly UE UE;
        public readonly R R;
        public readonly C.S S;

        public State(UE UE, R R, C.S S) { this.UE = UE; this.R = R; this.S = S; }

        public override Boolean Equals(Object obj) {
            State s = obj as State;
            if (ReferenceEquals(this, s)) return true;
            return s != null && s.UE == UE && s.R == R && s.S == S;
        }

        public override Int32 GetHashCode() { return 3 * UE.GetHashCode() + R.GetHashCode() + S.GetHashCode(); }
    }

    public sealed class SwitchedRoute {
        public readonly Tuple<SwitchedNet, SwitchedNet> SwitchedNetPair;
        public SwitchedRoute(Tuple<SwitchedNet, SwitchedNet> switchedNetPair) { SwitchedNetPair = switchedNetPair; }

        public Boolean Contains(SwitchedNet SN) { return SwitchedNetPair.Item1 == SN || SwitchedNetPair.Item2 == SN; }

        public override Boolean Equals(Object obj) {
            if (!(obj is SwitchedRoute sr)) return false;
            if (ReferenceEquals(this, sr)) return true;
            if (sr.SwitchedNetPair.Item1 == SwitchedNetPair.Item1 && sr.SwitchedNetPair.Item2 == SwitchedNetPair.Item2) return true;
            if (sr.SwitchedNetPair.Item1 == SwitchedNetPair.Item2 && sr.SwitchedNetPair.Item2 == SwitchedNetPair.Item1) return true;
            return false;
        }

        public override Int32 GetHashCode() { return 3 * SwitchedNetPair.GetHashCode(); }
    }

    public sealed class SwitchedRoutes {
        public readonly Dictionary<SwitchedRoute, HashSet<State>> SRs;

        public SwitchedRoutes(Dictionary<SwitchedRoute, HashSet<State>> RouteStates) { SRs = RouteStates; }

        public void Connect(SwitchedNet SN1, SwitchedNet SN2) { foreach (State s in SRs[GetSwitchedRoute(SN1, SN2)]) Set(s.UE, s.R, s.S); }

        public void Connect(SwitchedNet SN, HashSet<SwitchedNet> SNs) { foreach (SwitchedNet sn in SNs) Connect(SN, sn); }

        public void DisConnect(SwitchedNet SN1, SwitchedNet SN2) { foreach (State s in SRs[GetSwitchedRoute(SN1, SN2)]) Set(s.UE, s.R, GetStateOpposite(s.S)); }

        public void DisConnect(SwitchedNet SN, HashSet<SwitchedNet> SNs) { foreach (SwitchedNet sn in SNs) DisConnect(SN, sn); }

        public void Switch(SwitchedNet SN, SwitchedNet From, SwitchedNet To) {
            DisConnect(SN, From);
            Connect(SN, To);
        }

        public Boolean AreConnected(SwitchedNet SN1, SwitchedNet SN2) {
            Boolean ac = true;
            foreach (State s in SRs[GetSwitchedRoute(SN1, SN2)]) ac &= Is(s.UE, s.R, s.S);
            return ac;
        }

        public Boolean AreConnected(SwitchedNet SN, HashSet<SwitchedNet> SNs) {
            Boolean ac = true;
            foreach (SwitchedNet sn in SNs) ac &= AreConnected(SN, sn);
            return ac;
        }

        public Boolean AreDisConnected(SwitchedNet SN1, SwitchedNet SN2) { return !AreConnected(SN1, SN2); }

        public Boolean AreDisConnected(SwitchedNet SN, HashSet<SwitchedNet> SNs) { return !AreConnected(SN, SNs); }

        public Boolean AreConnectable(SwitchedNet SN1, SwitchedNet SN2) { return SRs.ContainsKey(new SwitchedRoute(Tuple.Create(SN1, SN2))); }

        public Boolean AreConnectable(SwitchedNet SN, HashSet<SwitchedNet> SNs) {
            Boolean ac = true;
            foreach (SwitchedNet sn in SNs) ac &= AreConnectable(SN, sn);
            return ac;
        }

        private SwitchedRoute GetSwitchedRoute(SwitchedNet SN1, SwitchedNet SN2) {
            SwitchedRoute sr = new SwitchedRoute(Tuple.Create(SN1, SN2));
            if (!SRs.ContainsKey(sr)) sr = new SwitchedRoute(Tuple.Create(SN2, SN1)); // If at first you don't succeed...
            if (!SRs.ContainsKey(sr)) throw new ArgumentException($"Invalid Route SwitchedNets SN1 '{SN1}' & SN2 '{SN2}'.");
            return sr;
        }

        public static C.S GetStateOpposite(C.S State) { return State == C.S.NO ? C.S.NC : C.S.NO; }

        public HashSet<SwitchedRoute> GetRoutes(SwitchedNet SN) {
            HashSet<SwitchedRoute> switchedRoutes = new HashSet<SwitchedRoute>();
            foreach (KeyValuePair<SwitchedRoute, HashSet<State>> kvp in SRs) { if (kvp.Key.Contains(SN)) switchedRoutes.Add(kvp.Key); }
            return switchedRoutes;
        }
    }

    public sealed class Relays {
        public readonly HashSet<Relay> Rs;
        public readonly Dictionary<String, HashSet<Terminal>> SNTs = new Dictionary<String, HashSet<Terminal>>();

        public Relays(HashSet<Relay> rs) {
            Rs = rs;
            Validate();

            //foreach (Relay r in this.Rs) {
            //    if (!this.SNTs.ContainsKey(r.C)) 
            //}
        }

        private void Validate() {
            StringBuilder sb = new StringBuilder($"Cannot currently accomodate USB-ERB24 Relays connected serially:{Environment.NewLine}Boards/Relays");
            List<(Relay, Relay)> rs =
                (from r1 in Rs
                 from r2 in Rs
                 where r1.C == r2.NC || r1.C == r2.NO
                 select (r1, r2)).ToList();
            if (rs.Count() != 0) {
                foreach ((Relay r1, Relay r2) in rs) {
                    sb.AppendLine("Below relay pair {R1, R2} serially connected, C1 to (NC2 ⨁ NO2)");
                    sb.AppendLine($"   B1='{Relay.GetUE(r1.UE)}', R1='{Relay.GetR(r1.R)}', C1='{r1.C}', NC1='{r1.NC}', NO1='{r1.NO}'.");
                    sb.AppendLine($"   B2='{Relay.GetUE(r2.UE)}', R2='{Relay.GetR(r2.R)}', C2='{r2.C}', NC2='{r2.NC}', NO2='{r2.NO}'.");
                    sb.AppendLine("");
                }
                throw new InvalidOperationException(sb.ToString());
            }
        }
    }
}
