using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MyIoc
{
    class Program
    {
        static void Main(string[] args)
        {
            
            using (Container container = new Container())
            {
                container.Register<IFoo, Foo>();
                IFoo f1 = container.ResolveSingle<IFoo>();
                IFoo f2 = container.ResolveSingle<IFoo>();
                IFoo f3 = container.ResolveNew<IFoo>();
                IFoo f4 = container.ResolveNew<IFoo>();


            }

            //Console.WriteLine(object.ReferenceEquals(f1, f2));
            //Console.WriteLine(object.ReferenceEquals(f4, f3));
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
}
