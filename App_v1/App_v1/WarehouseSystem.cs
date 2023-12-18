using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static App_v1.WarehouseSystem;
using System.Reflection;


namespace App_v1
{
    internal class WarehouseSystem
    {
        // КОНСТАНТА ВРЕМЕНИ МЕЖДУ ОПОВЕЩЕНИЯМИ
        public static int sleepTime = 0;

        //////////////////////////////////////////////////// КЛИЕНТЫ

        public class Client
        {
            public TaskSystem ts;
            public Warehouse warehouse;

            public string name;
            public string address;
        }

        public class Supplier : Client
        {
            public Invoice invoice;

            public Product productToSell;

            public Supplier(Product product, TaskSystem ts, Warehouse warehouse, string name, string address)
            {
                this.productToSell = product;
                this.name = name;
                this.ts = ts;
                this.warehouse = warehouse;
                this.address = address;
            }

            public void GetApprovement()
            {
                // Согласует поставку с менеджером
                Console.WriteLine("Supplier " + name + " создает заявку на согласование поставки товара " + productToSell.productName);
                ts.AddApprTask(new ApprovementTask(productToSell));
                Thread.Sleep(sleepTime);
            }

            public void SellProduct()
            {
                // Если товар соотвествует требованиям хранения (проверяют рабочие) и у него есть ID
                if (productToSell.isAccepted == true)
                {
                    // Продовец создает заявку, где указывает свои данные и продукт, который он хочет разместить на основном складе
                    Console.WriteLine("Supplier " + name + " создает заявку на продажу товара с ID " + productToSell.ProductId);

                    ts.AddSupplierTask(new SupplierTask(this.name, this.address, this.productToSell.ProductId));
                    Thread.Sleep(sleepTime);

                    // Удаляем продукт у продовца, теперь товар оффициально на временном складе
                    Console.WriteLine("Supplier " + name + " остался без товара " + productToSell.productName);
                    productToSell = null;
                }
            }

            public void TransportProduct()
            {
                // Если поставка согласован (менеджером), то можно привозить товар на временный склад
                if (productToSell.isApproved == true)
                {

                    // Продовец привзоит товар на временный склад
                    Console.WriteLine("Supplier " + name + " привозит товар " + productToSell.productName + " на временный склад");
                    Thread.Sleep(sleepTime);

                    warehouse.AddTempProduct(productToSell);
                    Thread.Sleep(sleepTime);

                    // Рабочие тут же проверяют поступивший товар и присваивают ему ID
                    Console.WriteLine("Supplier " + name + " создает заявку на проверку товара " + productToSell.productName);
                    ts.AddCheckTask(new CheckTask(productToSell, this));
                    Thread.Sleep(sleepTime);
                }
            }
        }

        public class Customer : Client
        {
            public int productID;

            public void BuyProduct()
            {
                Console.WriteLine($"Покупатель {name} оплатил товар с ID {productID}");
                ts.AddPaymentTask(new PaymentTask(this.name, productID));

                Console.WriteLine($"Покупатель {name} хочет купить товар с ID {productID}");
                ts.AddPurchaseTask(new PurchaseTask(productID, this.address));
            }

            public Customer(int productID, TaskSystem ts, Warehouse warehouse, string name, string address)
            {
                this.name = name;
                this.ts = ts;
                this.warehouse = warehouse;
                this.address = address;
                this.productID = productID;
            }
        }

        //////////////////////////////////////////////////// ПЕРСОНАЛ

        public class Staff
        {
            public string name;
            public TaskSystem ts;
        }

        
        public class Manager : Staff
        {
            /* Задачи, которые решает менеджер */
            public SupplierTask supplierTask;
            public ApprovementTask apprTask;
            public PaymentTask paymTask;

            public DataBase dataBase;

            public Manager(string name, TaskSystem ts, DataBase dataBase)
            {
                this.name = name;
                this.ts = ts;
                this.dataBase = dataBase;
            }

