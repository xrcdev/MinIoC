using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MyIoc
{
    #region Ioc
    public class Container : IDisposable
    {
        public readonly ConcurrentDictionary<Type, object> _instanceCache = new ConcurrentDictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _DicTypeConstructor = new Dictionary<Type, Func<object>>();

        private static Func<object> GetFirstConstructorFromType(Type itemType)
        {
            var constructors = itemType.GetConstructors();
            if (constructors.Length == 0) constructors = itemType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            var constructor = constructors.First();
            Expression expression = Expression.New(constructor);
            var lambda = Expression.Lambda(expression);
            return (Func<object>)lambda.Compile();
        }
        public void Register<IType, TType>() where TType : IType, new() where IType : class
        {
            _DicTypeConstructor[typeof(IType)] = GetFirstConstructorFromType(typeof(TType));
        }

        public void Dispose() => this.Dispose();
        /// <summary>
        /// 单例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T ResolveSingle<T>()
        {
            Func<object> constructor = null;
            if (_DicTypeConstructor.TryGetValue(typeof(T), out constructor))
            {
                return (T)_instanceCache.GetOrAdd(typeof(T), constructor());
            }
            return default(T);
        }
        //每次都是新的实例
        internal T ResolveNew<T>()
        {
            Func<object> constructor = null;
            if (_DicTypeConstructor.TryGetValue(typeof(T), out constructor))
            {
                return (T)constructor();
            }
            return default(T);
        }
    }
    #endregion

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



    public interface IFoo
    {

    }

    public class Foo : IFoo
    {
        public string fName { get; set; } = "F";
    }

}
