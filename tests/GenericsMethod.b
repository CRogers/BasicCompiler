using System;

namespace G {
	class Box`[T] {
		private var item: T;

		public static new(item: T):Box`[T] {
			var b = ctor();
			b.item = item;
			return b;
		}

        public getItem():T {
            return item;
        }

		public id`[A](a: A, i: Int32):T {
			return item;
		}

		public id`[A](a: A, i: Int8):T {
			return item;
		}

		public id`[A,B](a: A, a:B):T {
			return item;
		}

        public unboxIt`[A](box: Box`[A]):A {
            return box.getItem();
        }
	}

	class Program {
		public static main() {
			var b = Box`[Int32].new(10);
			var r = Box`[Bool].new(false).unboxIt`[Int32](b);
            Console.println(r);
		}
	}
}


/*[[[
10

]]]*/