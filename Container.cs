﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.MinIoC
{

    /// <summary>
    /// 控制容器的反转处理已注册类型的依赖项注入 1
    /// Inversion of control container handles dependency injection for registered types
    /// </summary>
    public class Container : Container.IScope
    {
        #region Public interfaces
        /// <summary>
        /// 表示某个时刻的范围对象的作用域
        /// </summary>
        public interface IScope : IDisposable, IServiceProvider
        {
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
            void PerScope();
        }
        #endregion

        /// <summary>
        /// 已注册类型的映射
        /// </summary>
        private readonly Dictionary<Type, Func<ILifetime, object>> _registeredTypes = new Dictionary<Type, Func<ILifetime, object>>();

        /// <summary>
        /// 生命周期管理
        /// </summary>
        private readonly ContainerLifetime _lifetime;

        /// <summary>
        /// Creates a new instance of IoC Container
        /// </summary>
        public Container()
        {
            _lifetime = new ContainerLifetime(GetRegisteredTypes);
        }

        Func<ILifetime, object> GetRegisteredTypes(Type t)
        {
            Console.WriteLine("aa" + (((object)t != null) ? t.ToString() : null));
            return _registeredTypes[t];
        }

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

        private IRegisteredType RegisterType(Type itemType, Func<ILifetime, object> factory)
        {
            return new RegisteredType(itemType, f => _registeredTypes[itemType] = f, factory);
        }

        /// <summary>
        /// Returns the object registered for the given type, if registered
        /// </summary>
        /// <param name="type">Type as registered with the container</param>
        /// <returns>Instance of the registered type, if registered; otherwise <see langword="null"/></returns>
        public object GetService(Type type)
        {
            Func<ILifetime, object> registeredType;

            if (!_registeredTypes.TryGetValue(type, out registeredType))
            {
                return null;
            }

            return registeredType(_lifetime);
        }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns>Scope object</returns>
        public IScope CreateScope() => new ScopeLifetimeMgr(_lifetime);

        /// <summary>
        /// Disposes any <see cref="IDisposable"/> objects owned by this container.
        /// </summary>
        public void Dispose() => _lifetime.Dispose();

        #region Lifetime management
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
            protected object GetCached(Type type, Func<ILifetime, object> factory, ILifetime lifetime)
            {
                return _instanceCache.GetOrAdd(type, _ => factory(lifetime));
            }

            public void Dispose()
            {
                foreach (var obj in _instanceCache.Values)
                    (obj as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// 容器寿命管理
        /// </summary>
        class ContainerLifetime : ObjectCache, ILifetime
        {
            /// <summary>
            /// 获取给定类型中检索构造函数,由 包含它的容器提供
            /// 使用的数据集合是 Container 中的 _registeredTypes
            /// </summary>
            public Func<Type, Func<ILifetime, object>> GetFactory { get; private set; }

            public ContainerLifetime(Func<Type, Func<ILifetime, object>> getFactory)
            {
                GetFactory = getFactory;
            }

            public object GetService(Type type)
            {
                Func<ILifetime, object> factory = GetFactory(type);
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
                return _parentLifetime.GetFactory(type)(this);
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

        #region Container items
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
            return (Func<ILifetime, object>)Expression.Lambda(
                Expression.New(constructor, constructor.GetParameters().Select(
                    param =>
                    {
                        var resolve = new Func<ILifetime, object>(
                            lifetime => lifetime.GetService(param.ParameterType));
                        return Expression.Convert(
                            Expression.Call(Expression.Constant(resolve.Target), resolve.Method, arg),
                            param.ParameterType);
                    })),
                arg).Compile();
        }

        // RegisteredType is supposed to be a short lived object tying an item to its container
        // and allowing users to mark it as a singleton or per-scope item
        class RegisteredType : IRegisteredType
        {
            private readonly Type _itemType;
            private readonly Action<Func<ILifetime, object>> _registerFactory;
            private readonly Func<ILifetime, object> _factory;

            public RegisteredType(Type itemType, Action<Func<ILifetime, object>> registerFactory, Func<ILifetime, object> factory)
            {
                _itemType = itemType;
                _registerFactory = registerFactory;
                _factory = factory;

                registerFactory(_factory);
            }

            public void AsSingleton()
                => _registerFactory(lifetime => lifetime.GetServiceAsSingleton(_itemType, _factory));

            public void PerScope()
                => _registerFactory(lifetime => lifetime.GetServicePerScope(_itemType, _factory));
        }
        #endregion
    }

    /// <summary>
    /// 容器的扩展方法
    /// </summary>
    static class ContainerExtensions
    {
        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <typeparam name="T">Interface to register</typeparam>
        /// <param name="container">This container instance</param>
        /// <param name="type">Implementing type</param>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<T>(this Container container, Type type)
        {
            return container.Register(typeof(T), type);
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
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
        /// Registers a factory function which will be called to resolve the specified interface
        /// </summary>
        /// <typeparam name="T">Interface to register</typeparam>
        /// <param name="container">This container instance</param>
        /// <param name="factory">Factory method</param>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<T>(this Container container, Func<T> factory)
        {
            return container.Register(typeof(T), () => factory());
        }

        /// <summary>
        /// 注册一个类型
        /// </summary>
        /// <param name="container">This container instance</param>
        /// <typeparam name="T">Type to register</typeparam>
        /// <returns>IRegisteredType object</returns>
        public static Container.IRegisteredType Register<T>(this Container container)
        {
            return container.Register(typeof(T), typeof(T));
        }

        /// <summary>
        /// 返回指定接口的实现(类)
        /// </summary>
        /// <typeparam name="T">Interface type</typeparam>
        /// <param name="scope">This scope instance</param>
        /// <returns>Object implementing the interface</returns>
        public static T Resolve<T>(this Container.IScope scope)
        {
            return (T)scope.GetService(typeof(T));
        }
    }
}