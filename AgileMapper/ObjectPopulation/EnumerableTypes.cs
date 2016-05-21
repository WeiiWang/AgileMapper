namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;

    internal static class EnumerableTypes
    {
        private static readonly EnumerableType[] _enumerableTypes =
        {
            new EnumerableType(td => td.IsArray, td => td.ListType, NewList),
            new EnumerableType(td => td.IsList, td => td.ListType, ExistingObjectOrNewList),
            new EnumerableType(td => td.IsCollection, td => td.CollectionType, NewCollection),
            new EnumerableType(td => true, td => td.CollectionInterfaceType, NewList)
        };

        private static Expression ExistingObjectOrNewList(EnumerableTypeData typeData, IObjectMappingContext omc)
            => Expression.Coalesce(omc.ExistingObject, Expression.New(typeData.ListType));

        private static Expression NewCollection(EnumerableTypeData typeData, IObjectMappingContext omc)
        {
            var existingCollectionOrNew = Expression.Condition(
                omc.ExistingObject.GetIsNotDefaultComparison(),
                omc.ExistingObject,
                Expression.New(typeData.CollectionType));

            return existingCollectionOrNew;
        }

        private static Expression NewList(EnumerableTypeData typeData, IObjectMappingContext omc)
        {
            var listConstructor = typeData.ListType.GetConstructor(new[] { typeData.EnumerableInterfaceType });

            var typedEmptyEnumerableMethod = typeof(Enumerable)
                .GetMethod("Empty", Constants.PublicStatic)
                .MakeGenericMethod(typeData.ElementType);

            var existingEnumerableOrEmpty = Expression.Coalesce(
                omc.ExistingObject,
                Expression.Call(typedEmptyEnumerableMethod));

            // ReSharper disable once AssignNullToNotNullAttribute
            return Expression.New(listConstructor, existingEnumerableOrEmpty);
        }

        public static Type GetEnumerableVariableType<TEnumerable>()
        {
            var typeData = new EnumerableTypeData(typeof(TEnumerable));

            return _enumerableTypes
                .First(et => et.IsFor(typeData))
                .GetInstanceVariableType(typeData);
        }

        public static Expression GetEnumerableVariableValue(IObjectMappingContext omc)
        {
            var typeData = new EnumerableTypeData(omc.ExistingObject.Type);

            return _enumerableTypes
                .First(et => et.IsFor(typeData))
                .GetInstanceCreation(typeData, omc);
        }

        private class EnumerableTypeData
        {
            private readonly Type _enumerableType;
            private Type _listType;
            private Type _collectionType;
            private Type _collectionInterfaceType;
            private Type _enumerableInterfaceType;

            public EnumerableTypeData(Type enumerableType)
            {
                _enumerableType = enumerableType;
                ElementType = enumerableType.GetEnumerableElementType();
            }

            public bool IsArray => _enumerableType.IsArray;

            public bool IsList => ListType.IsAssignableFrom(_enumerableType);

            public bool IsCollection => CollectionType.IsAssignableFrom(_enumerableType);

            public Type ElementType { get; }

            public Type ListType => GetEnumerableType(ref _listType, typeof(List<>));

            public Type CollectionType => GetEnumerableType(ref _collectionType, typeof(Collection<>));

            public Type CollectionInterfaceType => GetEnumerableType(ref _collectionInterfaceType, typeof(ICollection<>));

            public Type EnumerableInterfaceType => GetEnumerableType(ref _enumerableInterfaceType, typeof(IEnumerable<>));

            private Type GetEnumerableType(ref Type typeField, Type openGenericEnumerableType)
                => typeField ?? (typeField = openGenericEnumerableType.MakeGenericType(ElementType));
        }

        private class EnumerableType
        {
            private readonly Func<EnumerableTypeData, bool> _typeMatcher;
            private readonly Func<EnumerableTypeData, Type> _instanceVariableTypeFactory;
            private readonly Func<EnumerableTypeData, IObjectMappingContext, Expression> _instanceCreationFactory;

            public EnumerableType(
                Func<EnumerableTypeData, bool> typeMatcher,
                Func<EnumerableTypeData, Type> instanceVariableTypeFactory,
                Func<EnumerableTypeData, IObjectMappingContext, Expression> instanceCreationFactory)
            {
                _typeMatcher = typeMatcher;
                _instanceVariableTypeFactory = instanceVariableTypeFactory;
                _instanceCreationFactory = instanceCreationFactory;
            }

            public bool IsFor(EnumerableTypeData typeData) => _typeMatcher.Invoke(typeData);

            public Type GetInstanceVariableType(EnumerableTypeData typeData)
                => _instanceVariableTypeFactory.Invoke(typeData);

            public Expression GetInstanceCreation(EnumerableTypeData typeData, IObjectMappingContext omc)
                => _instanceCreationFactory.Invoke(typeData, omc);
        }
    }
}