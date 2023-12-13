using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static App_v1.Program;

namespace App_v1
{
    internal class Demonstration
    {
        public bool mR;

        public Client client;

        string mRInput;

        string clientType;

        int productID;

        string continueInput;

        bool isContinue = true;

        // Структура склада
        TaskSystem ts;

        Warehouse wh;

        Manager manager1;

        Worker worker1;

        Keepper keepper1;

        DataBase dataBase;

        public Demonstration()
        {
            ts = new TaskSystem();

            wh = new Warehouse();

            dataBase = new DataBase();

            manager1 = new Manager("Oleg", ts, dataBase);

            worker1 = new Worker("Grisha", ts, wh, dataBase);

            keepper1 = new Keepper("Misha", ts, wh);
        }

        public void Demonstrate()
        {

            while (isContinue == true)
            {
                do
                {
                    Console.WriteLine("Выберите тип клиента:\n1) Поставщик\n2) Покупатель");

                    clientType = Console.ReadLine();

                    if (clientType != "1" && clientType != "2")
                    {
                        Console.WriteLine("Ошибка ввода. Введите 1 или 2.");
                    }

                    if (clientType == "2" && wh.mainProducts.Count == 0)
                    {
                        Console.WriteLine("Невозможно создать покупателя - на складе нет товаров");
                    }

                } while (!(clientType == "1" || (clientType == "2" && wh.mainProducts.Count != 0)));


                Console.WriteLine("Введите имя клиента");

                string clientName = Console.ReadLine();

                Console.WriteLine("Введите адрес клиента");

                string clientAddr = Console.ReadLine();

                // Создание клиента поставщика
                if (int.Parse(clientType) == 1)
                {
                    Console.WriteLine($"Введите название продукта, который {clientName} хочет поставить на склад");

                    Product product = CreateProduct();

                    client = new Seller(product, ts, wh, clientName, clientAddr);

                    // Продавец пробует согласовать поставку
                    ((Seller)client).GetApprovement();

                    // Менеджер решает соглисовать поставку или нет
                    manager1.GetApprTask();
                    manager1.SolveApprTask();

                    // Поставщик привозит товар на временный склад, где товар сразу проверяют работники склада и присваивают ему ID (Могут вернуть)
                    ((Seller)client).TransportProduct();

                    worker1.GetCheckTask();
                    worker1.SolveCheckTask();

                    if (((Seller)client).invoice.isReturn != true)
                    {
                        // Если проверка товара прошла успешно, поставщик создает заяку по размещению товара на основном складе
                        ((Seller)client).SellProduct();

                        // Менеджер обрабатывает заявку на поставку
                        manager1.GetSellerTask();
                        manager1.SolveSellerTask();

                        // Хранитель решает свою задачу, путем создания CheckTask и TransportTask для Worker
                        keepper1.GetKeeperTask();
                        keepper1.SolveKeeperTask();

                        // рабочий переносит товар из временного хранилиша в основное
                        worker1.GetTransportTask();
                        worker1.SolveTransportTask();
                    }

                    // Иначе ничего не делаем, товар уже вернули поставщику
                    else
                    {
                    }
                }

                else if (int.Parse(clientType) == 2)
                {
                    int flag = 0;
                    // Создание клиента покупателя
                    do
                    {
                        Console.WriteLine($"Выберите, какой товар хочет купить {clientName}");

                        foreach (Product product in wh.mainProducts)
                        {
                            Console.WriteLine($"ID: {product.ProductId} Название: {product.productName} Цена: {product.productPrice}");
                        }

                        Console.Write("Введите ID товара: ");
                        if (int.TryParse(Console.ReadLine(), out productID))
                        {
                            if (wh.mainProducts.Any(product => product.ProductId == productID))
                            {
                                // ID товара существует, выполните нужные действия
                                Console.WriteLine($"Вы выбрали товар с ID {productID}");
                                flag = 1; // Выход из цикла
                            }
                            else
                            {
                                Console.WriteLine("Товар с указанным ID не существует. Пожалуйста, выберите еще раз.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Введите корректное число.");
                        }

                    } while (flag != 1);




                    client = new Buyer(productID, ts, wh, clientName, clientAddr);

                    // Покупатель оплачивает и получает товар
                    ((Buyer)client).BuyProduct();

                    worker1.GetPurchaseTask();
                    worker1.SolvePurchaseTask();

                    manager1.GetPaymTask();
                    manager1.SolvePaymTask();
                }

                do
                {
                    Console.WriteLine("Вы хотите продолжить?\n1)Да\n2)Нет");

                    continueInput = Console.ReadLine();

                    if (continueInput != "1" && continueInput != "2")
                    {
                        Console.WriteLine("Ошибка ввода. Введите 1 или 2.");

                    }

                } while (continueInput != "1" && continueInput != "2");

                if (int.Parse(continueInput) == 1)
                {
                    isContinue = true;
                }

                else
                {
                    isContinue = false;
                }
            }
        }

        public Product CreateProduct()
        {

            string productInput = Console.ReadLine();

            do
            {
                Console.WriteLine("Продукт удовлетворяет правилам хранения на складе?\n1)Да\n2)Нет");

                mRInput = Console.ReadLine();

                if (int.TryParse(mRInput, out int userInputAsInt))
                {
                    if (userInputAsInt == 1)
                    {
                        mR = true;
                    }
                    else if (userInputAsInt == 2)
                    {
                        mR = false;
                    }
                    else
                    {
                        Console.WriteLine("Ошибка ввода. Введите 1 или 2.");
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка ввода. Введите 1 или 2.");
                }

            } while (mRInput != "1" && mRInput != "2");

            Console.WriteLine("Укажите цену продукта");
            int price = int.Parse(Console.ReadLine());

            Product product = new Product(mR, productInput, price);

            return product;
        }
    }
}
