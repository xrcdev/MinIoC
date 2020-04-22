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
        static void Main(string[] args)
        {
            //System.Linq.Expressions.BinaryExpression binaryExpression =
            //    System.Linq.Expressions.Expression.MakeBinary(
            //        System.Linq.Expressions.ExpressionType.Subtract,
            //        System.Linq.Expressions.Expression.Constant(53),
            //        System.Linq.Expressions.Expression.Constant(14));
            //Console.WriteLine(binaryExpression.ToString());
            //var lambda = Expression.Lambda(binaryExpression);
            //var cp = lambda.Compile();
            //var aa = cp;

            Container container1 = new Container();

            container1.Register<IFoo>(typeof(Foo)).AsScope();
            var oScope = container1.Resolve<IFoo>();
            container1.Register<IFoo>(() => new Foo()).AsScope();
            var oScope2 = container1.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container1)},{nameof(oScope)} hashcode:".PadRight(35, '\0') + oScope.GetHashCode().ToString());
            Console.WriteLine($"{nameof(container1)},{nameof(oScope2)} hashcode:".PadRight(35, '\0') + oScope2.GetHashCode().ToString());

            using (var scope = container1.CreateScope())
            {
                var instance3 = scope.Resolve<IFoo>();
                var instance4 = scope.Resolve<IFoo>();

                Console.WriteLine($"{nameof(container1)},{nameof(instance3)} hashcode:".PadRight(35, '\0') + oScope.GetHashCode().ToString());
                Console.WriteLine($"{nameof(container1)},{nameof(instance4)} hashcode:".PadRight(35, '\0') + oScope.GetHashCode().ToString());

            }





            container1.Register<IFoo>(typeof(Foo)).AsSingleton();
            var oSingle = container1.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container1)},{nameof(oSingle)} hashcode:".PadRight(35, '\0') + oSingle.GetHashCode().ToString());
            var oSingle2 = container1.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container1)},{nameof(oSingle2)} hashcode:".PadRight(35, '\0') + oSingle.GetHashCode().ToString());

            Container container2 = new Container();

            container2.Register<IFoo>(typeof(Foo)).AsScope();
            var oScopeTwo = container2.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container2)},{nameof(oScopeTwo)} hashcode:".PadRight(35, '\0') + oScopeTwo.GetHashCode().ToString());
            var oScopeTwo2 = container2.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container2)},{nameof(oScopeTwo2)} hashcode:".PadRight(35, '\0') + oScopeTwo2.GetHashCode().ToString());

            container2.Register<IFoo>(typeof(Foo)).AsSingleton();
            var oSingleTwo = container2.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container2)},{nameof(oSingleTwo)} hashcode:".PadRight(35, '\0') + oSingleTwo.GetHashCode().ToString());
            var oSingleTwo2 = container2.Resolve<IFoo>();
            Console.WriteLine($"{nameof(container2)},{nameof(oSingleTwo2)} hashcode:".PadRight(35, '\0') + oSingleTwo2.GetHashCode().ToString());


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
