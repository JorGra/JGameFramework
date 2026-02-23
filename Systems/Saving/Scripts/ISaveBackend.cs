namespace JGameFramework.Saving
{
    public interface ISaveBackend
    {
        string Name { get; }
        void Save<T>(string slotId, string caseId, T value);
        T Load<T>(string slotId, string caseId, T defaultValue);
        bool Exists(string slotId, string caseId);
        void Delete(string slotId, string caseId);
        void DeleteSlot(string slotId);
    }
}
