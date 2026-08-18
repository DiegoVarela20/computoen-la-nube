using System;
using System.Threading;

namespace FilosofosComensales
{

    class Program
    {
        const int N = 5;

        static void Main(string[] args)
        {
            Tenedor[] tenedores = new Tenedor[N];
            Filosofo[] filosofos = new Filosofo[N];

            for (int i = 0; i < N; i++)
                tenedores[i] = new Tenedor(i);


            for (int i = 0; i < N; i++)
                filosofos[i] = new Filosofo(i, N, tenedores, filosofos);

            foreach (var f in filosofos) f.Iniciar();
            foreach (var f in filosofos) f.Esperar();

            Console.WriteLine("\nResumen final:");
            for (int i = 0; i < N; i++)
                Console.WriteLine($"  Filosofo {i}: comió {filosofos[i].VecesComido} veces.");

        }
    }


    class Filosofo
    {
        private static readonly object _mesa = new object();

        private readonly int _id;
        private readonly int _n;
        private readonly Tenedor[] _tenedores;
        private readonly Filosofo[] _filosofos;
        private readonly Thread _hilo;
        private readonly Random _rnd;

        private int _tiempoRestante;
        private int _prioridadActual;

        private const int TiempoInicial = 40;
        private const int Descuento = 5;
        private const int RandomMax = 3;

        public Filosofo(int id, int n, Tenedor[] tenedores, Filosofo[] filosofos)
        {
            _id = id;
            _n = n;
            _tenedores = tenedores;
            _filosofos = filosofos;
            _rnd = new Random(id * 7919 + Environment.TickCount);

            _tiempoRestante = TiempoInicial - _rnd.Next(0, RandomMax);
            _prioridadActual = _tiempoRestante;

            _hilo = new Thread(Vivir);
        }

        public bool Activo => _tiempoRestante > 0;
        public int Prioridad => _prioridadActual;
        public int VecesComido { get; private set; }

        private int IzqTenedor => _id;
        private int DerTenedor => (_id + 1) % _n;
        private int VecinoIzq  => (_id - 1 + _n) % _n;
        private int VecinoDer  => (_id + 1) % _n;

        public void Iniciar() => _hilo.Start();
        public void Esperar() => _hilo.Join();

        private void Vivir()
        {
            while (Activo)
            {
                Pensar();

                bool comio = false;

                lock (_mesa)
                {
                    _prioridadActual = _tiempoRestante + _rnd.Next(0, RandomMax);

                    Tenedor izq = _tenedores[IzqTenedor];
                    Tenedor der = _tenedores[DerTenedor];

                    if (izq.Libre && der.Libre && TengoMayorPrioridad())
                    {
                        izq.Tomar(_id);
                        der.Tomar(_id);
                        _tiempoRestante -= Descuento;
                        if (_tiempoRestante < 0) _tiempoRestante = 0;
                        comio = true;
                    }
                }

                if (comio)
                {
                    Comer();
                    VecesComido++;
                    _tenedores[DerTenedor].Soltar();
                    _tenedores[IzqTenedor].Soltar();
                }
                else
                {
                    Thread.Sleep(200);
                }
            }

            Console.WriteLine($"Filosofo {_id} agotó su tiempo (0 seg) y se retira.");
        }

        private bool TengoMayorPrioridad()
        {
            Filosofo vi = _filosofos[VecinoIzq];
            Filosofo vd = _filosofos[VecinoDer];

            if (vi.Activo && vi.Prioridad > _prioridadActual) return false;
            if (vd.Activo && vd.Prioridad > _prioridadActual) return false;
            return true;
        }

        private void Pensar()
        {
            Console.WriteLine($"Filosofo {_id} está pensando.  (tiempo: {_tiempoRestante}s)");
            Thread.Sleep(_rnd.Next(300, 800));
        }

        private void Comer()
        {
            Console.WriteLine($"Filosofo {_id} tomó tenedores {IzqTenedor} y {DerTenedor} -> COMIENDO. " +
                              $"(tiempo restante: {_tiempoRestante}s)");
            Thread.Sleep(500);
        }
    }

   
    class Tenedor
    {
        private readonly object _candado = new object();
        private int _dueno = -1;

        public Tenedor(int id) => Id = id;
        public int Id { get; }

        public bool Libre { get { lock (_candado) return _dueno == -1; } }
        public void Tomar(int id) { lock (_candado) _dueno = id; }
        public void Soltar() { lock (_candado) _dueno = -1; }
    }
}