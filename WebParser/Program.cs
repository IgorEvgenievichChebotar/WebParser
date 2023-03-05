using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebParser.Schemes;

/*
 * p.s. я не стал переусложнять код абстракциями, библиотеками, параллельностью и тд,
 * но убрать асинхронность мне не позволила совесть,
 * поскольку с ней код усложняется незначительно,
 * зато при операциях ввода-вывода даёт большие преимущества в эффективности.
 */

var url = "https://www.lesegais.ru/open-area/graphql";

while (true)
{
    var deals = await GetDealsAsync(url);

    deals = Validate(deals);

    await LoadToDbAsync(deals);

    await Task.Delay(TimeSpan.FromMinutes(10));
}


static List<Deal> Validate(List<Deal> deals)
{
    var duplicateDealNumbers = deals.GroupBy(d => d.dealNumber)
        .Where(g => g.Count() > 1)
        .SelectMany(g => g.Skip(1));

    var incorrectDate = deals.Where(d => d.dealDate > DateTime.Now ||
                                         d.dealDate < DateTime.Parse("01-01-1950"));

    var remained = deals.Except(incorrectDate)
        .Except(duplicateDealNumbers)
        .ToList();

    Console.WriteLine($"{DateTime.Now} | Удалено {deals.Count - remained.Count} некорректных записей");

    return remained;
}

static async Task LoadToDbAsync(List<Deal> deals)
{
    var connectionString = "Server=localhost;Database=Deals;Trusted_Connection=True;TrustServerCertificate=Yes;";
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    using var transaction = connection.BeginTransaction();
    try
    {
        foreach (var deal in deals)
        {
            var commandText = @"IF NOT EXISTS (SELECT 1 FROM Deals WHERE dealNumber = @dealNumber)
                                BEGIN
                                    INSERT INTO Deals (sellerName, sellerInn, buyerName, buyerInn, woodVolumeBuyer, woodVolumeSeller, dealDate, dealNumber) 
                                    VALUES (@sellerName, @sellerInn, @buyerName, @buyerInn, @woodVolumeBuyer, @woodVolumeSeller, @dealDate, @dealNumber)
                                END";
            using var command = new SqlCommand(commandText, connection, transaction);
            command.Parameters.AddWithValue("@sellerName", deal.sellerName ?? "");
            command.Parameters.AddWithValue("@sellerInn", deal.sellerInn ?? "");
            command.Parameters.AddWithValue("@buyerName", deal.buyerName ?? "");
            command.Parameters.AddWithValue("@buyerInn", deal.buyerInn ?? "");
            command.Parameters.AddWithValue("@woodVolumeBuyer", deal.woodVolumeBuyer);
            command.Parameters.AddWithValue("@woodVolumeSeller", deal.woodVolumeSeller);
            command.Parameters.AddWithValue("@dealDate", deal.dealDate ?? DateTime.Now);
            command.Parameters.AddWithValue("@dealNumber", deal.dealNumber);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
        Console.WriteLine($"{DateTime.Now} | Вставлено {deals.Count} строк в базу данных");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{DateTime.Now} | Ошибка: {ex.Message}");
        transaction.Rollback();
    }
}

static async Task<List<Deal>> GetDealsAsync(string url)
{
    var client = new HttpClient();
    var deals = new List<Deal>();
    var number = 0; // страница
    var size = 10000; // записей на странице

    while (true)
    {
        var request = new Request(size, number);
        var json = JsonConvert.SerializeObject(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.UserAgent.ParseAdd("c# parser");
        var response = await client.PostAsync(url, content);

        var result = JsonConvert.DeserializeObject<Response>(await response.Content.ReadAsStringAsync());
        var newDeals = result.Data.SearchReportWoodDeal.Content;

        if (newDeals.Count == 0) // новых записей нет, проход окончен, можно вернуть коллекцию
        {
            return deals;
        }

        deals.AddRange(newDeals);
        Console.WriteLine($"{DateTime.Now} | Загружено {newDeals.Count} новых записей, всего: {deals.Count}");
        number++;

        await Task.Delay(1000); // секунда на перерыв между запросами
    }
}