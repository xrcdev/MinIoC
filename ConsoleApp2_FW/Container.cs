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
    /// </summary>
    public class Container : Container.IBaseObjectProvider
    {
        #region 属性 _DicRegisteredTypesGlobal, _Lifetime
        /// <summary>
        /// 保存 已注册类型和构造函数的映射
        /// 构造函数可以是: constructor,GetObjPerScope,GetObjAsSingleton
        /// </summary>
        private readonly Dictionary<Type, Func<IObjectProvider, object>> _RegisteredTypeDic = new Dictionary<Type, Func<IObjectProvider, object>>();

        /// <summary>
        /// 提供生命周期管理; 继承ObjectCache, ILifetime
        /// </summary>
        private readonly SingleObjectProvider _SingleObjectProvider;
        #endregion

        /// <summary>
        /// 创建一个IoC容器的新实例
        /// </summary>
        public Container()
        {
            _SingleObjectProvider = new SingleObjectProvider(GetConstructorFromDicByType);
        }

        #region 实例方法

        Func<IObjectProvider, object> GetConstructorFromDicByType(Type t)
        {
            Console.WriteLine($"从词典获取到类型:{(((object)t != null) ? t.ToString() : null)},对应的构造函数{_RegisteredTypeDic[t].ToString()}:");
            return _RegisteredTypeDic[t];
        }

        /// <summary>
        /// Returns the object registered for the given type, if registered
        /// </summary>
        /// <param name="type">Type as registered with the container</param>
        /// <returns>Instance of the registered type, if registered; otherwise <see langword="null"/></returns>
        public object GetService(Type type)
        {
            Func<IObjectProvider, object> registeredType;

            if (!_RegisteredTypeDic.TryGetValue(type, out registeredType))
            {
                return null;
            }

            return registeredType(_SingleObjectProvider);
        }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns>Scope object</returns>
        public IBaseObjectProvider CreateScope()
        {
            return new ScopeObjectProvider(_SingleObjectProvider);
        }

        /// <summary>
        /// Disposes any <see cref="IDisposable"/> objects owned by this container.
        /// </summary>
        public void Dispose()
        {
            _SingleObjectProvider.Dispose();
        }

        #region Register 相关
        /// <summary>
        /// 注册工厂函数，将调用该函数来解析指定的接口
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
            return RegisterType(@interface, GetFirstConstructorFromType(implementation));
        }

        private IRegisteredType RegisterType(Type itemType, Func<IObjectProvider, object> constructor)
        {
            void Set_DicRegisteredTypesWithValue(Func<IObjectProvider, object> func)
            {
                Console.WriteLine($"类型:{(((object)itemType != null) ? itemType.ToString() : null)},对应的构造函数:{func.Method.Name},添加到词典:");
                _RegisteredTypeDic[itemType] = func;
            }
            //return new RegisteredType(itemType, f => _registeredTypesG[itemType] = f, constructor);
            //return new RegisteredType(itemType, f => Set_DicRegisteredTypesGlobalByValue(itemType, f), constructor);
            return new RegisteredType(itemType, Set_DicRegisteredTypesWithValue, constructor);
        }
        #endregion

        #endregion

        #region 静态方法
        /// <summary>
        /// 编译一个lambda，该lambda调用给定类型的第一个构造函数来解析参数
        /// Compiles a lambda that calls the given type's first constructor resolving arguments
        /// </summary>
        /// <param name="itemType"></param>
        /// <returns></returns>
        private static Func<IObjectProvider, object> GetFirstConstructorFromType(Type itemType)
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
            var arg = Expression.Parameter(typeof(IObjectProvider));
            List<Expression> expressList = new List<Expression>();
            var pArray = constructor.GetParameters();


            foreach (var param in pArray)
            {
                object GetInstanceFromIObjProviderByType(IObjectProvider iObjProvider)
                {
                    return iObjProvider.GetService(param.ParameterType);
                }
                //var resolveFunc = new Func<IObjectProvider, object>(iobjProvider => GetInstanceFromIObjProviderByType(iobjProvider, param.ParameterType));
                var resolveFunc = new Func<IObjectProvider, object>(GetInstanceFromIObjProviderByType);
                var express = Expression.Call(Expression.Constant(resolveFunc.Target), resolveFunc.Method, arg);
                var unaryExp = Expression.Convert(express, param.ParameterType);
                expressList.Add(unaryExp);
            }
            Expression expression = Expression.New(constructor, expressList);

            //var lambda = Expression.Lambda(expression, ("constructor_") + itemType.Name, new[] { arg });
            var lambda = Expression.Lambda(expression, expression.ToString(), new[] { arg });
            var func = (Func<IObjectProvider, object>)lambda.Compile();
            return func;
        }

        #endregion


        #region Public interfaces 共3个: IScope ,IObjectProvider, IRegisteredType
        /// <summary>
        /// 表示某个时刻的范围对象的作用域
        /// </summary>
        public interface IBaseObjectProvider : IDisposable, IServiceProvider
        {
        }

        /// <summary>
        /// 给IScope对象提供生命周期管理策略
        /// 获取Object/Service
        /// </summary>
        interface IObjectProvider : IBaseObjectProvider
        {
            /// <summary>
            /// 获取或者设置一个单例对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            object GetServiceAsSingleton(Type type, Func<IObjectProvider, object> factory);

            /// <summary>
            /// 获取或设置一个作用域对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            object GetServicePerScope(Type type, Func<IObjectProvider, object> factory);
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

        #region 其他成员类型,共4个:  ObjectCacheProvider; SingleObjectProvider/ScopeObjectProvider:(ObjectCacheProvider,IObjectProvider); RegisteredType:IRegisteredType

        #region Lifetime management
        /// <summary>
        /// 提供缓存逻辑,添加或者获取,abstract类
        /// </summary>
        abstract class ObjectCacheProvider
        {
            /// <summary>
            /// 对象实例的缓存
            /// </summary>
            public readonly ConcurrentDictionary<Type, object> _instanceCache = new ConcurrentDictionary<Type, object>();

            /// <summary>
            /// 添加或者获取缓存对象 
            /// </summary>
            protected object GetCached(Type type, Func<IObjectProvider, object> constructor, IObjectProvider iobjProvider)
            {
                if (_instanceCache.ContainsKey(type))
                {
                    Console.WriteLine("▲ 从实例缓存字典中,获取到对象");
                }
                else
                {
                    Console.WriteLine("△ 实例缓存字典中,新增对象");
                }
                return _instanceCache.GetOrAdd(type, _ => constructor(iobjProvider));
            }

            public void Dispose()
            {
                foreach (var obj in _instanceCache.Values)
                    (obj as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// 终生容器生命周期管理, ObjectCacheProvider, IObjectProvider
        /// </summary>
        class SingleObjectProvider : ObjectCacheProvider, IObjectProvider
        {
            /// <summary>
            /// 获取给定类型中检索构造函数,由 包含它的容器提供
            /// 使用的数据集合是 Container 中的 _registeredTypeDic   
            /// 使用的 GetConstructorFromDicByType 方法,获取生成对象的构造函数
            /// </summary>
            public Func<Type, Func<IObjectProvider, object>> GetConstructorByType { get; private set; }

            public SingleObjectProvider(Func<Type, Func<IObjectProvider, object>> getFactory)
            {
                GetConstructorByType = getFactory;
            }

            public object GetService(Type type)
            {
                Func<IObjectProvider, object> factory = GetConstructorByType(type);
                return factory(this);
            }

            /// <summary>
            /// 单例模式 获取缓存对象
            /// </summary>
            /// <param name="type"></param>
            /// <param name="constructor"></param>
            /// <returns></returns>
            public object GetServiceAsSingleton(Type type, Func<IObjectProvider, object> constructor)
            {
                return GetCached(type, constructor, this);
            }

            /// <summary>
            /// // 获取作用域内的缓存对象,在容器的级别范围内,对象是单例的
            /// At container level, per-scope items are equivalent to singletons
            /// </summary>
            /// <param name="type"></param>
            /// <param name="factory"></param>
            /// <returns></returns>
            public object GetServicePerScope(Type type, Func<IObjectProvider, object> factory)
            {
                return GetServiceAsSingleton(type, factory);
            }
        }

        /// <summary>
        /// 作用域管理
        /// </summary>
        class ScopeObjectProvider : ObjectCacheProvider, IObjectProvider
        {
            // Singletons come from parent container's lifetime
            private readonly SingleObjectProvider _parentObjectProvider;

            public ScopeObjectProvider(SingleObjectProvider parentObjectProvider)
            {
                _parentObjectProvider = parentObjectProvider;
            }

            public object GetService(Type type)
            {
                return _parentObjectProvider.GetConstructorByType(type)(this);
            }

            // 单例解决方案委托给父辈的生命周期
            public object GetServiceAsSingleton(Type type, Func<IObjectProvider, object> factory)
            {
                return _parentObjectProvider.GetServiceAsSingleton(type, factory);
            }

            // Per-scope objects get cached
            public object GetServicePerScope(Type type, Func<IObjectProvider, object> factory)
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

            private readonly Func<IObjectProvider, object> _constructor;

            /// <summary>
            /// Set_DicRegisteredTypesWithValue, object表示的是构造函数
            /// </summary>
            private readonly Action<Func<IObjectProvider, object>> _Set_DicRegisteredTypesWithValue;

            public RegisteredType(Type itemType, Action<Func<IObjectProvider, object>> registerFactory, Func<IObjectProvider, object> constructor)
            {
                _itemType = itemType;
                _constructor = constructor;
                _Set_DicRegisteredTypesWithValue = registerFactory;
                //注册到 Container.DicRegisteredTypesGlobal
                registerFactory(_constructor);
            }

            public void AsSingleton()
            {
                _Set_DicRegisteredTypesWithValue(GetObjAsSingleton);
            }
            /// <summary>
            /// Func 
            /// </summary>
            /// <param name="ltime"></param>
            /// <returns></returns>
            object GetObjAsSingleton(IObjectProvider ltime)
            {
                return ltime.GetServiceAsSingleton(_itemType, _constructor);
            }

            public void AsScope()
            {
                //_registerFactory(lifetime => lifetime.GetServicePerScope(_itemType, _constructor));
                _Set_DicRegisteredTypesWithValue(GetObjPerScope);
            }

            /// <summary>
            /// Func 
            /// </summary>
            /// <param name="iprovider"></param>
            /// <returns></returns>
            object GetObjPerScope(IObjectProvider iprovider)
            {
                return iprovider.GetServicePerScope(_itemType, _constructor);
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
        public static T Resolve<T>(this Container.IBaseObjectProvider scope)
        {
            return (T)scope.GetService(typeof(T));
        }
    }
}
