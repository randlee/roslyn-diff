namespace TestFixtures;

public class ClassA : IService
{
    public void Execute() { }
    public void DoWork() { }
}

public class ClassB
{
    public void Run() { }
    public void Stop() { }
}

public interface IService
{
    void Execute();
    void DoWork();
}
