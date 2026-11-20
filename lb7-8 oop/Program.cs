using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PraktDesignPatterns
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            DemoTask1(); DemoTask2(); DemoTask3(); DemoTask4(); DemoTask5();
        }

        // ЗАВДАННЯ 1: Singleton (AppConfig) і Factory Method (NotifierCreator)
        private static void DemoTask1()
        {
            Console.WriteLine("=== ЗАВДАННЯ 1: Singleton і Factory Method ===");
            var instances = new AppConfig[6];
            var threads = new Thread[6];
            for (int i = 0; i < 6; i++) { int idx = i; threads[i] = new Thread(() => instances[idx] = AppConfig.Instance); threads[i].Start(); }
            foreach (var t in threads) t.Join();
            bool allSame = Array.TrueForAll(instances, x => ReferenceEquals(x, instances[0]));
            Console.WriteLine($"Singleton створено 1 раз, усі 6 потоків отримали той самий екземпляр: {allSame} (Env={AppConfig.Instance.Environment})");

            NotifierCreator[] creators = { new EmailNotifierCreator(), new SmsNotifierCreator(), new PushNotifierCreator() };
            foreach (var c in creators) c.Notify("Данило", "Шаблони готові");
            Console.WriteLine();
        }

        // ЗАВДАННЯ 2: Adapter (IPaymentGateway -> LegacyCardProcessor)
        private static void DemoTask2()
        {
            Console.WriteLine("=== ЗАВДАННЯ 2: Adapter ===");
            IPaymentGateway gateway = new CardGatewayAdapter(new LegacyCardProcessor());
            Console.WriteLine($"Оплата 250.50 грн пройшла: {gateway.Pay(250.50m)}");
            Console.WriteLine($"Оплата -10 грн пройшла: {gateway.Pay(-10m)}\n");
        }

        // ЗАВДАННЯ 3: Decorator (різні порядки обгортання ITaskProcessor)
        private static void DemoTask3()
        {
            Console.WriteLine("=== ЗАВДАННЯ 3: Decorator ===");
            ITaskProcessor p1 = new LoggingDecorator(new TimingDecorator(new RetryDecorator(new CoreTaskProcessor(), 2)), "OrderA");
            ITaskProcessor p2 = new LoggingDecorator(new RetryDecorator(new TimingDecorator(new CoreTaskProcessor()), 2), "OrderB");
            ITaskProcessor p3 = new TimingDecorator(new LoggingDecorator(new CoreTaskProcessor(), "OrderC"));
            p1.Process("Запит-1"); p2.Process("Запит-2"); p3.Process("Запит-3");
            Console.WriteLine();
        }

        // ЗАВДАННЯ 4: Observer (підписка, відписка, ізоляція винятків)
        private static void DemoTask4()
        {
            Console.WriteLine("=== ЗАВДАННЯ 4: Observer ===");
            var tracker = new OrderTracker();
            IOrderObserver logSub = new LogObserver(), brokenSub = new BrokenObserver(), auditSub = new AuditObserver();
            tracker.Subscribe(logSub); tracker.Subscribe(brokenSub); tracker.Subscribe(auditSub);
            tracker.SetStatus("Оплачено");
            tracker.Unsubscribe(brokenSub);
            Console.WriteLine("Після відписки BrokenObserver:");
            tracker.SetStatus("Відправлено");
            Console.WriteLine();
        }

        // ЗАВДАННЯ 5 + інтеграція з Factory Method і Observer
        private static void DemoTask5()
        {
            Console.WriteLine("=== ЗАВДАННЯ 5: Strategy ===");
            var checkout = new CheckoutContext(new NoDiscountStrategy());
            Console.WriteLine($"Звичайний клієнт (1000 грн): {checkout.CalculateTotal(1000m)} грн");
            checkout.SetStrategy(new VipDiscountStrategy());
            Console.WriteLine($"VIP-клієнт (1000 грн): {checkout.CalculateTotal(1000m)} грн");
            checkout.SetStrategy(new PromoDiscountStrategy(0.15m));
            Console.WriteLine($"Промокод 15% (1000 грн): {checkout.CalculateTotal(1000m)} грн\n");
        }
    }

    // --- Завдання 1: Singleton & Factory Method ---
    public sealed class AppConfig
    {
        private static readonly Lazy<AppConfig> _lazy = new(() => new AppConfig(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static AppConfig Instance => _lazy.Value;
        public string Environment { get; } = "Production";
        private AppConfig() { }
    }
    public interface INotifier { void Send(string to, string msg); }
    public sealed class EmailNotifier : INotifier { public void Send(string to, string msg) => Console.WriteLine($"[Email -> {to}] {msg}"); }
    public sealed class SmsNotifier : INotifier { public void Send(string to, string msg) => Console.WriteLine($"[SMS -> {to}] {msg}"); }
    public sealed class PushNotifier : INotifier { public void Send(string to, string msg) => Console.WriteLine($"[Push -> {to}] {msg}"); }
    public abstract class NotifierCreator { public abstract INotifier CreateNotifier(); public void Notify(string to, string msg) => CreateNotifier().Send(to, msg); }
    public sealed class EmailNotifierCreator : NotifierCreator { public override INotifier CreateNotifier() => new EmailNotifier(); }
    public sealed class SmsNotifierCreator : NotifierCreator { public override INotifier CreateNotifier() => new SmsNotifier(); }
    public sealed class PushNotifierCreator : NotifierCreator { public override INotifier CreateNotifier() => new PushNotifier(); }

    // --- Завдання 2: Adapter ---
    public interface IPaymentGateway { bool Pay(decimal amountUah); }
    public sealed class LegacyCardProcessor { public string ChargeCents(int cents) => cents > 0 ? $"OK:{cents}" : "ERR:AMOUNT"; }
    public sealed class CardGatewayAdapter : IPaymentGateway
    {
        private readonly LegacyCardProcessor _legacy;
        public CardGatewayAdapter(LegacyCardProcessor legacy) => _legacy = legacy;
        public bool Pay(decimal amountUah) => _legacy.ChargeCents((int)Math.Round(amountUah * 100m)).StartsWith("OK:");
    }

    // --- Завдання 3: Decorator ---
    public interface ITaskProcessor { void Process(string payload); }
    public sealed class CoreTaskProcessor : ITaskProcessor { public void Process(string payload) => Thread.Sleep(10); }
    public sealed class LoggingDecorator : ITaskProcessor
    {
        private readonly ITaskProcessor _inner; private readonly string _tag;
        public LoggingDecorator(ITaskProcessor inner, string tag) { _inner = inner; _tag = tag; }
        public void Process(string payload) { Console.WriteLine($"[LOG:{_tag}] старт '{payload}'"); _inner.Process(payload); Console.WriteLine($"[LOG:{_tag}] завершено"); }
    }
    public sealed class TimingDecorator : ITaskProcessor
    {
        private readonly ITaskProcessor _inner;
        public TimingDecorator(ITaskProcessor inner) => _inner = inner;
        public void Process(string payload) { var sw = Stopwatch.StartNew(); _inner.Process(payload); Console.WriteLine($"[TIME] {sw.ElapsedMilliseconds} мс"); }
    }
    public sealed class RetryDecorator : ITaskProcessor
    {
        private readonly ITaskProcessor _inner; private readonly int _retries;
        public RetryDecorator(ITaskProcessor inner, int retries) { _inner = inner; _retries = retries; }
        public void Process(string payload) { for (int i = 0; i <= _retries; i++) { try { _inner.Process(payload); return; } catch when (i < _retries) { } } }
    }

    // --- Завдання 4: Observer ---
    public interface IOrderObserver { void OnStatusChanged(string status); }
    public sealed class OrderTracker
    {
        private readonly List<IOrderObserver> _observers = new();
        public void Subscribe(IOrderObserver o) => _observers.Add(o);
        public void Unsubscribe(IOrderObserver o) => _observers.Remove(o);
        public void SetStatus(string status)
        {
            Console.WriteLine($"Статус замовлення: {status}");
            foreach (var o in _observers.ToArray())
            {
                try { o.OnStatusChanged(status); }
                catch (Exception ex) { Console.WriteLine($"[Ізоляція помилки {o.GetType().Name}] {ex.Message}"); }
            }
        }
    }
    public sealed class LogObserver : IOrderObserver { public void OnStatusChanged(string status) => Console.WriteLine($"  LogObserver -> {status}"); }
    public sealed class BrokenObserver : IOrderObserver { public void OnStatusChanged(string status) => throw new InvalidOperationException("Збій підписника"); }
    public sealed class AuditObserver : IOrderObserver { public void OnStatusChanged(string status) => Console.WriteLine($"  AuditObserver -> зафіксовано '{status}'"); }

    // --- Завдання 5: Strategy ---
    public interface IPricingStrategy { decimal Apply(decimal baseAmount); }
    public sealed class NoDiscountStrategy : IPricingStrategy { public decimal Apply(decimal baseAmount) => baseAmount; }
    public sealed class VipDiscountStrategy : IPricingStrategy { public decimal Apply(decimal baseAmount) => baseAmount * 0.85m; }
    public sealed class PromoDiscountStrategy : IPricingStrategy
    {
        private readonly decimal _rate;
        public PromoDiscountStrategy(decimal rate) => _rate = rate;
        public decimal Apply(decimal baseAmount) => baseAmount * (1m - _rate);
    }
    public sealed class CheckoutContext
    {
        private IPricingStrategy _strategy;
        public CheckoutContext(IPricingStrategy strategy) => _strategy = strategy;
        public void SetStrategy(IPricingStrategy strategy) => _strategy = strategy;
        public decimal CalculateTotal(decimal amount) => _strategy.Apply(amount);
    }
}