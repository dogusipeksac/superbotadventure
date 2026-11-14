using SuperBot.Save;

namespace SuperBot.Core
{
    public sealed class EconomyService
    {
        private readonly SaveService save;

        public EconomyService(SaveService save) => this.save = save;

        public int Bolts => save.Data.bolts;

        public void AddBolts(int amount)
        {
            if (amount <= 0) return;
            save.Data.bolts += amount;
            save.MarkDirty();
            GameEvents.RaiseBoltsChanged(save.Data.bolts);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || save.Data.bolts < amount) return false;
            save.Data.bolts -= amount;
            save.MarkDirty();
            GameEvents.RaiseBoltsChanged(save.Data.bolts);
            return true;
        }
    }
}
