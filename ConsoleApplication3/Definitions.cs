using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;

namespace ConsoleApplication3
{
    class Match<T1, T2>
    {
        private T1 input { get; }
        private T2 result;
        protected bool isMatched = false;
        protected bool isComputed = false;
        public Match(T1 what, bool isMatched = false)
        {
            this.input = what;
            this.isMatched = isMatched;
        }
        public CaseClause Case(Func<T1, bool> test)
        {
            if (!this.isMatched && test(this.input))
                this.isMatched = true;
            return new CaseClause(this);
        }
        public Match<T1, T2> CaseDo<SubType>(Func<SubType, T2> action) {
            var subtype = typeof(SubType);
            if (subtype.IsInstanceOfType(input))
            {
                isMatched = true;
                result = action((SubType)Convert.ChangeType(input, subtype));
                isComputed = true;
            }
            return this;
        }
        public T2 Default(Func<T1, T2> action)
        {
            if (this.isMatched && !this.isComputed)
            {
                this.result = action(input);
                this.isComputed = true;
            }
            return this.result;
        }
        public Option<T2> Get() {
            if (isComputed)
                return new Some<T2>(result);
            else return None.Get<T2>();
        }

        public class CaseClause
        {
            private Match<T1, T2> parent;
            public CaseClause(Match<T1,T2> parent)
            {
                this.parent = parent;
            }
            public Match<T1, T2> Do(Func<T1, T2> action)
            {
                if (parent.isMatched && !parent.isComputed) {
                    parent.result = action(parent.input);
                    parent.isComputed = true;
                }
                return parent;
            }
        }
    }

    public class Either<T1, T2>
    {
        private bool leftOrRight; // false for left, true for right
        public bool isLeft { get { return !leftOrRight; } }
        public bool isRight { get { return leftOrRight; } }
        public T1 Left { get; }
        public T2 Right { get; }
        public Either(T1 left)
        {
            leftOrRight = false;
            Left = left;
        }
        public Either(T2 right)
        {
            leftOrRight = true;
            Right = right;
        }
    }

    public static class FunctionalExtensions
    {
        public static ICollection<T> flattenOpt<T>(this ICollection<Option<T>> collection)
        {
            var type = collection.GetType();
            var type2 = type.GetGenericTypeDefinition();
            var type3 = type2.MakeGenericType(typeof(T));
            ICollection<T> ret = (ICollection<T>) Activator.CreateInstance(type3);
            foreach (var el in collection)
                if (el.isDefinded)
                    ret.Add(el.get);
            return ret;
        }
    }

    public abstract class Option<T>
    {
        public virtual bool isDefinded { get; }
        public virtual bool isEmpty { get; }
        public abstract T get { get; }
        public abstract T getOrElse(T other);
        public abstract Option<T1> Map<T1>(Func<T, T1> action);
    }

    public static class None
    {
        public static Option<T> Get<T>()
        {
            return None<T>.New<T>();
        }
    }

    public class None<T> : Option<T>
    {
        private None() { }
        private static ConcurrentDictionary<Type, Object> cache
            = new ConcurrentDictionary<Type, Object>();
        public static Option<T> New<T>() {
            var key = typeof(T);
            if (!cache.ContainsKey(key)) {
                cache.TryAdd(key, (Object)new None<T>());
            }
            return (Option<T>)cache[key];
        }
        public override bool isDefinded { get { return false; } }
        public override bool isEmpty { get { return true; } }
        public override Option<T1> Map<T1>(Func<T, T1> action) { return new None<T1>(); }
        public override T getOrElse(T other) { return other; }
        public override T get
        {
            get
            {
                throw new Exception("No element, option is not defined");
            }
        }
    }

    public class Some<T> : Option<T>
    {
        public override bool isDefinded { get { return true; } }
        public override bool isEmpty { get { return false; } }
        private T data { get; }
        public Some(T data)
        {
            if (data == null)
                throw new ArgumentNullException();
            this.data = data;
        }
        public override Option<T1> Map<T1>(Func<T, T1> action)
        {
            var res = action(this.data);
            return new Some<T1>(res);
        }
        public override T getOrElse(T other) { return this.data; }
        public override T get
        {
            get
            {
                return this.data;
            }
        }
    }

    public class Try<T>
    {
        public Boolean isSuccess { get; }
        public Boolean isFailure { get; }
        private T result;
        private Exception ex;

        public Try(Func<T> action)
        {
            try
            {
                this.result = action();
                isSuccess = true;
                isFailure = false;
            }
            catch (Exception ex)
            {
                this.ex = ex;
                isSuccess = false;
                isFailure = true;
            }
        }

        public T get()
        {
            if (isSuccess)
                return result;
            else throw new ArgumentException("Try failed, cannot get result");
        }

        public Exception exception()
        {
            if (isFailure)
                return this.ex;
            else throw new ArgumentException("Try succeded, cannot get exception");
        }

        public Try<T1> OnSuccess<T1>(Func<T, T1> action)
        {
            Func<T1> func = () => action(this.result);
            return new Try<T1>(func);
        }

        public Try<T1> OnFailure<T1>(Func<Exception, T1> action)
        {
            Func<T1> func = () => action(this.ex);
            return new Try<T1>(func);
        }
    }

    public class Immutable<T> // where T : new()
    {
        private static ConcurrentDictionary<string, PropertyInfo[]> properitesCache 
            = new ConcurrentDictionary<string, PropertyInfo[]>();

        public Immutable()
        {
            var thisType = this.GetType();
            PropertyInfo[] props = null;
            properitesCache.TryGetValue(thisType.FullName, out props);
            if (props == null)
            {
                var constructors = thisType.GetConstructors();
                var largestConstructorParams = constructors
                    .Select(x => x.GetParameters())
                    .OrderByDescending(x => x.Length)
                    .First();
                props = thisType.GetProperties();
                properitesCache[thisType.FullName] = props;
                for (var i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (!p.CanRead || p.CanWrite)
                        throw new FieldAccessException(
                                "Properties in CaseClasses should not have setter property name: " +
                                p.Name + " of type: " + p.PropertyType + " in class: " + thisType.FullName);
                    if (p.PropertyType != largestConstructorParams[i].ParameterType)
                        throw new ArgumentException("Constructor parameters for immutable type should have same types as class properties, propertyType: " + p.PropertyType +
                            " propertyName " + p.Name);
                    if (p.Name != largestConstructorParams[i].Name)
                        throw new ArgumentException("Constructor parameters for immutable type should have same names as class properties, propertyType: " + p.PropertyType +
                            " propertyName: " + p.Name);

                }
            }
        }

        public bool Equals(T that)
        {
            var ret = true;
            foreach (var prop in properitesCache[this.GetType().FullName])
                if (prop.GetValue(this) != prop.GetValue(that))
                {
                    ret = false;
                    break;
                }
            return ret;
        }

        public override bool Equals(object that)
        {
            if(that.GetType() != this.GetType())
                throw new InvalidOperationException("Don't use Equals with objects of other types");
            return this.Equals((T)that);
        }

        public override int GetHashCode()
        {
            var ret = 0;
            foreach (var prop in properitesCache[this.GetType().FullName])
                ret += prop.GetValue(this).GetHashCode();
            return ret;
        }
    }
}
