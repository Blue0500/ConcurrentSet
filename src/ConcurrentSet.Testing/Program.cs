using JoshuaKearney;
using JoshuaKearney.Collections;
using System;
using System.Collections.Generic;

namespace ConcurrentSet.Testing {
    class Program {
        static void Main(string[] args) {
            ConcurrentSet<int> set = new ConcurrentSet<int>() {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9
            };

            var other = new[] { 4, 5, 6, 7, 99, 5 };

            set.SymmetricExceptWith(other);
            Console.WriteLine(string.Join(", ", set));
            Console.Read();
        }
    }
}