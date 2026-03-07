namespace JG.Modding
{
    public interface IModEntryPoint
    {
        void Initialize(IModContext context);
    }

    public interface IModContext
    {
        string ModId { get; }
        string ModPath { get; }
    }
}