            /* Менеджер берет в исполнение заявку на поступление */
            public void GetSupplierTask()
            {
                this.supplierTask = ts.GetSupplierTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по продаже товара c ID " + supplierTask.productID);
                Thread.Sleep(sleepTime);
            }

            /* Менеджер берет в исполнение заявку на согласование */
            public void GetApprTask()
            {
                this.apprTask = ts.GetApprTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по согласованию поставки товара " + apprTask.product.productName);
                Thread.Sleep(sleepTime);
            }

            /* Менеджер берет в исполнение заявку на проведение оплаты */
            public void GetPaymTask()
            {
                this.paymTask = ts.GetPaymentTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по оплате товара с ID " + paymTask.productID);
                Thread.Sleep(sleepTime);
            }

            /* Менеджер исполняет заявку на поступление */
            public void SolveSupplierTask()
            {
                /* Создает задачу для Keeper, чтобы он  проверил и разместил товар на складе */
                ts.AddKeeperTask(new KeeperTask(supplierTask.productID));
                Console.WriteLine("Менеджер " + name + " создает заявку на размещение товара с ID " + supplierTask.productID + " на основной склад");

                /* Менеджер помечает заявку как выполненную */
                supplierTask.Execute();
                Thread.Sleep(sleepTime);
            }

            /* Менеджер исполняет заявку на согласование */
                public void SolveApprTask()
            {
                apprTask.product.isApproved = true;
                Console.WriteLine("Manager " + name + " согласовал для поставки товар " + apprTask.product.productName);
                Thread.Sleep(sleepTime);
                apprTask.Execute();
            }

            /* Менеджер исполняет заявку на проведение оплаты */
            public void SolvePaymTask()
            {
                /* Менеджер в базе данных накладную на товар из заявки */
                Invoice foundInvoice = null; 
                foreach (Invoice invoice in dataBase.Invoices)
                {
                    if (invoice.productID == paymTask.productID)
                    {
                        Console.WriteLine($"Manager {name} отправил поставщику {invoice.supplierName} сумму {invoice.productPrice} до вычета комиссии");
                        foundInvoice = invoice; 
                        break; 
                    }
                }
                if (foundInvoice != null)
                {
                    dataBase.Invoices.Remove(foundInvoice); 
                    paymTask.Execute();
                }
            }
        }

        public class Worker : Staff
        {
            public CheckTask checkTask;
            public TransportTask transportTask;
            public PurchaseTask purchaseTask;
            public Warehouse warehouse;
            public DataBase dataBase;

            private static int nextProductId = 1;

            public Worker(string name, TaskSystem ts, Warehouse warehouse, DataBase dataBase)
            {
                this.name = name;
                this.ts = ts;
                this.warehouse = warehouse;
                this.dataBase = dataBase;
            }

            public void GetCheckTask()
            {
                this.checkTask = ts.GetCheckTask();
                Console.WriteLine("Worker " + name + " взял в исполнение задачу на проверку товара " + checkTask.product.productName);
                Thread.Sleep(sleepTime);
            }

            public void GetPurchaseTask()
            {
                this.purchaseTask = ts.GetPurchaseTask();
                Console.WriteLine("Worker " + name + " взял в исполнение задачу на покупку товара");
                Thread.Sleep(sleepTime);
            }

            public void GetTransportTask()
            {
                this.transportTask = ts.GetTransportTask();
                Console.WriteLine("Worker " + name + " взял в исполнение задачу на перевозку товара с ID " + transportTask.productID + " на основной склад.");
                Thread.Sleep(sleepTime);
            }

