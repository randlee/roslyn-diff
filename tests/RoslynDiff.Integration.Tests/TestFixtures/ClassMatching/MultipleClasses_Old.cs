namespace TestFixtures;

public class ClassA : IService
{
    public void Execute() { }
}

public class ClassB
{
    public void Run() { }
}

public interface IService
{
    void Execute();
}
