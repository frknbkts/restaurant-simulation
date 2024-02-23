using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Yazlab13
{
    public class OptionalSemaphore
    {
        private readonly SemaphoreSlim? semaphore;

        public OptionalSemaphore(int count)
        {
            if(count > 0)
            {
                semaphore = new SemaphoreSlim(count);
            }
        }

        public void Wait()
        {
            if(semaphore != null)
            {
                semaphore.Wait();
            }
        }

        public bool Wait(int millisecondsTimeout)
        {
            return semaphore != null ? semaphore.Wait(millisecondsTimeout) : false;
        }

        public int Release()
        {
            return semaphore != null ? semaphore.Release() : 0;
        }
    }
    public enum ClientStateEnum
    {
        WAITING_TABLE,
        WAITING_WAITER,
        WAITING_ORDER,
        EATING,
        WAITING_CASHIER,
        LEFT
    }

    public class ClientState
    {
        public ClientStateEnum value;
        public ClientState(ClientStateEnum value) {
            this.value = value;
        }

        override public string ToString()
        {
            switch(value)
            {
                case ClientStateEnum.WAITING_TABLE:
                    return "Masa bekliyor";
                case ClientStateEnum.WAITING_WAITER:
                    return "Garson bekliyor";
                case ClientStateEnum.WAITING_ORDER:
                    return "Sipariş bekliyor";
                case ClientStateEnum.EATING:
                    return "Yemek yiyor";
                case ClientStateEnum.WAITING_CASHIER:
                    return "Kasada belkiyor";
                case ClientStateEnum.LEFT:
                    return "Ayrıldı";
                default:
                    return "";
            }
        }
    }
    public class Client
    {
        public ClientState state { get; set; }

        public int priority { get; set; }

        public string priorityString
        {
            get
            {
                if (priority == 0) return "Evet";
                else return "Hayır";
            }
        }
        public Client(int priority) {
            this.priority = priority;
            this.state = new ClientState(ClientStateEnum.WAITING_TABLE);
        }

        //public string describePriority()
        //{
        //    if (priority == 0) return "Evet";
        //    else return "Hayır";
        //}
    }
 
    class ProcessingStopped : Exception
    {

    }

    class SimulationResult
    {
        public int profit { get; set; }
        public int tableNum { get; set; }
        public int waiterNum { get; set; }
        public int cookNum { get; set; }
        public int clientNum { get; set; }

        public SimulationResult(int profit, int tableNum, int waiterNum, int cookNum, int clientNum)
        {
            this.profit = profit;
            this.tableNum = tableNum;
            this.waiterNum = waiterNum;
            this.cookNum = cookNum;
            this.clientNum = clientNum;
        }
    }

    class Simulator
    {
        public int steps;
        public int tableNum;
        public int waiterNum;
        public int cookNum;
        public int cashierNum;
        public int totalClients;
        PriorityQueue<Client, int> clients;
        public List<Client> processedClients;
        public OptionalSemaphore tables;
        public OptionalSemaphore waiters;
        public OptionalSemaphore cooks;
        public OptionalSemaphore cashiers;
        object stepCondition;
        bool finished;
        //Thread processingThread;
        List<Thread> threads;
        ClientAdder clientAdder;
        static Random random = new Random();
        Dictionary<int, int> stepWaiters;

        public delegate (int, int) ClientAdder(Simulator simulator);

        public Simulator(int tableNum, int waiterNum, int cookNum, int cashierNum, ClientAdder? clientAdder) { 
            this.tableNum = tableNum;
            this.waiterNum =   waiterNum;
            this.cookNum = cookNum;
            this.cashierNum = cashierNum;
            this.clientAdder = clientAdder != null ? clientAdder : RandomFlowClientAdder;
            totalClients = 0;
            clients = new PriorityQueue<Client, int>();
            processedClients = new List<Client>();
            tables = new OptionalSemaphore(tableNum);
            waiters = new OptionalSemaphore(waiterNum);
            cooks = new OptionalSemaphore(cookNum * 2);
            cashiers = new OptionalSemaphore(cashierNum);
            stepCondition = new object();
            finished = false;
            stepWaiters = new Dictionary<int, int>();
            //processingThread = new Thread(ProcessClients);
            //processingThread.IsBackground = true;
            //processingThread.Start();
            threads = new List<Thread>();
        }

        public static (int, int) RandomFlowClientAdder(Simulator simulator)
        {
            int min = simulator.steps > 0 ? 0 : 1;
            int clientNum = random.Next(min, 11);
            int vipClientNum = clientNum > 0 ? random.Next(0, clientNum) : 0;

            return (clientNum - vipClientNum, vipClientNum);
        }

        //private void ProcessClients()
        //{
        //    while(!finished || clients.Count != 0)
        //    {
        //        try
        //        {
        //            Client client = clients.Dequeue();
        //            if (client != null)
        //            {
        //                Trace.WriteLine("client {}", client.ToString());
        //                processedClients.Add(client);
        //                var thread = new Thread(Run);
        //                thread.IsBackground = true;
        //                thread.Start(client);
        //                threads.Add(thread);
        //            }
        //        } catch (Exception e)
        //        {
        //            Thread.Sleep(100);
        //        }

        //    }
        //}

        private void BlockUntilStep()
        {
            int steps = this.steps;
            int n = stepWaiters.GetValueOrDefault(steps, 1);
            stepWaiters[steps] = n;


            while (true) {
                if(Monitor.Wait(stepCondition, 100))
                {
                    stepWaiters[steps] -= 1;
                    return;
                }
                if (finished) {
                    stepWaiters[steps] -= 1;
                    throw new ProcessingStopped();
                }
            }
        }

        public void WaitSteps(int count) { 
            for(int i = 0; i < count; i++)
            {
                lock (stepCondition) {
                    BlockUntilStep();
                }
            } 
        }

        private void TakeTable(Client client) {
            for (int i = 0; i < 20; i++) {
                WaitSteps(1);
                if(tables.Wait(10))
                {
                    client.state.value = ClientStateEnum.WAITING_WAITER;
                    return;
                }
            }
            client.state.value = ClientStateEnum.LEFT;
            throw new ProcessingStopped();
        }

        private void CallWaiter(Client client) {
            waiters.Wait();
            WaitSteps(2);
            client.state.value = ClientStateEnum.WAITING_ORDER;
            waiters.Release();
        }

        private void WaitForOrder(Client client) { 
            cooks.Wait();
            WaitSteps(3);
            client.state.value = ClientStateEnum.EATING;
            cooks.Release();
        }
        
        private void Eat(Client client)
        {
            WaitSteps(3);
            client.state.value = ClientStateEnum.WAITING_CASHIER;
            tables.Release();
        }

        private void Pay(Client client) {
            cashiers.Wait();
            WaitSteps(1);
            client.state.value = ClientStateEnum.LEFT;
            cashiers.Release();
        }


        private void Run(object? obj)
        {
            Client client = (Client)obj;
            try {
                TakeTable(client);
                CallWaiter(client);
                WaitForOrder(client);
                Eat(client);
                Pay(client);
            } catch(ProcessingStopped e)
            {

            }
        }

        private void AddClients()
        {
            var (normalClientNum, vipClientNum) = clientAdder(this);
            totalClients += normalClientNum + vipClientNum;
            List<int> priorities = new List<int>();
            for (int i = 0; i < vipClientNum; i++)
            {
                priorities.Add(0);
            }
            for (int i = 0; i < normalClientNum; i++)
            {
                priorities.Add(1);
            }
            foreach(int priority in priorities) {
                Client client = new(priority);
                clients.Enqueue(client, priority);
            }
        }

        public void Step()
        {
            lock(stepCondition)
            {
                Monitor.PulseAll(stepCondition);
            }
            AddClients();
            while (clients.Count != 0)
            {
                Client client = clients.Dequeue();
                if (client != null)
                {
                    //Trace.WriteLine("client {}", client.ToString());
                    processedClients.Add(client);
                    var thread = new Thread(Run);
                    thread.IsBackground = true;
                    thread.Start(client);
                    threads.Add(thread);
                }
            }

            if (steps > 0)
            {
                while (stepWaiters.GetValueOrDefault(steps, 0) > 0) {
                    Thread.Sleep(10);
                }
            }

            steps++;

        }

        public void Finish() {
            finished = true;
            /*processingThread.Join();*/
            foreach (Thread thread in threads) {
                thread.Join();
            }
        }

        public int Profit()
        {
            return processedClients.Count - tableNum - waiterNum - cookNum;
        }

        public static List<SimulationResult> CalculateMaxProfit(ClientAdder clientAdder, int n, int steps, Action<int> onProgress)
        {
            List<SimulationResult > simulations = new List<SimulationResult>();

            for(int tableNum = 0; tableNum < n + 1; tableNum++)
            {
                for (int waiterNum = 0; waiterNum < n + 1; waiterNum++)
                {
                    for (int cookNum = 0; cookNum < n + 1; cookNum++)
                    {
                        Simulator simulator = new Simulator(tableNum, waiterNum, cookNum, 1, clientAdder);
                        for(int i = 0; i < steps; i++)
                        {
                            simulator.Step();
                            //Thread.Sleep(100);
                        }
                        simulator.Finish();
                        simulations.Add(new SimulationResult(simulator.Profit(), simulator.tableNum, simulator.waiterNum, simulator.cookNum, simulator.processedClients.Count));
                    }
                }
                onProgress(tableNum);
            }

            return simulations;
        }

        public static void Demo()
        {
        }

        public static void main()
        {
            //Problem1();
            //Problem2();
        }
    }
}