            public void SolveCheckTask()
            {
                checkTask.product.ProductId = nextProductId++;
                Console.WriteLine("Worker " + name + " присвоил товару " + checkTask.product.productName + " ID " + checkTask.product.ProductId);
                Thread.Sleep(sleepTime);

                Console.WriteLine($"Worker {name} создет накладную на товар {checkTask.product.productName} для поставщика {checkTask.supplier.name}");
                checkTask.supplier.invoice = new Invoice(checkTask.supplier.name, checkTask.supplier.address, checkTask.product.ProductId, checkTask.product.productPrice);
                Thread.Sleep(sleepTime);

                Console.WriteLine($"Worker {name} добавляет накладную в базу данных");
                dataBase.Invoices.Add(checkTask.supplier.invoice);
                Thread.Sleep(sleepTime);

                if (checkTask.product.meetRequirements == true)
                {
                    checkTask.product.isAccepted = true;
                    Console.WriteLine("Worker " + name + " присвоил товару " + checkTask.product.productName + " статус accepted");
                    Thread.Sleep(sleepTime);
                }

                else
                {
                    Console.WriteLine($"Товар {checkTask.product.productName} не прошел проверку");
                    Thread.Sleep(sleepTime);

                    Console.WriteLine($"Worker {name} присвоил накладной поставщика {checkTask.product.productName} статус возврата");
                    Thread.Sleep(sleepTime);

                    checkTask.supplier.invoice.isReturn = true;
                    Console.WriteLine($"Worker {name} вернул товар {checkTask.product.productName} по накладной поставщику {checkTask.supplier.name}");
                    Thread.Sleep(sleepTime);

                    Product product = warehouse.GetTempProduct(checkTask.product.ProductId);
                    Thread.Sleep(sleepTime);
                }

                checkTask.Execute();
            }

            public void SolvePurchaseTask()
            {
                Product product = warehouse.GetMainProduct(purchaseTask.productID);

                Console.WriteLine("Worker " + name + " отправил товар " + product.productName + " клиенту по адресу " + purchaseTask.clientAddress);
                Thread.Sleep(sleepTime);

                Thread.Sleep(sleepTime);

                purchaseTask.Execute();
                Thread.Sleep(sleepTime);
            }

            public void SolveTransportTask()
            {
                warehouse.MoveProduct(transportTask.productID);
                Console.WriteLine("Worker " + name + " перенес товар с ID " + transportTask.productID + " на основной склад");
                Thread.Sleep(sleepTime);

                transportTask.Execute();
            }
        }

        public class Keepper : Staff
        {
            public KeeperTask keeperTask;
            public Warehouse warehouse;

            public Keepper(string name, TaskSystem ts, Warehouse warehouse)
            {
                this.name = name;
                this.ts = ts;
                this.warehouse = warehouse;
            }

            public void GetKeeperTask()
            {
                this.keeperTask = ts.GetKeeperTask();
                Console.WriteLine("Keeper " + name + " взял в исполнение заявку по размещению товара с ID " + keeperTask.productID);
                Thread.Sleep(sleepTime);
            }

            public void SolveKeeperTask()
            {
                // Назначает рабочим место, куда нужно разместить указанный товар
                Console.WriteLine("Keeper " + name + " назначил рабочим разместить на основной склад товар с ID " + keeperTask.productID);
                ts.AddTransportTask(new TransportTask(keeperTask.productID, warehouse));
                Thread.Sleep(sleepTime);
                keeperTask.Execute();
            }
        }
        //////////////////////////////////////////////////// НАКЛАДНАЯ

        public class Invoice
        {
            public string supplierName;

            public string supplierAddress;

            public int productID;

            public int productPrice;

            public bool isReturn;

            public Invoice(string supplierName, string supplierAddress, int productID, int productPrice)
            {
                this.supplierName = supplierName;
                this.supplierAddress = supplierAddress;
                this.productID = productID;
                isReturn = false;
                this.productPrice = productPrice;
            }
        }

        public class DataBase
        {
            public List<Invoice> Invoices;

            public DataBase() { Invoices = new List<Invoice>(); }

        }


        //////////////////////////////////////////////////// ПРОДУКТ

        public class Product
        {
            public int productPrice;
            public string productName;
            public bool meetRequirements;

            // Свойство для хранения уникального ID
            public int ProductId;

            // Товар согласован для хранения
            public bool isApproved = false;

