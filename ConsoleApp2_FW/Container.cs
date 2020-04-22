using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ConsoleApp2_FW
{
    /// <summary>
    /// 控制容器的反转处理已注册类型的依赖项注入
    /// Inversion of control container handles dependency injection for registered types
    /// </summary>
    public class Container : Container.IScope
    {
        #region 属性 _DicRegisteredTypesGlobal, _Lifetime
        /// <summary>
        /// 已注册类型的映射
        /// </summary>
        private readonly Dictionary<Type, Func<ILifetime, object>> _DicRegisteredTypesGlobal = new Dictionary<Type, Func<ILifetime, object>>();

        /// <summary>
        /// 提供生命周期管理; 继承ObjectCache, ILifetime
        /// </summary>
        private readonly ContainerLifetime _Lifetime;

        #endregion

        /// <summary>
        /// 创建一个IoC容器的新实例
        /// </summary>
        public Container()
        {
            _Lifetime = new ContainerLifetime(Set_DicRegisteredTypesGlobalByKey);
        }

        #region 实例方法

        #region Register
        /// <summary>
        ///注册工厂函数，将调用该函数来解析指定的接口
        /// </summary>
        /// <param name="interface">Interface to register</param>
        /// <param name="factory">Factory function</param>
        /// <returns></returns>
        public IRegisteredType Register(Type @interface, Func<object> factory)
        {
            return RegisterType(@interface, _ => factory());
        }

        /// <summary>
        /// 注册指定接口的实现类型
        /// </summary>
        /// <param name="interface">Interface to register</param>
        /// <param name="implementation">Implementing type</param>
        /// <returns></returns>
        public IRegisteredType Register(Type @interface, Type implementation)
        {
            return RegisterType(@interface, FactoryFromType(implementation));
        }

        private IRegisteredType RegisterType(Type itemType, Func<ILifetime, object> constructor)
        {
            //return new RegisteredType(itemType, f => _registeredTypesG[itemType] = f, constructor);
            return new RegisteredType(itemType, f => Set_DicRegisteredTypesGlobalByValue(itemType, f), constructor);
        }
        #endregion


        Func<ILifetime, object> Set_DicRegisteredTypesGlobalByKey(Type t)
        {
            Console.WriteLine("类型添加到词典" + (((object)t != null) ? t.ToString() : null));
            return _DicRegisteredTypesGlobal[t];
        }

        void Set_DicRegisteredTypesGlobalByValue(Type itype, Func<ILifetime, object> func)
        {
            Console.WriteLine("类型对应的构造函数添加到词典" + (((object)itype != null) ? itype.ToString() : null));
            _DicRegisteredTypesGlobal[itype] = func;
        }

        /// <summary>
        /// Returns the object registered for the given type, if registered
        /// </summary>
        /// <param name="type">Type as registered with the container</param>
        /// <returns>Instance of the registered type, if registered; otherwise <see langword="null"/></returns>
        public object GetService(Type type)
        {
            Func<ILifetime, object> registeredType;

            if (!_DicRegisteredTypesGlobal.TryGetValue(type, out registeredType))
            {
                return null;
            }

            return registeredType(_Lifetime);
        }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns>Scope object</returns>
        public IScope CreateScope()
        {
            return new ScopeLifetimeMgr(_Lifetime);
        }

        /// <summary>
        /// Disposes any <see cref="IDisposable"/> objects owned by this container.
        /// </summary>
        public void Dispose()
        {
            return _Lifetime.Dispose();
        }

        #endregion

        #region 静态方法
        /// <summary>
        /// 编译一个lambda，该lambda调用给定类型的第一个构造函数来解析参数
        /// Compiles a lambda that calls the given type's first constructor resolving arguments
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private static Func<ILifetime, object> FactoryFromType(Type itemType)
        {
            // Get first constructor for the type
            var constructors = itemType.GetConstructors();
            if (constructors.Length == 0)
            {
                // If no public constructor found, search for an internal constructor
                constructors = itemType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            }
            var constructor = constructors.First();

            // Compile constructor call as a lambda expression
            var arg = Expression.Parameter(typeof(ILifetime));
            //var exs = constructor.GetParameters().Select(
            //        param =>
            //        {
            //            var resolve = new Func<ILifetime, object>(lifetime => GetFromLifeTime(lifetime, param.ParameterType));
            //            var expresion2 = Expression.Call(Expression.Constant(resolve.Target), resolve.Method, arg);
            //            return Expression.Convert(expresion2, param.ParameterType);
            //        });
            var ps = constructor.GetParameters();
            List<Expression> exp2 = new List<Expression>();
            foreach (var param in ps)
            {
                var resolve = new Func<ILifetime, object>(lifetime => GetInstanceFromLifeTimeByType(lifetime, param.ParameterType));
                var expresion2 = Expression.Call(Expression.Constant(resolve.Target), resolve.Method, arg);
                var unaryExp = Expression.Convert(expresion2, param.ParameterType);
                exp2.Add(unaryExp);
            }
            Expression expression = Expression.New(constructor, exp2);

            var lambda = Expression.Lambda(expression, ("Lambda_express_") + itemType.Name, new[] { arg });
            var func = (Func<ILifetime, object>)lambda.Compile();
            return func;
        }

        static object GetInstanceFromLifeTimeByType(ILifetime ltime, Type tp)
        {
            return ltime.GetService(tp);
        }
        #endregion


        #region Public interfaces ;IScope ,ILifetime, IRegisteredType
        /// <summary>
        /// 表示某个时刻的范围对象的作用域
        /// </summary>
        public interface IScope : IDisposable, IServiceProvider
        {
        }

        /// <summary>
        /// 给IScope对象提供生命周期管理策略
        /// </summary>
        interface ILifetime : IScope
        {

            /// <summary>
            /// 获取一个单例对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory);

            /// <summary>
            /// 获取一个作用域对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            object GetServicePerScope(Type type, Func<ILifetime, object> factory);
        }

        /// <summary>
        /// 代表注册的类型
        /// Container.Register 方法 返回这个类型,并可以允许进一步配置注册
        /// </summary>
        public interface IRegisteredType
        {
            /// <summary>
            /// 使注册类型为单例
            /// </summary>
            void AsSingleton();

            /// <summary>
            /// 将注册类型设为一个作用域类型（范围内的单个实例）
            /// </summary>
            void AsScope();
        }
        #endregion



        #region 其他成员对象 ,ObjectCache ,ContainerLifetime,RegisteredType,ScopeLifetimeMgr

        #region Lifetime management
        /// <summary>
        /// 提供终身的缓存逻辑,添加或者获取,abstract
        /// </summary>
        abstract class ObjectCache
        {
            /// <summary>
            /// 对象实例的缓存
            /// </summary>
            private readonly ConcurrentDictionary<Type, object> _instanceCache = new ConcurrentDictionary<Type, object>();

            /// <summary>
            /// 添加或者获取缓存对象 
            /// </summary>
            protected object GetCached(Type type, Func<ILifetime, object> constructor, ILifetime lifetime)
            {
                return _instanceCache.GetOrAdd(type, _ => constructor(lifetime));
            }

            public void Dispose()
            {
                foreach (var obj in _instanceCache.Values)
                    (obj as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// 容器生命周期管理, 继承ObjectCache, ILifetime
        /// </summary>
        class ContainerLifetime : ObjectCache, ILifetime
        {
            string LifetimeType = string.Empty;

            /// <summary>
            /// 获取给定类型中检索构造函数,由 包含它的容器提供
            /// 使用的数据集合是 Container 中的 _registeredTypes constructor
            /// </summary>
            public Func<Type, Func<ILifetime, object>> GetConstructorByType { get; private set; }

            public ContainerLifetime(Func<Type, Func<ILifetime, object>> getFactory)
            {
                GetConstructorByType = getFactory;
            }
            //public ContainerLifetime( )
            //{
            //    GetFactory = GetRegisteredTypes;
            //}

            public object GetService(Type type)
            {
                Func<ILifetime, object> factory = GetConstructorByType(type);
                return factory(this);
            }

            /// <summary>
            /// 单例模式 获取缓存对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
            {
                return GetCached(type, factory, this);
            }

            /// <summary>
            /// // 获取作用域内的缓存对象,在容器的级别范围内,对象是单例的
            /// At container level, per-scope items are equivalent to singletons
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            public object GetServicePerScope(Type type, Func<ILifetime, object> factory)
            {
                return GetServiceAsSingleton(type, factory);
            }
        }

        /// <summary>
        /// 作用域管理
        /// </summary>
        class ScopeLifetimeMgr : ObjectCache, ILifetime
        {
            // Singletons come from parent container's lifetime
            private readonly ContainerLifetime _parentLifetime;

            public ScopeLifetimeMgr(ContainerLifetime parentContainer)
            {
                _parentLifetime = parentContainer;
            }

            public object GetService(Type type)
            {
                return _parentLifetime.GetConstructorByType(type)(this);
            }

            // 单例解决方案委托给父辈的生命周期
            public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
            {
                return _parentLifetime.GetServiceAsSingleton(type, factory);
            }

            // Per-scope objects get cached
            public object GetServicePerScope(Type type, Func<ILifetime, object> factory)
            {
                return GetCached(type, factory, this);
            }
        }
        #endregion
        /// <summary>
        ///Registered Type应该是将项目绑定到其容器的短暂对象
        ///并允许用户将其标记为单例或按作用域的项目
        /// RegisteredType is supposed to be a short lived object tying an item to its container
        /// and allowing users to mark it as a singleton or per-scope item
        /// </summary>
        class RegisteredType : IRegisteredType
        {
            private readonly Type _itemType;
            private readonly Action<Func<ILifetime, object>> _registerFactory;
            private readonly Func<ILifetime, object> _constructor;

            public RegisteredType(Type itemType, Action<Func<ILifetime, object>> registerFactory, Func<ILifetime, object> constructor)
            {
                _itemType = itemType;
                _registerFactory = registerFactory;
                _constructor = constructor;

                registerFactory(_constructor);
            }

            public void AsSingleton()
            {
                _registerFactory(GetObjAsSingleton);
            }

            public void AsScope()
            {
                //_registerFactory(lifetime => lifetime.GetServicePerScope(_itemType, _constructor));
                _registerFactory(GetObjPerScope);
            }

            object GetObjPerScope(ILifetime ltime)
            {
                return ltime.GetServicePerScope(_itemType, _constructor);
            }
            object GetObjAsSingleton(ILifetime ltime)
            {
                return ltime.GetServiceAsSingleton(_itemType, _constructor);
            }
        }
        #endregion
    }


    /// <summary>
    /// 容器的扩展方法,提供注册和获取实例的方法
    /// </summary>
    static class ContainerExtensions
    {
        #region Register
        /// <summary>
        /// 注册指定接口的实现类型
        /// </summary>
        /// <typeparam name="T">注册的接口</typeparam>
        /// <param name="container">This container instance</param>
        /// <param name="type">实施类型</param>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<T>(this Container container, Type type)
        {
            return container.Register(typeof(T), type);
        }

        /// <summary>
        /// 注册指定接口的实现类型
        /// </summary>
        /// <typeparam name="TInterface">Interface to register</typeparam>
        /// <typeparam name="TImplementation">Implementing type</typeparam>
        /// <param name="container">This container instance</param>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<TInterface, TImplementation>(this Container container)
            where TImplementation : TInterface
        {
            return container.Register(typeof(TInterface), typeof(TImplementation));
        }

        /// <summary>
        /// 注册工厂函数，将调用该函数来解析指定的接口
        /// </summary>
        /// <typeparam name="T">Interface to register</typeparam>
        /// <param name="container">This container instance</param>
        /// <param name="factory">Factory method</param>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<T>(this Container container, Func<T> factory)
        {
            return container.Register(typeof(T), () => factory());
        }

        ///// <summary>
        ///// 注册一个类型
        ///// </summary>
        ///// <param name="container">This container instance</param>
        ///// <typeparam name="T">Type to register</typeparam>
        ///// <returns>IRegisteredType object</returns>
        //public static Container.IRegisteredType Register<T>(this Container container)
        //{
        //    return container.Register(typeof(T), typeof(T));
        //}

        #endregion

        /// <summary>
        /// 返回指定接口的实现(一般是类)
        /// </summary>
        /// <typeparam name="T">接口的类型</typeparam>
        /// <param name="scope">1个作用域实例</param>
        /// <returns>Object implementing the interface</returns>
        public static T Resolve<T>(this Container.IScope scope)
        {
            return (T)scope.GetService(typeof(T));
        }
    }
}
