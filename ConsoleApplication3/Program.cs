using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var a1 = new A("1", "1");
            var a2 = new A("1", "1");
            var ret = a1.Equals(a2);
            var ret2 = a1.GetHashCode() == a2.GetHashCode();
            var tryres1 = new Try<int>(() =>
            {
                return 1;
            });
            var tryres2 = new Try<int>(() =>
            {
                throw new Exception("test");
                return 1;
            });
            var e1 = new Either<int, string>("string");
            var t1 = -1;
            var t2 = "";
            if (e1.isLeft)
                t1 = e1.Left;
            else t2 = e1.Right;

            var t3 = new List<Option<string>>() {
                Option.Some("StringOption"),
                Option.None<string>(),
                new Some<string>("Bla"),
                Option.None<string>() };
            var t4 = t3.Select(x => x.Map<int>(y => y.Length));
            foreach (var opt in t4)
            {
                Console.Write(opt.isDefinded + " ");
                if (opt.isDefinded)
                    Console.Write(opt.get);
                Console.WriteLine();
            }
            var t5 = t4.Where(x => x.isDefinded);
            var t6 = new Match<string, int>("abcdef")
                    .Case(x => x.Length == 10).Do(x => 10)
                    .Case(x => x.Length == 6).Do(x => 6)
                    .Default(x => -1);

            var t7 = t3.flattenOpt();
            var t8 = new Match<object, Boolean>((object)a1)
                .CaseDo<A>(x => {
                    Console.WriteLine(x.Name);
                    return true;
                }).Default(x => false);

            Console.ReadKey();
        }
    }



    class A : Immutable<A>
    {
        public string Name { get; }
        public string LastName { get; }

        public A()
        {
            throw new Exception("don't use empty constructor of immutable class");
        }
        public A(string Name, string LastName)
        {
            this.Name = Name;
            this.LastName = LastName;
        }
    }
}