            // Товар принят на хранение
            public bool isAccepted = false;

            public Product(bool mR, string name, int productPrice)
            {
                this.meetRequirements = mR;
                this.productName = name;
                this.productPrice = productPrice;
            }
        }

        //////////////////////////////////////////////////// СКЛАД

        public class Warehouse
        {
            // Временный склад
            public List<Product> tempProducts;

            // Основной склад
            public List<Product> mainProducts;

            public Warehouse()
            {
                tempProducts = new List<Product>();
                mainProducts = new List<Product>();
            }

            public void AddTempProduct(Product product)
            {
                Console.WriteLine($"Товар {product.productName} успешно перемещен на временный склад.");
                tempProducts.Add(product);
            }

            public void MoveProduct(int productId)
            {
                // Находим Товар в tempProducts по productID
                Product productToMove = tempProducts.Find(p => p.ProductId == productId);

                // Если Товар найден, добавляем его в mainProducts и удаляем из tempProducts
                if (productToMove != null)
                {
                    mainProducts.Add(productToMove);
                    tempProducts.Remove(productToMove);

                    Console.WriteLine($"Товар с ID {productId} успешно перемещен на основной склад.");
                }
                else
                {
                    Console.WriteLine($"Товар с ID {productId} не найден на временном складе.");
                }
            }

            public Product GetTempProduct(int productId)
            {
                // Ищем продукт с нужным ProductId на временном складе
                Product productToRemove = tempProducts.FirstOrDefault(product => product.ProductId == productId);

                if (productToRemove != null)
                {
                    Console.WriteLine($"Товар {productToRemove.productName} с ID {productId} забрали с временного склада.");
                    tempProducts.Remove(productToRemove);
                }
                else
                {
                    Console.WriteLine($"Товар с ID {productId} не найден на временном складе.");
                }

                return productToRemove;
            }

            public Product GetMainProduct(int productId)
            {
                // Ищем продукт с нужным ProductId на временном складе
                Product productToRemove = mainProducts.FirstOrDefault(product => product.ProductId == productId);

                if (productToRemove != null)
                {
                    Console.WriteLine($"Товар {productToRemove.productName} с ID {productId} забрали с временного склада.");
                    mainProducts.Remove(productToRemove);
                }
                else
                {
                    Console.WriteLine($"Товар с ID {productId} не найден на временном складе.");
                }

                return productToRemove;
            }
        }

        //////////////////////////////////////////////////// ЗАДАЧИ

        public class TaskSystem
        {
            /* Стеки для хранения заявок */
            private Stack<PurchaseTask> purchaseTasks;
            private Stack<SupplierTask> supplierTasks;
            private Stack<ApprovementTask> apprTasks;
            private Stack<CheckTask> checkTasks;
            private Stack<KeeperTask> keeperTasks;
            private Stack<TransportTask> transportTasks;
            private Stack<PaymentTask> paymentTasks;


            public TaskSystem()
            {
                purchaseTasks = new Stack<PurchaseTask>();
                supplierTasks = new Stack<SupplierTask>();
                apprTasks = new Stack<ApprovementTask>();
                checkTasks = new Stack<CheckTask>();
                keeperTasks = new Stack<KeeperTask>();
                transportTasks = new Stack<TransportTask>();
                paymentTasks = new Stack<PaymentTask>();
            }

            public void AddPaymentTask(PaymentTask task)
            {
                paymentTasks.Push(task);
            }

            public void AddPurchaseTask(PurchaseTask task)
            {
                purchaseTasks.Push(task);
            }

            public void AddSupplierTask(SupplierTask task)
            {
                supplierTasks.Push(task);
            }

            public void AddApprTask(ApprovementTask task)
            {
                apprTasks.Push(task);
            }

            public void AddCheckTask(CheckTask task)
            {
                checkTasks.Push(task);
            }

            public void AddTransportTask(TransportTask task)
            {
                transportTasks.Push(task);
            }

