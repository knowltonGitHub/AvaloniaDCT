using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            UDPSocket _s = new UDPSocket();
            _s.Server("127.0.0.1", 27000);

            Console.ReadKey();
        }
    }
}
