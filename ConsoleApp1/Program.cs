using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            Container container = new Container();
            container.Register<IFoo>(typeof(Foo));
            var fo = container.Resolve<IFoo>();


            container.Register<IFoo>(typeof(Foo));

            object instance = container.Resolve<IFoo>();
            Console.WriteLine(fo);
        }
    }
    #region Types used for tests
    public interface IFoo
    {

    }

    public class Foo : IFoo
    {
        public string fName { get; set; } = "a";
    }

    public interface IBar
    {
    }

    public class Bar : IBar
    {
        public IFoo Foo { get; set; }

        public Bar(IFoo foo)
        {
            Foo = foo;
        }
    }

    interface IBaz
    {
    }

    public class Baz : IBaz
    {
        public IFoo Foo { get; set; }
        public IBar Bar { get; set; }

        public Baz(IFoo foo, IBar bar)
        {
            Foo = foo;
            Bar = bar;
        }
    }

    class SpyDisposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
    #endregion
}