            public void AddKeeperTask(KeeperTask task)
            {
                keeperTasks.Push(task);
            }

            public PaymentTask GetPaymentTask()
            {
                return paymentTasks.Pop();
            }

            public PurchaseTask GetPurchaseTask()
            {
                return purchaseTasks.Pop();
            }

            public SupplierTask GetSupplierTask()
            {
                return supplierTasks.Pop();
            }

            public ApprovementTask GetApprTask()
            {
                return apprTasks.Pop();
            }

            public CheckTask GetCheckTask()
            {
                return checkTasks.Pop();
            }

            public TransportTask GetTransportTask()
            {
                return transportTasks.Pop();
            }

            public KeeperTask GetKeeperTask()
            {
                return keeperTasks.Pop();
            }
        }

        public class Application
        {
            private static int nextApplicationID = 1;

            public int applicationID;
            public ApplicationStatus status;

            public Application()
            {
                Thread.Sleep(sleepTime);
                applicationID = nextApplicationID++;
                Console.WriteLine($"Создана заявка с ID {applicationID}. {CheckDocType()}");
                status = ApplicationStatus.UnderReview;
                Thread.Sleep(sleepTime);
            }

            public void Execute()
            {
                Console.WriteLine($"Выполнена заявка с ID {applicationID}. {CheckDocType()}");
                status = ApplicationStatus.Executed;
                Thread.Sleep(sleepTime);
            }

            // Метод для проверки типа документа и вывода сообщения
            public string CheckDocType()
            {
                if (this is PurchaseTask)
                {
                    return "Покупка товара";
                }
                else if (this is SupplierTask)
                {
                    return "Продажа товара";
                }
                else if (this is KeeperTask)
                {
                    return "Размещение товара";
                }
                else if (this is ApprovementTask)
                {
                    return "Согласование поставки";
                }
                else if (this is TransportTask)
                {
                    return "Размещение товара на основном складе";
                }
                else if (this is CheckTask)
                {
                    return "Проверка товара";
                }
                else if (this is PaymentTask)
                {
                    return "Оплата товара";
                }
                else
                {
                    return null;
                }
            }
        }

        public enum ApplicationStatus
        {
            UnderReview,
            Executed
        }

        public class PaymentTask : Application
        {
            public string customerName;

            public int productID;

            public PaymentTask(string customerName, int productID)
            {
                this.customerName = customerName;
                this.productID = productID;
            }
        }

        public class PurchaseTask : Application
        {
            public int productID;

            public string clientAddress;

            public PurchaseTask(int productID, string clientAddress)
            {
                this.productID = productID;
                this.clientAddress = clientAddress;
            }
        }

        public class SupplierTask : Application
        {
            public int productID;

            public string supplierName;

            public string supplierAddress;

            public SupplierTask(string address, string name, int productID)
            {
                this.supplierName = name;
                this.supplierAddress = address;
                this.productID = productID;
            }
        }

        // Кладовщик решает, как резместить товар
        public class KeeperTask : Application
        {
            public int productID;

            public KeeperTask(int productID)
            {
                this.productID = productID;
            }

        }

        // Менеджер решает, принять товар на склад или нет
        public class ApprovementTask : Application
        {
            public Product product;

            public ApprovementTask(Product product)
            {
                this.product = product;
            }
        }

        // Работник проверяет поступивший на temp склад товар
        public class CheckTask : Application
        {
            public Product product;

            public Supplier supplier;

            public CheckTask(Product product, Supplier supplier)
            {
                this.product = product;
                this.supplier = supplier;
            }
        }

        // Работник размещает товар на основной склад
        public class TransportTask : Application
        {
            public int productID;

            public Warehouse wh;

            // Нужно ID товара и место на складе
            public TransportTask(int productID, Warehouse wh)
            {
                this.productID = productID;
                this.wh = wh;
            }
        }

        static void Main(string[] args)
        {
            Demonstration demo = new Demonstration();

            demo.Demonstrate();
        }
    }
}



