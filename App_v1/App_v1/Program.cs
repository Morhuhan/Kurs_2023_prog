using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static App_v1.Program;
using System.Reflection;


namespace App_v1
{
    internal class Program
    {
        // КОНСТАНТА ВРЕМЕНИ МЕЖДУ ОПОВЕЩЕНИЯМИ
        public static int sleepTime = 500;

        //////////////////////////////////////////////////// КЛИЕНТЫ

        public class Client
        {
            public TaskSystem ts;
            public Warehouse warehouse;

            public string name;
            public string address;
        }

        public class Seller : Client
        {
            public Invoice invoice;

            public Product productToSell;

            public Seller(Product product, TaskSystem ts, Warehouse warehouse, string name, string address)
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
                Console.WriteLine("Seller " + name + " создает заявку на согласование поставки товара " + productToSell.productName);
                ts.AddApprTask(new ApprovementTask(productToSell));
                Thread.Sleep(sleepTime);
            }

            public void SellProduct()
            {
                // Если товар соотвествует требованиям хранения (проверяют рабочие) и у него есть ID
                if (productToSell.isAccepted == true)
                {
                    // Продовец создает заявку, где указывает свои данные и продукт, который он хочет разместить на основном складе
                    Console.WriteLine("Seller " + name + " создает заявку на продажу товара с ID " + productToSell.ProductId);

                    ts.AddSellerTask(new SellerTask(this.name, this.address, this.productToSell.ProductId));
                    Thread.Sleep(sleepTime);

                    // Удаляем продукт у продовца, теперь товар оффициально на временном складе
                    Console.WriteLine("Seller " + name + " остался без товара " + productToSell.productName);
                    productToSell = null;
                }
            }

            public void TransportProduct()
            {
                // Если поставка согласован (менеджером), то можно привозить товар на временный склад
                if (productToSell.isApproved == true)
                {

                    // Продовец привзоит товар на временный склад
                    Console.WriteLine("Seller " + name + " привозит товар " + productToSell.productName + " на временный склад");
                    Thread.Sleep(sleepTime);

                    warehouse.AddTempProduct(productToSell);
                    Thread.Sleep(sleepTime);

                    // Рабочие тут же проверяют поступивший товар и присваивают ему ID
                    Console.WriteLine("Seller " + name + " создает заявку на проверку товара " + productToSell.productName);
                    ts.AddCheckTask(new CheckTask(productToSell, this));
                    Thread.Sleep(sleepTime);
                }
            }
        }

        public class Buyer : Client
        {
            public int productID;

            public void BuyProduct()
            {
                Console.WriteLine($"Покупатель {name} оплатил товар с ID {productID}");
                ts.AddPaymentTask(new PaymentTask(this.name, productID));

                Console.WriteLine($"Покупатель {name} хочет купить товар с ID {productID}");
                ts.AddPurchaseTask(new PurchaseTask(1, this.address));
            }

            public Buyer(int productID, TaskSystem ts, Warehouse warehouse, string name, string address)
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
            public SellerTask sellerTask;
            public ApprovementTask apprTask;
            public PaymentTask paymTask;

            public DataBase dataBase;

            public Manager(string name, TaskSystem ts, DataBase dataBase)
            {
                this.name = name;
                this.ts = ts;
                this.dataBase = dataBase;
            }

            public void GetSellerTask()
            {
                this.sellerTask = ts.GetSellerTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по продаже товара c ID " + sellerTask.productID);
                Thread.Sleep(sleepTime);
            }

            public void GetApprTask()
            {
                this.apprTask = ts.GetApprTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по согласованию поставки товара " + apprTask.product.productName);
                Thread.Sleep(sleepTime);
            }

            public void GetPaymTask()
            {
                this.paymTask = ts.GetPaymentTask();
                Console.WriteLine("Менеджер " + name + " взял в исполнение заявку по оплате товара с ID " + paymTask.productID);
                Thread.Sleep(sleepTime);
            }


