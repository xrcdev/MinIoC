using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2_FW
{
    class Program
    {
        delegate void TestDelegate(string s);

        static Func<string> Func;

        static void M(string s)
        {
            Console.WriteLine(s);
        }

        static void Main(string[] args)
        {

            #region anonymous methoed
            //// Original delegate syntax required
            //// initialization with a named method.
            //TestDelegate testDelA = new TestDelegate(M);

            //// Original delegate syntax required
            //// initialization with a named method.
            //TestDelegate testDelA2 = M;

            ////Func= new Func<string>(m);
            ////-------------

            //// C# 2.0: A delegate can be initialized with
            //// inline code, called an "anonymous method." This
            //// method takes a string as an input parameter.
            //TestDelegate testDelB = delegate (string s) { Console.WriteLine(s); };

            //// C# 3.0. A delegate can be initialized with
            //// a lambda expression. The lambda also takes a string
            //// as an input parameter (x). The type of x is inferred by the compiler.
            //TestDelegate testDelC = (x) => { Console.WriteLine(x); };

            //// Invoke the delegates.
            //testDelA("Hello. My name is M and I write lines.");
            //testDelB("That's nothing. I'm anonymous and ");
            //testDelC("I'm a famous author.");

            //// Keep console window open in debug mode.
            //Console.WriteLine("Press any key to exit.");
            //Console.ReadKey();
            #endregion


            Container container1 = new Container();

            //container1.Register<IFoo>(() => new Foo()).AsSingleton();
            //var oScope2 = container1.Resolve<IFoo>();
            //Console.WriteLine($"{nameof(container1)},{nameof(oScope2)} hashcode:".PadRight(35, '\0') + oScope2.GetHashCode().ToString());
            //container1.Register<IFoo>(typeof(Foo)).AsSingleton();//▲
            //container1.Register<IFoo>(typeof(Foo)).AsScope();//▲
            container1.Register<IFoo>(typeof(Foo));//▲
            var foo1 = container1.Resolve<IFoo>();
            using (var scope = container1.CreateScope())
            {
                var foo2 = scope.Resolve<IFoo>();
                Console.WriteLine($"■ {nameof(foo1)} object.ReferenceEquals {nameof(foo2)} :".PadRight(35, '\0') + (object.ReferenceEquals(foo1, foo2)).ToString());

                var foo3 = scope.Resolve<IFoo>();
                Console.WriteLine($"■ {nameof(foo2)} object.ReferenceEquals {nameof(foo3)} :".PadRight(35, '\0') + (object.ReferenceEquals(foo2, foo3)).ToString());
            }
            var foo4 = container1.Resolve<IFoo>();
            Console.WriteLine($"■ {nameof(foo1)} object.ReferenceEquals {nameof(foo4)} :".PadRight(35, '\0') + (object.ReferenceEquals(foo1, foo4)).ToString());
            Console.WriteLine($"{Environment.NewLine}");

            //Console.WriteLine($"{nameof(container1)},{nameof(foo1)} hashcode:".PadRight(35, '\0') + foo1.GetHashCode().ToString());
            //Console.WriteLine($"{nameof(container1)},{nameof(foo1)} object.ReferenceEquals {nameof(oScope2)} :".PadRight(35, '\0') + (object.ReferenceEquals(foo1, oScope2)).ToString());


            container1.Register<IA>(typeof(A)).AsScope();//▲
            var a1 = container1.Resolve<IA>();
            using (var aScope = container1.CreateScope())
            {
                var a2 = aScope.Resolve<IA>();
                //Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"■ {nameof(a1)} object.ReferenceEquals {nameof(a2)} :".PadRight(35, '\0') + (object.ReferenceEquals(a1, a2)).ToString());
                var a3 = aScope.Resolve<IA>();
                Console.WriteLine($"■ {nameof(a2)} object.ReferenceEquals {nameof(a3)} :".PadRight(35, '\0') + (object.ReferenceEquals(a2, a3)).ToString());
            }
            var a4 = container1.Resolve<IA>();
            Console.WriteLine($"■ {nameof(a1)} object.ReferenceEquals {nameof(a4)} :".PadRight(35, '\0') + (object.ReferenceEquals(a1, a4)).ToString());
            Console.WriteLine($"{Environment.NewLine}");
           
            container1.Register<IB>(typeof(B)).AsSingleton();//▲
            var b1 = container1.Resolve<IB>();
            using (var bScope = container1.CreateScope())
            {
                var b2 = bScope.Resolve<IB>();
                Console.WriteLine($"■ {nameof(b1)} object.ReferenceEquals {nameof(b2)} :".PadRight(35, '\0') + (object.ReferenceEquals(b1, b2)).ToString());
                var b3 = bScope.Resolve<IB>();
                Console.WriteLine($"■ {nameof(b2)} object.ReferenceEquals {nameof(b3)} :".PadRight(35, '\0') + (object.ReferenceEquals(b2, b3)).ToString());
            }
            var b4 = container1.Resolve<IB>();
            Console.WriteLine($"■ {nameof(b1)} object.ReferenceEquals {nameof(b4)} :".PadRight(35, '\0') + (object.ReferenceEquals(b1, b4)).ToString());

            //using (var scope = container1.CreateScope())
            //{
            //    var foo3 = scope.Resolve<IFoo>();
            //    Console.WriteLine($"{nameof(container1)},{nameof(foo3)} hashcode:".PadRight(35, '\0') + foo1.GetHashCode().ToString());
            //    Console.WriteLine($"{nameof(foo1)} object.ReferenceEquals {nameof(foo3)} :".PadRight(35, '\0') + (object.ReferenceEquals(foo1, foo3)).ToString());

            //    var instance4 = scope.Resolve<IFoo>();

            //    Console.WriteLine($"{nameof(container1)},{nameof(instance4)} hashcode:".PadRight(35, '\0') + foo1.GetHashCode().ToString());
            //    Console.WriteLine($"{nameof(instance4)} object.ReferenceEquals {nameof(foo3)} :".PadRight(35, '\0') + (object.ReferenceEquals(instance4, foo3)).ToString());

            //}

            //container1.Register<IBaz>(typeof(Baz));
            //container1.Register<IBar>(typeof(Bar));
            //var oSingle = container1.Resolve<IBaz>();//▲必须把其他的依赖类型也注册; //如果子类型配置了不同的策略是什么效果 //如果构造函数是带参数的,注册顺序有影响没
            //Console.WriteLine($"{nameof(container1)},{nameof(oSingle)} hashcode:".PadRight(35, '\0') + oSingle.GetHashCode().ToString());
            //var oSingle2 = container1.Resolve<IBaz>();
            //Console.WriteLine($"{nameof(container1)},{nameof(oSingle2)} hashcode:".PadRight(35, '\0') + oSingle.GetHashCode().ToString());

            //Container container2 = new Container();

            //container2.Register<IFoo>(typeof(Foo)).AsScope();
            //var oScopeTwo = container2.Resolve<IFoo>();
            //Console.WriteLine($"{nameof(container2)},{nameof(oScopeTwo)} hashcode:".PadRight(35, '\0') + oScopeTwo.GetHashCode().ToString());
            //var oScopeTwo2 = container2.Resolve<IFoo>();
            //Console.WriteLine($"{nameof(container2)},{nameof(oScopeTwo2)} hashcode:".PadRight(35, '\0') + oScopeTwo2.GetHashCode().ToString());

            //container2.Register<IFoo>(typeof(Foo)).AsSingleton();
            //var oSingleTwo = container2.Resolve<IFoo>();
            //Console.WriteLine($"{nameof(container2)},{nameof(oSingleTwo)} hashcode:".PadRight(35, '\0') + oSingleTwo.GetHashCode().ToString());
            //var oSingleTwo2 = container2.Resolve<IFoo>();
            //Console.WriteLine($"{nameof(container2)},{nameof(oSingleTwo2)} hashcode:".PadRight(35, '\0') + oSingleTwo2.GetHashCode().ToString());


            //Console.WriteLine("");
            Console.Read();
        }
    }
    #region Types used for tests
    public interface IFoo
    {

    }

    public class Foo : IFoo
    {
        public string fName { get; set; } = "F";
    }

    public interface IA
    {

    }

    public class A : IA
    {
        public string fName { get; set; } = "A";
    }
    public interface IB
    {

    }

    public class B : IB
    {
        public string fName { get; set; } = "B";
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
