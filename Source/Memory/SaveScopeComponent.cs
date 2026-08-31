using System;
using Verse;

namespace RimTelligence.Memory
{

    public class SaveScopeComponent : GameComponent
    {
        private static SaveScopeComponent _current;
        public static SaveScopeComponent Current => _current;

        private string saveId = "";

        public string SaveId => saveId;

        public SaveScopeComponent(Game game)
        {
            _current = this;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref saveId, "saveId", "");
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (string.IsNullOrEmpty(saveId))
                saveId = Guid.NewGuid().ToString();
        }
    }
}