            public void SolveSellerTask()
            {
                // Создает задачу для Keeper, чтобы он  проверил и разместил товар на складе
                ts.AddKeeperTask(new KeeperTask(sellerTask.productID));
                Console.WriteLine("Менеджер " + name + " создает заявку на размещение товара с ID " + sellerTask.productID + " на основной склад");

                // Менеджер помечает задачу как выполненную и может брать новую
                sellerTask.Execute();
                Thread.Sleep(sleepTime);
            }

            public void SolveApprTask()
            {
                apprTask.product.isApproved = true;
                Console.WriteLine("Manager " + name + " согласовал для поставки товар " + apprTask.product.productName);
                Thread.Sleep(sleepTime);
                apprTask.Execute();
            }

            public void SolvePaymTask()
            {
                // Ищет в базе данных накладную на товар из заявки
                Invoice inv = dataBase.Invoices[paymTask.productID-1];
                dataBase.Invoices.Remove(inv);

                Console.WriteLine($"Manager {name} отправил поставщику {inv.sellerName} сумму {inv.productPrice} до вычета комиссии");
                dataBase.Invoices.Remove(inv);

                paymTask.Execute();
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
                Console.WriteLine($"Worker {name} создет накладную на товар {checkTask.product.productName} для поставщика {checkTask.seller.name}");
                checkTask.seller.invoice = new Invoice(checkTask.seller.name, checkTask.seller.address, checkTask.product.productName, checkTask.product.productPrice);
                Thread.Sleep(sleepTime);

                Console.WriteLine($"Worker {name} добавляет накладную в базу данных");
                dataBase.Invoices.Add(checkTask.seller.invoice);
                Thread.Sleep(sleepTime);

                if (checkTask.product.meetRequirements == true)
                {
                    checkTask.product.isAccepted = true;
                    Console.WriteLine("Worker " + name + " присвоил товару " + checkTask.product.productName + " статус accepted");
                    Thread.Sleep(sleepTime);


                    checkTask.product.ProductId = nextProductId++;
                    Console.WriteLine("Worker " + name + " присвоил товару " + checkTask.product.productName + " ID " + checkTask.product.ProductId);
                    Thread.Sleep(sleepTime);
                }

                else
                {
                    Console.WriteLine($"Товар {checkTask.product.productName} не прошел проверку");
                    Thread.Sleep(sleepTime);

                    Console.WriteLine($"Worker {name} присвоил накладной поставщика {checkTask.product.productName} статус возврата");
                    Thread.Sleep(sleepTime);

                    checkTask.seller.invoice.isReturn = true;
                    Console.WriteLine($"Worker {name} вернул товар {checkTask.product.productName} по накладной поставщику {checkTask.seller.name}");
                    Thread.Sleep(sleepTime);

                    Product product = warehouse.GetTempProduct(checkTask.product.ProductId+1);
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
            public string sellerName;

            public string sellerAddress;

            public string productName;

            public int productPrice;

            public bool isReturn;

            public Invoice(string sellerName, string sellerAddress, string productName, int productPrice)
            {
                this.sellerName = sellerName;
                this.sellerAddress = sellerAddress;
                this.productName = productName;
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
            public int ProductId { get; set; }

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

            public Product GetTempProduct(int id)
            {
                id--;
                Console.WriteLine($"Товар {tempProducts[id].productName} забрали с временного склада.");
                Product product = tempProducts[id];
                tempProducts.RemoveAt(id);
                return product;
            }

            public Product GetMainProduct(int id)
            {
                id--;

                Console.WriteLine($"Товар {mainProducts[id].productName} забрали с основного склада.");
                Product product = mainProducts[id];
                mainProducts.RemoveAt(id);
                return product;
            }
        }

        //////////////////////////////////////////////////// ЗАДАЧИ

        public class TaskSystem
        {
            private Stack<PurchaseTask> purchaseTasks;
            private Stack<SellerTask> sellerTasks;
            private Stack<ApprovementTask> apprTasks;
            private Stack<CheckTask> checkTasks;
            private Stack<KeeperTask> keeperTasks;
            private Stack<TransportTask> transportTasks;
            private Stack<ReturnTask> returnTasks;
            private Stack<PaymentTask> paymentTasks;


            public TaskSystem()
            {
                purchaseTasks = new Stack<PurchaseTask>();
                sellerTasks = new Stack<SellerTask>();
                apprTasks = new Stack<ApprovementTask>();
                checkTasks = new Stack<CheckTask>();
                keeperTasks = new Stack<KeeperTask>();
                transportTasks = new Stack<TransportTask>();
                returnTasks = new Stack<ReturnTask>();
                paymentTasks = new Stack<PaymentTask>();
            }

            public void AddPaymentTask(PaymentTask task)
            {
                paymentTasks.Push(task);
            }

            public void AddReturnTask(ReturnTask task)
            {
                returnTasks.Push(task);
            }

            public void AddPurchaseTask(PurchaseTask task)
            {
                purchaseTasks.Push(task);
            }

            public void AddSellerTask(SellerTask task)
            {
                sellerTasks.Push(task);
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

            public ReturnTask GetReturnTask()
            {
                return returnTasks.Pop();
            }

            public PaymentTask GetPaymentTask()
            {
                return paymentTasks.Pop();
            }

            public PurchaseTask GetPurchaseTask()
            {
                return purchaseTasks.Pop();
            }

            public SellerTask GetSellerTask()
            {
                return sellerTasks.Pop();
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

        public class Document
        {
            private static int nextDocumentID = 1;

            public int DocumentID;
            public DocumentStatus Status;

            public Document()
            {
                Thread.Sleep(sleepTime);
                DocumentID = nextDocumentID++;
                Console.WriteLine($"Создана заявка с ID {DocumentID}. {CheckDocType()}");
                Status = DocumentStatus.UnderReview;
                Thread.Sleep(sleepTime);
            }

            public void Execute()
            {
                Console.WriteLine($"Выполнена заявка с ID {DocumentID}. {CheckDocType()}");
                Status = DocumentStatus.Executed;
                Thread.Sleep(sleepTime);
            }

            // Метод для проверки типа документа и вывода сообщения
            public string CheckDocType()
            {
                if (this is ReturnTask)
                {
                    return "Возврат товара";
                }
                else if (this is PurchaseTask)
                {
                    return "Покупка товара";
                }
                else if (this is SellerTask)
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

        public enum DocumentStatus
        {
            UnderReview,
            AcceptedForExecution,
            Executed,
            Archived
        }

        public class PaymentTask : Document
        {
            public string buyerName;

            public int productID;

            public PaymentTask(string buyerName, int productID)
            {
                this.buyerName = buyerName;
                this.productID = productID;
            }
        }


        public class ReturnTask : Document
        {
            public string clientAddr;
            public string clientName;

            public ReturnTask(string clientAddr, string clientName)
            {
                this.clientAddr = clientAddr;
                this.clientName = clientName;
            }
        }

        public class PurchaseTask : Document
        {
            public int productID;

            public string clientAddress;

            public PurchaseTask(int productID, string clientAddress)
            {
                this.productID = productID;
                this.clientAddress = clientAddress;
            }
        }

        // Заявка создается продовцом
        public class SellerTask : Document
        {
            // Поля, заполняемые менеджером
            public int productID;

            public string sellerName;

            public string sellerAddress;

            public SellerTask(string address, string name, int productID)
            {
                this.sellerName = name;
                this.sellerAddress = address;
                this.productID = productID;
            }
        }

        // Кладовщик решает, как резместить товар
        public class KeeperTask : Document
        {
            public int productID;

            public KeeperTask(int productID)
            {
                this.productID = productID;
            }

        }

        // Менеджер решает, принять товар на склад или нет
        public class ApprovementTask : Document
        {
            public Product product;

            public ApprovementTask(Product product)
            {
                this.product = product;
            }
        }

        // Работник проверяет поступивший на temp склад товар
        public class CheckTask : Document
        {
            public Product product;

            public Seller seller;

            public CheckTask(Product product, Seller seller)
            {
                this.product = product;
                this.seller = seller;
            }
        }

        // Работник размещает товар на основной склад
        public class TransportTask : Document
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



