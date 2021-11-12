using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Features;

namespace TS.StandaloneKestrelServer
{
    /// <summary>
    /// Reimplementation of <see cref="Microsoft.AspNetCore.Http.Features.FeatureCollection"/> with some minor changes.
    /// Licensed under MIT License: https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt
    /// </summary>
    public class PersistentContainer : IFeatureCollection
    {
        public bool IsReadOnly => false;
        
        public int Revision => 1; //TODO: Maybe implement it? 

        private IDictionary<Type, Object> _container;

        public PersistentContainer()
        {
            _container = new Dictionary<Type, object>();
        }

#nullable enable
        public object? this[Type key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                return (_container.TryGetValue(key, out var result)) ? result : null;
            }
            set
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (value == null)
                {
                    _container.Remove(key);
                }
                else
                {
                    _container[key] = value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return _container.GetEnumerator();
        }

#pragma warning disable 8766
        public TFeature? Get<TFeature>() => (TFeature?) this[typeof(TFeature)];
#pragma warning restore 8766

        public TFeature? Get<TFeature>(out TFeature? result) => result = (TFeature?) this[typeof(TFeature)];

        public void Set<TFeature>(TFeature? instance) => this[typeof(TFeature)] = instance;
#nullable disable
    }
}